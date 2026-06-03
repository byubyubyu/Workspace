using UnityEngine;

[CreateAssetMenu(fileName = "MinionData", menuName = "Project/Minion/MinionData")]
public class MinionData : ScriptableObject, IMinionData
{
    [SerializeField] private VitalityData vitality;
    [SerializeField] private MovementData movement;
    [SerializeField] private AttackData attack;
    [SerializeField] private VisionData vision;
    [SerializeField] private BuilderData builder;
    [SerializeField] private StaminaData stamina;
    [SerializeField] private float productionCost; // 直接フィールド（特定コンポーネントに属さないメタ情報）
    [SerializeField] private GameObject prefab;

    public VitalityData Vitality => vitality;
    public MovementData Movement => movement;
    public AttackData Attack => attack;
    public VisionData Vision => vision;
    public BuilderData Builder => builder;
    public StaminaData Stamina => stamina;
    public float ProductionCost => productionCost;
    public GameObject Prefab => prefab;
}
