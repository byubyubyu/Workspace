using UnityEngine;
using UnityEngine.UI;

// 装備UI（C画面）の能力値パネル。開いている間、現在の実効値を表示する。
//   装備の脱着で値が変わる様子をその場で見せるのが目的（表示のみ・ロジックなし）。
//   ラベル列＋数値列の2テキストで桁を揃える（P画面と同じ流儀）。
public class EquipmentStatsView : MonoBehaviour
{
    [SerializeField] private EquipmentUIController controller; // 開閉状態の参照
    [SerializeField] private PlayerCombatCore core;
    [SerializeField] private Stamina stamina;
    [SerializeField] private EquipmentHolder equipmentHolder;
    [SerializeField] private Text labelColumn;   // 項目名（左寄せ）
    [SerializeField] private Text valueColumn;   // 数値（右寄せ）
    [SerializeField] private Text movesLabel;    // 武器のワザ一覧（任意）

    private void Update()
    {
        if (controller == null || !controller.IsOpen || core == null) return;

        if (labelColumn != null) labelColumn.text = "HP\nスタミナ\n攻撃\n防御\n軽減\n移動補正";
        if (valueColumn != null)
        {
            float moveBonus = equipmentHolder != null ? equipmentHolder.TotalMoveSpeedBonus : 0f;
            string atk = core.AttackPower > 0f ? $"{core.AttackPower:F0}" : "－";
            valueColumn.text =
                $"{core.Current:F0} / {core.Max:F0}\n" +
                $"{(stamina != null ? stamina.Current : 0f):F0} / {(stamina != null ? stamina.Max : 0f):F0}\n" +
                $"{atk}\n" +
                $"{core.Defense:F0}\n" +
                $"{core.DamageCut:F1}\n" +
                $"{(moveBonus >= 0 ? "+" : "")}{moveBonus:F1}";
        }

        // 武器のワザ一覧（P画面と同じDisplayName表示）。
        if (movesLabel != null)
        {
            var weapon = equipmentHolder != null ? equipmentHolder.GetWeaponAttack() : null;
            if (weapon == null || weapon.moves == null || weapon.moves.Count == 0)
            {
                movesLabel.text = "（武器なし）";
            }
            else
            {
                var names = new System.Collections.Generic.List<string>();
                foreach (var m in weapon.moves)
                    if (m != null) names.Add(m.DisplayName);
                movesLabel.text = string.Join("\n", names);
            }
        }
    }
}
