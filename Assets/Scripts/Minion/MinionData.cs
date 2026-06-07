using System.Collections.Generic;
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
    [SerializeField] private DodgeData dodge;
    [SerializeField] private float productionCost; // 直接フィールド（特定コンポーネントに属さないメタ情報）
    [SerializeField] private GameObject prefab;
    [SerializeField] private List<ItemData> initialItems = new List<ItemData>(); // 生成時に瓶へ入れる初期アイテム（死体を漁る用）

    public VitalityData Vitality => vitality;
    public MovementData Movement => movement;
    public AttackData Attack => attack;
    public VisionData Vision => vision;
    public BuilderData Builder => builder;
    public StaminaData Stamina => stamina;
    public DodgeData Dodge => dodge;
    public float ProductionCost => productionCost;
    public GameObject Prefab => prefab;
    public List<ItemData> InitialItems => initialItems;
}
