// 保存先: Assets/Scripts/Demon/DevourPool.cs
// 捕食ポイント（進化リソース）。肉体に貯める数値で、Resourceをラップする（HP/スタミナと同じ構造）。
//   ・Devourer（捕食）が加算し、DemonCore.Evolve（進化）が消費する。
//   ・死亡で全喪失（DemonCoreのリスポーン処理がClearを呼ぶ）。
//   ・表示は進化画面（EvolutionUIController）内のみ（頭上ゲージには出さない）。
using UnityEngine;

public class DevourPool : MonoBehaviour
{
    [SerializeField] private float max = 100f; // 肉体に貯められる上限

    private Resource pool;

    public float Current => pool != null ? pool.Current : 0f;
    public float Max => pool != null ? pool.Max : 0f;

    private void Awake()
    {
        pool = new Resource(max, 0f);
    }

    public void Add(float amount) => pool?.Add(amount);
    public bool CanAfford(float amount) => pool != null && pool.CanAfford(amount);
    public bool Consume(float amount) => pool != null && pool.Consume(amount);

    // 死亡ペナルティ：貯めたポイントを全て失う。
    public void Clear() => pool?.Add(-Current);
}
