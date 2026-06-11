// 保存先: Assets/Scripts/Player/LineageData.cs
// 家系（世代交代・遺伝）の設定SO（GDDセクション15・人間側）。
//   遺伝式の数値はすべてここ：子の遺伝値 = (父の実効値＋母の個体値) ÷ 2 × inheritDecay。
//   減衰率が「血筋の最大恩恵」を決める単一ノブ（遺伝値の理論上限＝スキル上限×減衰率）。
using UnityEngine;

[CreateAssetMenu(fileName = "LineageData", menuName = "Project/Player/LineageData")]
public class LineageData : ScriptableObject
{
    public float inheritDecay = 0.7f; // 遺伝の減衰率（両親の平均に掛ける。必ず強い方の親より下がる）
    public float childStartAge = 15f; // 乗り移った子の開始年齢

    [Header("結納金（求婚のコイン代。個体値が高い相手ほど高い）")]
    public float bridePriceBase = 5f;       // 基本額（コイン額面の合計）
    public float bridePricePerPoint = 0.1f; // 相手の個体値合計1あたりの上乗せ
}
