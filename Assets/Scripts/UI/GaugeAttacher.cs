// 保存先: Assets/Scripts/UI/GaugeAttacher.cs
using System.Collections.Generic;
using UnityEngine;

// ゲージを出したいオブジェクト（兵士・建物）に付けるだけで、
// StatBar Prefab を頭上に自動生成・配置する。種別リスト分を縦に並べる。
//
// 前提:
//   ・同じGameObjectに、対応する供給元(IGaugeSource)が付いていること
//       Hp    -> HpGaugeSource（IHealthを実装したCoreが必要）
//       Build -> BuildGaugeSource（Constructionが必要）
//   ・statBarPrefab は Canvas(World Space)＋Frame＋Fill＋StatBar を組んだPrefab（Unity側で作成）
//   ・生成したPrefabは自分の子にするので、StatBarが親方向のIGaugeSourceを解決できる
public class GaugeAttacher : MonoBehaviour
{
    [SerializeField] private StatBar statBarPrefab;          // 頭上に出すゲージPrefab
    [SerializeField] private List<GaugeType> gauges = new List<GaugeType> { GaugeType.Hp }; // 出す種別
    [SerializeField] private float baseHeight = 2.0f;        // 頭上の高さ（一番下のゲージ）
    [SerializeField] private float spacing = 0.25f;          // ゲージ同士の縦間隔

    private void Start()
    {
        if (statBarPrefab == null) return;

        for (int i = 0; i < gauges.Count; i++)
        {
            // 自分の子として生成（StatBarが親のIGaugeSourceを解決できるように）
            StatBar bar = Instantiate(statBarPrefab, transform);
            bar.transform.localPosition = new Vector3(0f, baseHeight + spacing * i, 0f);
            bar.SetGaugeType(gauges[i]);
        }
    }
}
