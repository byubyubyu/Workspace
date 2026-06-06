// 保存先: Assets/Scripts/Player/HandState.cs
// プレイヤーの手の状態。PlayerHandStateが保持し、左クリックの意味や移動速度（将来）に影響する。
//   Empty  … 手ぶら・武器しまい。攻撃不可。移動は通常速度
//   Weapon … 武器構え。攻撃可能。将来：移動速度ダウン（段階3c）
//   Item   … アイテム所持。攻撃不可（使うことのデメリット）。左クリックで使う
public enum HandState
{
    Empty,
    Weapon,
    Item,
}
