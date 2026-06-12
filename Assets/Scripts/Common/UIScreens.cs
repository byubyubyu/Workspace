// 保存先: Assets/Scripts/Common/UIScreens.cs
// 画面系UIの開閉状態の一覧（静的ヘルパー・MonoBehaviourではない）。
//   「今なにかUIが開いているか」の判定が PlayerHandState / DemonInputController /
//   TabMenuController / CitizenProfileUIController 等に重複していたのを一本化した。
//   各UIのInstance（シーンに1個流儀）を読むだけで、状態は持たない。
public static class UIScreens
{
    // --- 個別の開閉状態 ---
    public static bool MenuOpen => TabMenuController.Instance != null && TabMenuController.Instance.IsOpen;          // 統合メニュー（装備/スキル/マップ/瓶・魔族=進化/マップ/瓶）
    public static bool BottleOpen => BottleUIController.Instance != null && BottleUIController.Instance.IsOpen;      // 瓶（メニューの瓶タブ・直接I・死体漁りを含む）
    public static bool MerchantOpen => MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen; // 商人売買
    public static bool MinimapOpen => MinimapController.Instance != null && MinimapController.Instance.IsOpen;        // マップ（メニューのタブとして開く）
    public static bool StatusOpen => StatusUIController.Instance != null && StatusUIController.Instance.IsOpen;       // スキル/ステータス（メニューのタブとして開く）
    public static bool ProfileOpen => CitizenProfileUIController.Instance != null && CitizenProfileUIController.Instance.IsOpen; // 市民プロフィール（婚活）

    // 画面系UIのどれかが開いているか＝左クリック（攻撃/使用/技）をワールドへ通さない判定。
    public static bool AnyBlocking => MenuOpen || BottleOpen || MerchantOpen || MinimapOpen || StatusOpen || ProfileOpen;

    // 状況起動UI（商人・市民プロフィール）が開いているか＝統合メニューを出さない判定。
    public static bool SituationalOpen => MerchantOpen || ProfileOpen;
}
