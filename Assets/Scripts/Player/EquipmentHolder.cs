// 保存先: Assets/Scripts/Player/EquipmentHolder.cs
// プレイヤーの装備を管理する。スロットごとに装備中のItemDataを保持し、脱着を扱う。
//   両手武器は右手＋左手を占有する（排他処理あり）。外した装備は自分の瓶(InventoryHolder.pending)に戻す。
//   ステータス反映は段階2、見た目は段階3。ここは脱着のデータ操作のみ。
using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentHolder : MonoBehaviour
{
    [SerializeField] private InventoryHolder playerHolder; // 外した装備の戻し先（自分の瓶）
    [SerializeField] private BottleUIController bottleUI;  // 瓶が開いているかの判定（開いていれば物理で即入れる）
    [SerializeField] private ItemPicker itemPicker;        // 物理で瓶に入れる（PutIntoBottle）

    private readonly Dictionary<EquipmentSlot, ItemData> equipped = new Dictionary<EquipmentSlot, ItemData>();

    // 装備が変わったら発火（PlayerCombatCore=武器/防御・PlayerMovement=速度が購読して再計算）。
    public event Action OnEquipmentChanged;

    // スロットの装備を取得（無ければnull）。
    public ItemData Get(EquipmentSlot slot)
    {
        return equipped.TryGetValue(slot, out var item) ? item : null;
    }

    // 装備する。占有スロットに居る既存装備は外して瓶へ戻す（両手武器の排他もここで処理）。
    public void Equip(ItemData item)
    {
        if (item == null || item.Equipment == null) return;

        var slots = SlotsFor(item.Equipment.EquipType);

        // 占有するスロットに居る既存装備を、それぞれ丸ごと外す（両手武器なら両スロットが外れる）。
        foreach (var slot in slots)
        {
            if (equipped.TryGetValue(slot, out var existing) && existing != null)
                Unequip(slot);
        }

        // 新装備を占有スロットに登録。
        foreach (var slot in slots)
            equipped[slot] = item;

        OnEquipmentChanged?.Invoke();
    }

    // 外す。その装備が占有する全スロットからクリアし、自分の瓶へ戻す。
    public void Unequip(EquipmentSlot slot)
    {
        if (!equipped.TryGetValue(slot, out var item) || item == null) return;

        foreach (var s in SlotsFor(item.Equipment.EquipType))
            equipped.Remove(s);

        // 瓶が今開いていて自分の瓶を表示中なら、物理で口から落として即反映する（開き直し不要）。
        //   閉じていれば pending に溜める（次に瓶を開いた時に積まれる）。
        if (bottleUI != null && bottleUI.IsOpen && bottleUI.CurrentHolder == playerHolder && itemPicker != null)
            itemPicker.PutIntoBottle(item);
        else if (playerHolder != null)
            playerHolder.PendingItems.Add(item);

        OnEquipmentChanged?.Invoke();
    }

    // テスト用：全スロットを外す（段階1のH キー）。
    public void UnequipAll()
    {
        foreach (var slot in new List<EquipmentSlot>(equipped.Keys))
            Unequip(slot);
    }

    // 装備中の防御力補正の合計（両手武器の重複は除外）。
    public float TotalDefenseBonus => SumBonus(eq => eq.DefenseBonus);
    // 装備中の移動速度補正の合計（両手武器の重複は除外）。
    public float TotalMoveSpeedBonus => SumBonus(eq => eq.MoveSpeedBonus);

    private float SumBonus(Func<EquipmentData, float> selector)
    {
        float sum = 0f;
        var seen = new HashSet<ItemData>();
        foreach (var item in equipped.Values)
        {
            if (item == null || item.Equipment == null) continue;
            if (!seen.Add(item)) continue; // 両手武器は2スロットに居るので一度だけ数える
            sum += selector(item.Equipment);
        }
        return sum;
    }

    // 右手の武器の技セット（武器を装備していなければnull＝攻撃不可）。
    public AttackData GetWeaponAttack()
    {
        var item = Get(EquipmentSlot.RightHand);
        return (item != null && item.Equipment != null) ? item.Equipment.WeaponAttack : null;
    }

    // 装備種別 → 占有スロット。
    private static EquipmentSlot[] SlotsFor(ItemEquipType type)
    {
        switch (type)
        {
            case ItemEquipType.OneHandWeapon: return new[] { EquipmentSlot.RightHand };
            case ItemEquipType.TwoHandWeapon: return new[] { EquipmentSlot.RightHand, EquipmentSlot.LeftHand };
            case ItemEquipType.Shield:        return new[] { EquipmentSlot.LeftHand };
            case ItemEquipType.Head:          return new[] { EquipmentSlot.Head };
            case ItemEquipType.Armor:         return new[] { EquipmentSlot.Armor };
            case ItemEquipType.Feet:          return new[] { EquipmentSlot.Feet };
            default:                          return new EquipmentSlot[0];
        }
    }
}
