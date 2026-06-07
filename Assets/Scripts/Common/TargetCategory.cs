// 保存先: Assets/Scripts/Common/TargetCategory.cs （新規ファイル）
public enum TargetCategory
{
    Minion,   // 敵兵士（優先度1位）
    Player,   // プレイヤー（優先度2位）。敵Teamのプレイヤーを攻撃対象にする
    Building,  // 敵建物（優先度3位）
}
