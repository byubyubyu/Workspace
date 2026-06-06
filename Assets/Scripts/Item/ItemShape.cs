// 保存先: Assets/Scripts/Item/ItemShape.cs
// アイテムの形（物理カテゴリ）。瓶の中での積みやすさ・転がりやすさを決める。
//   生成時にこの種類を見て、対応するCollider2Dを付ける（Circle/Box）。
//   サイズ（寸法）はItemDataが別フィールドで持つ。将来、形を増やす場合はここに追加する。
public enum ItemShape
{
    Circle, // 丸（転がりやすい）
    Box,    // 四角（積みやすい）
    Long,   // 細長（倒れると場所を食う。Boxの細長い比率として扱う）
}
