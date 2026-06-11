// 保存先: Assets/Scripts/Player/EquipmentUIController.cs
// 装備UI（C画面）＝統合メニュー（TabMenuController）の「装備」タブ。
//   中身の本体は部位ホットスポット（EquipmentHotspotView）と能力値/ワザ（EquipmentStatsView）。
//   ここはパネルの表示と「自分の瓶を右1/3で一緒に開く」（取り出し→自動装備の動線）だけを担う。
//   カメラ（クローズアップ）・キー入力・画面の相互排他は TabMenuController が一元管理する。
//   ※ 旧・中央カラム（スロット枠列＋RenderTexture表示）と専用カメラ運用、C/P/M切替キー処理、
//     フラッシュ演出は統合メニュー化（2026-06-12）で廃止・削除した。
using UnityEngine;

public class EquipmentUIController : MonoBehaviour, IMenuTab
{
    [SerializeField] private GameObject panel;            // 装備UIのパネル（開いている間だけ表示）
    [SerializeField] private BottleUIController bottleUI; // 装備タブと一緒に開く自分の瓶（右側・漁って装備）

    private bool open;
    public bool IsOpen => open;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // --- IMenuTab（TabMenuControllerから呼ばれる） ---

    public void TabShow()
    {
        if (open) return;
        open = true;
        if (panel != null) panel.SetActive(true);
        if (bottleUI != null)
        {
            bottleUI.SetRightHalf(true); // 装備画面と並べるため瓶を右1/3に
            bottleUI.OpenBottle();       // 自分の瓶も一緒に開く（取り出して自動装備）
        }
    }

    public void TabHide()
    {
        if (!open) return;
        open = false;
        if (panel != null) panel.SetActive(false);
        if (bottleUI != null) bottleUI.CloseBottle();
    }
}
