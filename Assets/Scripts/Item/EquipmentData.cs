// 保存先: Assets/Scripts/Item/EquipmentData.cs
// 装備品のデータ（ItemDataが参照するサブSO・null可＝非装備品）。
//   装備種別(equipType)で占有スロットが決まる。ステータス補正と、武器なら技セット(weaponAttack)を持つ。
//   ステータス反映は段階2、見た目は段階3。ここは値の入れ物のみ。
using UnityEngine;

[CreateAssetMenu(fileName = "EquipmentData", menuName = "Project/Item/EquipmentData")]
public class EquipmentData : ScriptableObject
{
    [SerializeField] private ItemEquipType equipType;
    [SerializeField] private float defenseBonus;      // 防御力加算（防具用）
    [SerializeField] private float moveSpeedBonus;    // 移動速度加算（テスト体感用・将来も使える）
    [SerializeField] private AttackData weaponAttack; // 武器の技セット（武器を装備すると攻撃がこれに変わる。武器以外はnull）

    public ItemEquipType EquipType => equipType;
    public float DefenseBonus => defenseBonus;
    public float MoveSpeedBonus => moveSpeedBonus;
    public AttackData WeaponAttack => weaponAttack;
}
