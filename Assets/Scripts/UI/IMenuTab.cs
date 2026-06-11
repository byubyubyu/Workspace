// 保存先: Assets/Scripts/UI/IMenuTab.cs
// 統合タブメニュー（TabMenuController）に乗る画面の契約。
//   画面側は「自分のパネルの表示/非表示」だけを担当し、
//   カメラ（クローズアップ）・キー入力・相互排他はメニュー側が一元管理する。
public interface IMenuTab
{
    void TabShow(); // タブが選ばれた：自分のパネルを表示する（カメラは触らない）
    void TabHide(); // タブが外れた／メニューが閉じた：自分のパネルを隠す
}
