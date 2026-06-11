// 保存先: Assets/Scripts/Minion/CorpseSpawner.cs
// 兵士が死んだ瞬間、死体（Corpse）を死亡位置に生成し、兵士の瓶の中身を移譲する。
//   兵士Prefabに付ける。MinionCore.OnDestroyed を購読するだけ（MinionCoreは変更しない＝DeathPoofと同じ疎結合）。
//   死体は別オブジェクトなので、兵士本体が即Destroyされても残る（一定時間後に自分で消える）。
//   中身が空でも死体は出す（現状の方針）。
using UnityEngine;

[RequireComponent(typeof(MinionCore))]
public class CorpseSpawner : MonoBehaviour
{
    [SerializeField] private GameObject corpsePrefab; // InventoryHolder＋Corpse＋Collider(タグ"Corpse")を持つ死体prefab

    [Header("重なり回避")]
    [SerializeField] private float corpseSpacing = 1f; // 死体同士の最小間隔（これ未満に既存死体があればずらす）
    [SerializeField] private float spreadRadius = 2f;  // ずらす時の探索範囲
    [SerializeField] private int maxTries = 8;         // 空き位置を探す試行回数

    private MinionCore core;
    private InventoryHolder holder; // 兵士自身の中身（死亡時に死体へ移す）

    private void Awake()
    {
        core = GetComponent<MinionCore>();
        holder = GetComponent<InventoryHolder>();
        if (core != null) core.OnDestroyed += SpawnCorpse;
    }

    private void OnDestroy()
    {
        if (core != null) core.OnDestroyed -= SpawnCorpse; // 念のため購読解除
    }

    // MinionCore.Die() が Destroy する直前に OnDestroyed で呼ばれる。
    private void SpawnCorpse()
    {
        if (corpsePrefab == null) return;

        // 既存の死体と重ならない位置を探して生成する。
        Vector3 pos = FindFreePosition(transform.position);
        GameObject obj = Instantiate(corpsePrefab, pos, transform.rotation);

        // 兵士の中身（records＋pendingItems）を死体へ移譲する（兵士にholderが無ければ空のまま）。
        var corpseHolder = obj.GetComponent<InventoryHolder>();
        if (corpseHolder != null && holder != null) corpseHolder.CopyFrom(holder);

        // 出自（Team）を死体に記録する（魂ポイントの倍率判定用。野生=None／兵士=国のTeam）。
        var corpse = obj.GetComponent<Corpse>();
        if (corpse != null) corpse.SetSource(core.Team);
    }

    // 死亡位置に既存の死体が重なっていたら、周囲の空いた位置を探して返す（見つからなければ元の位置で妥協）。
    private Vector3 FindFreePosition(Vector3 origin)
    {
        if (!HasCorpseNear(origin)) return origin;
        for (int i = 0; i < maxTries; i++)
        {
            Vector2 r = Random.insideUnitCircle * spreadRadius;
            Vector3 candidate = origin + new Vector3(r.x, 0f, r.y);
            if (!HasCorpseNear(candidate)) return candidate;
        }
        return origin;
    }

    // pos の周囲(corpseSpacing)に既存の死体があるか（自己申告レジストリ＝Corpse.Allから判定）。
    private bool HasCorpseNear(Vector3 pos)
    {
        return NearestFinder.Find(Corpse.All, pos, corpseSpacing) != null;
    }
}
