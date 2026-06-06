// 保存先: Assets/Scripts/Item/ItemEffect.cs
// アイテム効果のSO土台（abstract）。具体効果（HealEffect 等）が将来これを継承する。
//   ItemDataが「効果SOを1つ」参照する（null可＝効果なしのアイテムも作れる）。
//   BuildStrategy → ManualBuildStrategy / AutoBuildStrategy と同じ「abstract SO＋具体実装で差し替え」構造。
//   ※ 中身は将来。今は差込口として形だけ用意する。
using UnityEngine;

public abstract class ItemEffect : ScriptableObject, IItemEffect
{
    public abstract void Use();
}
