// 保存先: Assets/Scripts/Item/IItemEffect.cs
// アイテムを使ったときの効果の差込口（最小形）。
//   今は Use() のみ。誰に・どこで効くか等の文脈は、効果の中身を実装するときに引数として足す。
//   実装は効果SO（ItemEffect を継承した具体効果）が担う。中身は将来。
public interface IItemEffect
{
    // アイテムが取り出された（使用された）瞬間に呼ばれる。
    void Use();
}
