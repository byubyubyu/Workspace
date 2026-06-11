// 保存先: Assets/Scripts/UI/TabMenuController.cs
// 統合タブメニュー。画面群（装備/スキル/マップ/瓶）をタブで束ね、
//   開閉キー・タブ切替・相互排他・キャラのクローズアップを一元管理する。
//   各画面は IMenuTab（TabShow/TabHide＝パネルの表示だけ）として乗る。
//
//   操作：Tab/ESC=開閉（最後のタブを復元）／C・P・M・I=そのタブを直接開く／←→=タブ巡回。
//   世界は止まらない（既存設計）。被弾でメニューは閉じる（closeOnDamage=falseのタブ＝瓶は除く）。
//   クローズアップ（カメラ矩形・寄り・自分以外消し）はメニューが1回だけ行い、
//   useCloseUp=true のタブ間の切替ではカメラを触らない＝キャラが動かず一瞬で切り替わる。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TabMenuController : MonoBehaviour
{
    public static TabMenuController Instance { get; private set; }

    [Serializable]
    public class TabDef
    {
        public string label;                 // タブ表示名（装備・スキル…）
        public Key directKey = Key.None;     // 直接開くキー（C/P/M/I。バッジ表示にも使う）
        public MonoBehaviour screen;         // IMenuTab実装（装備UI等のコントローラ）
        public bool useCloseUp = true;       // キャラのクローズアップを使うタブか（マップ/瓶はfalse）
        public bool closeOnDamage = true;    // 被弾でメニューを閉じる対象か（瓶はfalse）
        public Button button;                // タブバーのボタン
        public Image background;             // タブ背景（アクティブ色替え）
        public Text labelText;               // タブ名テキスト
    }

    [SerializeField] private List<TabDef> tabs = new List<TabDef>();
    [SerializeField] private Key menuKey = Key.Tab;
    [SerializeField] private GameObject tabBar;            // タブバーUI（開いている間だけ表示）
    [SerializeField] private GameObject closeUpBackdrop;   // クローズアップ中の左右未描画域カバー（各画面の暗幕を一本化）

    [Header("クローズアップ（メニューが一元管理）")]
    [SerializeField] private Camera mainCamera;            // 未設定ならCamera.main
    [SerializeField] private TPSCamera tpsCamera;
    [SerializeField] private float closeUpDistance = 2.0f;
    [SerializeField] private float closeUpPitch = 5f;
    [SerializeField] private float closeUpHeight = 1.2f;
    [SerializeField] private float closeUpFarClip = 8f;
    [SerializeField] private float closeUpViewX = 0.10f;   // ビューポート開始X（キャラを中央カラムへ）
    [SerializeField] private float closeUpViewWidth = 2f / 3f;

    [Header("タブバーの色")]
    [SerializeField] private Color activeTabColor = new Color(0.94f, 0.62f, 0.15f, 0.95f);
    [SerializeField] private Color inactiveTabColor = new Color(0.15f, 0.12f, 0.08f, 0.9f);

    private bool open;
    private int current;
    private bool closeUpActive;
    private float lastHp;
    private PlayerCombatCore watchedCore; // 被弾検知（HP減少ポーリング・ActivePlayerから取得）

    private Rect savedCamRect = new Rect(0f, 0f, 1f, 1f);
    private float savedFarClip;
    private CameraClearFlags savedClearFlags;
    private Color savedBgColor;
    private int savedCullingMask;
    private bool cullingMaskSaved;

    public bool IsOpen => open;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (tabBar != null) tabBar.SetActive(false);
        for (int i = 0; i < tabs.Count; i++)
        {
            int idx = i; // クロージャ束縛
            if (tabs[i].button != null) tabs[i].button.onClick.AddListener(() => OnTabClicked(idx));
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 操作対象がいない／魔族操作中は人間メニューを出さない（進化・転生は既存のC運用のまま）。
        if (!ActivePlayer.Exists || ActivePlayer.Go.GetComponent<DemonCore>() != null)
        {
            if (open) CloseMenu();
            return;
        }

        // 状況起動UI（商人・市民プロフィール）や、直接Iで開いた瓶の最中はメニューを出さない。
        bool blocked =
            (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen) ||
            (CitizenProfileUIController.Instance != null && CitizenProfileUIController.Instance.IsOpen) ||
            (!open && BottleUIController.Instance != null && BottleUIController.Instance.IsOpen);

        // 開閉（Tab / ESC）
        if (kb[menuKey].wasPressedThisFrame || kb[Key.Escape].wasPressedThisFrame)
        {
            if (open) CloseMenu();
            else if (!blocked) OpenMenu(current);
        }

        // 直接キー（C/P/M/I）＝そのタブを開く。開いている時は切替、同じタブなら閉じる。
        for (int i = 0; i < tabs.Count; i++)
        {
            Key k = tabs[i].directKey;
            if (k == Key.None || !kb[k].wasPressedThisFrame) continue;
            if (blocked) break;
            if (open && current == i) CloseMenu();
            else if (open) SwitchTo(i);
            else OpenMenu(i);
            break;
        }

        if (!open) return;

        // タブ巡回（←→。Q/EはゲームのE=拾うと衝突するため不採用）
        if (kb[Key.LeftArrow].wasPressedThisFrame) SwitchTo((current - 1 + tabs.Count) % tabs.Count);
        if (kb[Key.RightArrow].wasPressedThisFrame) SwitchTo((current + 1) % tabs.Count);

        // クローズアップ中は見た目レイヤーを当て直す（装備変更で見た目が作り直される対策）。
        if (closeUpActive) CloseUpIsolator.Refresh();

        // 被弾でメニューを閉じる（瓶タブ等closeOnDamage=falseは除く）。世界は止まらない設計の緊張感。
        if (watchedCore != null)
        {
            float hp = watchedCore.Current;
            if (hp < lastHp - 0.01f && tabs[current].closeOnDamage)
            {
                CloseMenu();
                return;
            }
            lastHp = hp;
        }
    }

    private void OnTabClicked(int index)
    {
        if (!open) return;
        SwitchTo(index);
    }

    public void OpenMenu(int tabIndex)
    {
        if (open || tabs.Count == 0) return;
        open = true;
        current = Mathf.Clamp(tabIndex, 0, tabs.Count - 1);
        if (tabBar != null) tabBar.SetActive(true);

        watchedCore = ActivePlayer.Exists ? ActivePlayer.Go.GetComponent<PlayerCombatCore>() : null;
        lastHp = watchedCore != null ? watchedCore.Current : 0f;

        if (tabs[current].useCloseUp) BeginCloseUp();
        ShowScreen(current);
        UpdateTabBar();
    }

    public void CloseMenu()
    {
        if (!open) return;
        HideScreen(current);
        if (closeUpActive) EndCloseUp();
        if (tabBar != null) tabBar.SetActive(false);
        open = false;
    }

    public void SwitchTo(int index)
    {
        if (!open || index == current || index < 0 || index >= tabs.Count) return;
        HideScreen(current);
        // クローズアップの要否が変わる時だけカメラを触る（装備⇔スキルは触らない＝一瞬で切替）。
        if (tabs[index].useCloseUp && !closeUpActive) BeginCloseUp();
        else if (!tabs[index].useCloseUp && closeUpActive) EndCloseUp();
        current = index;
        ShowScreen(current);
        UpdateTabBar();
    }

    private void ShowScreen(int i) => (tabs[i].screen as IMenuTab)?.TabShow();
    private void HideScreen(int i) => (tabs[i].screen as IMenuTab)?.TabHide();

    private void UpdateTabBar()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            if (tabs[i].background != null)
                tabs[i].background.color = i == current ? activeTabColor : inactiveTabColor;
        }
    }

    // --- クローズアップ（旧・装備UI/P画面に重複していたカメラ管理の一本化） ---

    private void BeginCloseUp()
    {
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            savedCamRect = cam.rect;
            savedFarClip = cam.farClipPlane;
            savedClearFlags = cam.clearFlags;
            savedBgColor = cam.backgroundColor;
            cam.rect = new Rect(closeUpViewX, 0f, closeUpViewWidth, 1f);
            cam.farClipPlane = closeUpFarClip;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            savedCullingMask = cam.cullingMask;
            cullingMaskSaved = true;
            cam.cullingMask = CloseUpIsolator.Mask;
        }
        if (tpsCamera != null) tpsCamera.BeginCloseUp(closeUpDistance, closeUpPitch, closeUpHeight);
        CloseUpIsolator.Isolate(ActivePlayer.Exists ? ActivePlayer.Go : null);
        if (closeUpBackdrop != null) closeUpBackdrop.SetActive(true);
        closeUpActive = true;
    }

    private void EndCloseUp()
    {
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            cam.rect = savedCamRect;
            cam.farClipPlane = savedFarClip;
            cam.clearFlags = savedClearFlags;
            cam.backgroundColor = savedBgColor;
            if (cullingMaskSaved) { cam.cullingMask = savedCullingMask; cullingMaskSaved = false; }
        }
        if (tpsCamera != null) tpsCamera.EndCloseUp();
        CloseUpIsolator.Restore();
        if (closeUpBackdrop != null) closeUpBackdrop.SetActive(false);
        closeUpActive = false;
    }
}
