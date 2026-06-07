// 保存先: Assets/Scripts/Player/HandState.cs
// プレイヤーの手の状態。PlayerHandStateが保持し、左クリックの意味や移動速度に影響する。
//   Empty   … 手ぶら・武器しまい。攻撃不可。移動は通常速度
//   Drawing … 抜刀中（構える途中の隙）。攻撃不可。移動は遅い。走り/回避でキャンセルしてEmptyへ
//   Weapon  … 武器構え。攻撃可能。移動速度ダウン
//   Item    … アイテム所持。攻撃不可（使うことのデメリット）。左クリックで使う
public enum HandState
{
    Empty,
    Drawing,
    Weapon,
    Item,
}
