// 保存先: Assets/Scripts/Player/PlayerCorpseDropper.cs
// 人間プレイヤーの死亡ドロップ（GDDセクション15：死＝復活するが装備・持ち物を落とす）。
//   PlayerCombatCore.OnDied を購読するだけ（Coreは落とし方を知らない＝DeathPoof/CorpseSpawnerと同じObserver流儀）。
//   流れ：装備を全部外す（瓶へ戻る）→ 兵士と同じ死体（Corpse）を生成し、瓶の中身を丸ごと移譲 → 自分の瓶は空に。
//   死体には自分のTeamを記録（魔族に食べられたら人間倍率の魂ポイントになる＝既存の出自判定に乗る）。
//   自分の死体をEで漁って回収する、は既存のCorpse/死体漁りがそのまま使える。
using UnityEngine;

[RequireComponent(typeof(PlayerCombatCore))]
public class PlayerCorpseDropper : MonoBehaviour
{
    [SerializeField] private GameObject corpsePrefab; // 兵士と同じ死体prefab（InventoryHolder＋Corpse＋タグ"Corpse"）

    private PlayerCombatCore core;
    private InventoryHolder holder;
    private EquipmentHolder equipment;

    private void Awake()
    {
        core = GetComponent<PlayerCombatCore>();
        holder = GetComponent<InventoryHolder>();
        equipment = GetComponent<EquipmentHolder>();
        core.OnDied += Drop;
    }

    private void OnDestroy()
    {
        if (core != null) core.OnDied -= Drop;
    }

    private void Drop()
    {
        // 装備を全部外す（既存の流儀どおり瓶＝records/pendingへ戻る）→ 持ち物と一緒に死体へ移す。
        if (equipment != null) equipment.UnequipAll();

        if (corpsePrefab == null)
        {
            Debug.LogWarning($"[PlayerCorpseDropper] corpsePrefab未設定（ドロップなしで復活）: {name}");
            return;
        }

        var obj = Instantiate(corpsePrefab, transform.position, transform.rotation);
        var corpseHolder = obj.GetComponent<InventoryHolder>();
        if (corpseHolder != null && holder != null) corpseHolder.CopyFrom(holder);
        var corpse = obj.GetComponent<Corpse>();
        if (corpse != null) corpse.SetSource(core.Team);

        // 自分の瓶は空になる（落とした物は死体から回収する）。
        if (holder != null)
        {
            holder.Records.Clear();
            holder.PendingItems.Clear();
        }
        Debug.Log("[PlayerCorpseDropper] 死亡ドロップ：装備・持ち物を死体に落とした");
    }
}
