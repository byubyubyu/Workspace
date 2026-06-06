// 保存先: Assets/Scripts/Item/IItemSpawner.cs
// アイテムの供給役（差し替え可能）の共通窓口。
//   「マップにアイテムを生成する」共通処理(MapItemFactory)の上に、「いつ・どこに・何を出すか」を
//   決める供給役を載せる。InventorySystemが Spawn() を起動する。
//   実装：FixedItemSpawner（固定配置）。将来：撃破ドロップ・リスポーン型などを別実装で足す。
public interface IItemSpawner
{
    // 供給を開始する（固定配置なら「開始時に全部置く」）。
    void Spawn();
}
