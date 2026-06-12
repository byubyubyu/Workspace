// 保存先: Assets/Scripts/UI/TabMenuController.cs
// 統合タブメニュー。画面群をタブで束ね、開閉キー・タブ切替・相互排他・キャラのクローズアップを一元管理する。
//   各画面は IMenuTab（TabShow/TabHide＝パネルの表示だけ）として乗る。
//
//   タブセットは操作対象で切り替わる（ActivePlayerにDemonCoreが付いているかで判定）：
//     人間＝装備C/スキルP/マップM/瓶I ／ 魔族＝進化C/マップM/瓶I（マップ・瓶の画面実体は共用）。
//   クローズアップの寄り値・カメラ矩形もセットごと（人間=中央カラム／魔族=左カラム30%＝進化画面の現行レイアウト）。
//
//   操作：Tab/ESC=開閉（最後のタブを復元）／直接キー=そのタブを直接開く／←→=タブ巡回。
//   世界は止まらない（既存設計）。被弾でメニューは閉じる（closeOnDamage=falseのタブ＝瓶は除く）。
//   魔族の転生待ち（死亡）中はメニューを出さない（転生画面が全画面で出るため）。
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

    [Header("人間用タブ（装備/スキル/マップ/瓶）")]
    [SerializeField] private List<TabDef> tabs = new List<TabDef>();
    [SerializeField] private GameObject tabBar;             // タブバーUI（開いている間だけ表示）

    [Header("魔族用タブ（進化/マップ/瓶）")]
    [SerializeField] private List<TabDef> demonTabs = new List<TabDef>();
    [SerializeField] private GameObject demonTabBar;

    [SerializeField] private Key menuKey = Key.Tab;
    [SerializeField] private GameObject closeUpBackdrop;   // クローズアップ中の未描画域カバー（各画面の暗幕を一本化）

    [Header("クローズアップ・人間（メニューが一元管理）")]
    [SerializeField] private Camera mainCamera;            // 未設定ならCamera.main
    [SerializeField] private TPSCamera tpsCamera;
    [SerializeField] private float closeUpDistance = 2.0f;
    [SerializeField] private float closeUpPitch = 5f;
    [SerializeField] private float closeUpHeight = 1.2f;
    [SerializeField] private float closeUpFarClip = 8f;
    [SerializeField] private float closeUpViewX = 0.10f;   // ビューポート開始X（キャラを中央カラムへ）
    [SerializeField] private float closeUpViewWidth = 2f / 3f;

    [Header("クローズアップ・魔族（四足は横に広いので引き気味。矩形=左30%＝進化画面の現行レイアウト）")]
    [SerializeField] private float demonCloseUpDistance = 3.5f;
    [SerializeField] private float demonCloseUpPitch = 5f;
    [SerializeField] private float demonCloseUpHeight = 0.8f;
    [SerializeField] private float demonCloseUpViewX = 0f;
    [SerializeField] private float demonCloseUpViewWidth = 0.3f;

    [Header("タブバーの色")]
    [SerializeField] private Color activeTabColor = new Color(0.94f, 0.62f, 0.15f, 0.95f);
    [SerializeField] private Color inactiveTabColor = new Color(0.15f, 0.12f, 0.08f, 0.9f);

    private bool open;
    private bool demonMode;       // 今のタブセット（false=人間/true=魔族。操作対象から毎フレーム判定）
    private int currentHuman;     // 最後に開いていたタブ（セットごとに記憶＝Tabで復元）
    private int currentDemon;
    private bool closeUpActive;
    private float lastHp;
    private IHealth watchedHealth; // 被弾検知（HP減少ポーリング。人間=PlayerCombatCore/魔族=DemonCore）

    private Rect savedCamRect = new Rect(0f, 0f, 1f, 1f);
    private float savedFarClip;
    private CameraClearFlags savedClearFlags;
    private Color savedBgColor;
    private int savedCullingMask;
    private bool cullingMaskSaved;

    public bool IsOpen => open;

    // 今のモードのタブセット・タブバー・選択タブ。
    private List<TabDef> ActiveTabs => demonMode ? demonTabs : tabs;
    private GameObject ActiveTabBar => demonMode ? demonTabBar : tabBar;
    private int Current
    {
        get => demonMode ? currentDemon : currentHuman;
        set { if (demonMode) currentDemon = value; else currentHuman = value; }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (tabBar != null) tabBar.SetActive(false);
        if (demonTabBar != null) demonTabBar.SetActive(false);
        RegisterTabButtons(tabs);
        RegisterTabButtons(demonTabs);
    }

    private void RegisterTabButtons(List<TabDef> set)
    {
        for (int i = 0; i < set.Count; i++)
        {
            int idx = i; // クロージャ束縛
            if (set[i].button != null)
                set[i].button.onClick.AddListener(() => { if (ActiveTabs == set) OnTabClicked(idx); });
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // 操作対象がいなければメニューなし（陣営選択前など）。
        if (!ActivePlayer.Exists)
        {
            if (open) CloseMenu();
            return;
        }

        // 操作対象でタブセットを選ぶ。切り替わったら開いていても一旦閉じる（カメラ復元込み）。
        var demonCore = ActivePlayer.Go.GetComponent<DemonCore>();
        if ((demonCore != null) != demonMode)
        {
            if (open) CloseMenu();
            demonMode = demonCore != null;
        }
        if (ActiveTabs.Count == 0)
        {
            if (open) CloseMenu();
            return;
        }

        // 転生待ち（死亡）中はメニューを出さない／開いていたら閉じる（転生画面が全画面で出る）。
        if (demonCore != null && demonCore.AwaitingReincarnation)
        {
            if (open) CloseMenu();
            return;
        }

        // 状況起動UI（商人・市民プロフィール）や、直接Iで開いた瓶の最中はメニューを出さない。
        bool blocked = UIScreens.SituationalOpen || (!open && UIScreens.BottleOpen);

        // 開閉（Tab / ESC）
        if (kb[menuKey].wasPressedThisFrame || kb[Key.Escape].wasPressedThisFrame)
        {
            if (open) CloseMenu();
            else if (!blocked) OpenMenu(Current);
        }

        // 直接キー（C/P/M/I）＝そのタブを開く。開いている時は切替、同じタブなら閉じる。
        for (int i = 0; i < ActiveTabs.Count; i++)
        {
            Key k = ActiveTabs[i].directKey;
            if (k == Key.None || !kb[k].wasPressedThisFrame) continue;
            if (blocked) break;
            if (open && Current == i) CloseMenu();
            else if (open) SwitchTo(i);
            else OpenMenu(i);
            break;
        }

        if (!open) return;

        // タブ巡回（←→。Q/EはゲームのE=拾うと衝突するため不採用）
        if (kb[Key.LeftArrow].wasPressedThisFrame) SwitchTo((Current - 1 + ActiveTabs.Count) % ActiveTabs.Count);
        if (kb[Key.RightArrow].wasPressedThisFrame) SwitchTo((Current + 1) % ActiveTabs.Count);

        // クローズアップ中は見た目レイヤーを当て直す（装備変更・部位進化で見た目が作り直される対策）。
        if (closeUpActive) CloseUpIsolator.Refresh();

        // 被弾でメニューを閉じる（瓶タブ等closeOnDamage=falseは除く）。世界は止まらない設計の緊張感。
        if (watchedHealth != null)
        {
            float hp = watchedHealth.Current;
            if (hp < lastHp - 0.01f && ActiveTabs[Current].closeOnDamage)
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
        if (open || ActiveTabs.Count == 0) return;
        open = true;
        Current = Mathf.Clamp(tabIndex, 0, ActiveTabs.Count - 1);
        if (ActiveTabBar != null) ActiveTabBar.SetActive(true);

        watchedHealth = ActivePlayer.Exists ? ActivePlayer.Go.GetComponent<IHealth>() : null;
        lastHp = watchedHealth != null ? watchedHealth.Current : 0f;

        if (ActiveTabs[Current].useCloseUp) BeginCloseUp();
        ShowScreen(Current);
        UpdateTabBar();
    }

    public void CloseMenu()
    {
        if (!open) return;
        HideScreen(Current);
        if (closeUpActive) EndCloseUp();
        if (ActiveTabBar != null) ActiveTabBar.SetActive(false);
        open = false;
    }

    public void SwitchTo(int index)
    {
        if (!open || index == Current || index < 0 || index >= ActiveTabs.Count) return;
        HideScreen(Current);
        // クローズアップの要否が変わる時だけカメラを触る（装備⇔スキルは触らない＝一瞬で切替）。
        if (ActiveTabs[index].useCloseUp && !closeUpActive) BeginCloseUp();
        else if (!ActiveTabs[index].useCloseUp && closeUpActive) EndCloseUp();
        Current = index;
        ShowScreen(Current);
        UpdateTabBar();
    }

    private void ShowScreen(int i) => (ActiveTabs[i].screen as IMenuTab)?.TabShow();
    private void HideScreen(int i) => (ActiveTabs[i].screen as IMenuTab)?.TabHide();

    private void UpdateTabBar()
    {
        for (int i = 0; i < ActiveTabs.Count; i++)
        {
            if (ActiveTabs[i].background != null)
                ActiveTabs[i].background.color = i == Current ? activeTabColor : inactiveTabColor;
        }
    }

    // --- クローズアップ（旧・装備UI/P画面/進化画面に重複していたカメラ管理の一本化） ---

    private void BeginCloseUp()
    {
        float dist = demonMode ? demonCloseUpDistance : closeUpDistance;
        float pitch = demonMode ? demonCloseUpPitch : closeUpPitch;
        float height = demonMode ? demonCloseUpHeight : closeUpHeight;
        float viewX = demonMode ? demonCloseUpViewX : closeUpViewX;
        float viewWidth = demonMode ? demonCloseUpViewWidth : closeUpViewWidth;

        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam != null)
        {
            savedCamRect = cam.rect;
            savedFarClip = cam.farClipPlane;
            savedClearFlags = cam.clearFlags;
            savedBgColor = cam.backgroundColor;
            cam.rect = new Rect(viewX, 0f, viewWidth, 1f);
            cam.farClipPlane = closeUpFarClip;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            savedCullingMask = cam.cullingMask;
            cullingMaskSaved = true;
            cam.cullingMask = CloseUpIsolator.Mask;
        }
        if (tpsCamera != null) tpsCamera.BeginCloseUp(dist, pitch, height);
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
