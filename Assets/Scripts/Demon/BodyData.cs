// 保存先: Assets/Scripts/Demon/BodyData.cs
// 魔族プレイヤーの「素体」SO（MonsterFormDataの後継）。
//   素体＝器。体の骨格prefab・基礎ステータス・部位スロットの定義・部位の進化上限を持つ。
//   部位（PartData）＝生きている間に捕食ポイントで育てる軸。素体の基礎値に補正を加算し、ワザを付与する。
//   段階3（将来）：死亡時に条件で次の素体が決まる（脱皮）。進化上限（maxPartTier）はそこで素体ごとに差をつける。
//   マルチプレイ方針：SOは全クライアント同梱の静的データ。実行時は番号（スロット番号・候補番号）で扱う。
using System.Collections.Generic;
using UnityEngine;

// 部位スロット1つぶんの定義。脚のように同じ部位を複数アンカーで共有する場合はpartObjectNamesに並べる。
[System.Serializable]
public class BodySlot
{
    public string slotName;                  // 表示名（頭・前脚など。進化UIで使う）
    public List<string> partObjectNames = new List<string>(); // bodyPrefab内の部位オブジェクト名（例：Leg_FL, Leg_FR）
    public PartData initialPart;             // 開始時（リスポーン時）の部位
}

[CreateAssetMenu(fileName = "BodyData", menuName = "Project/Demon/BodyData")]
public class BodyData : ScriptableObject
{
    [SerializeField] private string bodyName = "素体";   // 表示名
    [SerializeField] private GameObject bodyPrefab;       // 体（部位＋PartHurtbox＋Hitbox＋モーション入り）
    [SerializeField] private float scale = 1f;            // 体の大きさ（bodyPrefabに掛ける）

    [Header("基礎ステータス（部位の補正Σが加算される）")]
    [SerializeField] private VitalityData vitality;       // HP・防御の基礎値
    [SerializeField] private StaminaData stamina;         // スタミナ（上限・回復・遅延）
    [SerializeField] private float attackPower = 5f;      // 基礎攻撃力（実威力 = これ＋部位Σ × 技のpowerMultiplier）

    [Header("ひるみ（蓄積式）")]
    [SerializeField] private float staggerThreshold = 3f;
    [SerializeField] private float bigStaggerDuration = 1.5f;

    [Header("部位スロット")]
    [SerializeField] private int maxPartTier = 99;        // 部位の進化上限（素体ごとに差をつける）
    [SerializeField] private List<BodySlot> slots = new List<BodySlot>();

    [Header("転生（魂システム）")]
    [SerializeField] private float requiredSoulPoints = 0f; // この素体の解放に必要な魂ポイント（0=最初から選べる）

    public string BodyName => bodyName;
    public GameObject BodyPrefab => bodyPrefab;
    public float Scale => scale;
    public VitalityData Vitality => vitality;
    public StaminaData Stamina => stamina;
    public float AttackPower => attackPower;
    public float StaggerThreshold => staggerThreshold;
    public float BigStaggerDuration => bigStaggerDuration;
    public int MaxPartTier => maxPartTier;
    public IReadOnlyList<BodySlot> Slots => slots;
    public float RequiredSoulPoints => requiredSoulPoints;
}
