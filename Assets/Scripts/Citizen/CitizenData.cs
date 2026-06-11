// 保存先: Assets/Scripts/Citizen/CitizenData.cs
// 市民の種類SO（にぎやかし市民・将来は商人など）。徘徊速度と見た目prefabを持つ。
//   兵士のMinionDataに相当する市民版（戦闘要素を持たない簡略版）。
using UnityEngine;

[CreateAssetMenu(fileName = "CitizenData", menuName = "Project/Citizen/CitizenData")]
public class CitizenData : ScriptableObject
{
    [SerializeField] private float moveSpeed = 2f;      // 徘徊の移動速度
    [SerializeField] private float wanderInterval = 3f; // 次の目的地を決め直す間隔（到着 or この秒数で）
    [SerializeField] private GameObject prefab;
    [SerializeField] private CitizenSkillData skillData; // スキル個体値の生成ルール（null=個体値なし。婚活・遺伝用）

    public float MoveSpeed => moveSpeed;
    public float WanderInterval => wanderInterval;
    public GameObject Prefab => prefab;
    public CitizenSkillData SkillData => skillData;
}
