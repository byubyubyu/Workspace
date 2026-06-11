// 保存先: Assets/Scripts/Demon/Devourer.cs
// 捕食（魔族プレイヤー専用）。死体の近くでFキー→むしゃむしゃ食べ始め、食べている時間ぶんポイントが増える。
//   ・食べている間は毎秒 pointsPerSecond を DevourPool に加算（リスク＝食べている間は隙だらけ）。
//   ・死体1体ぶん（nutritionPerCorpse）を食べ尽くすと：中身のアイテムを周囲にばらまき、死体は消滅。
//   ・死体は腐る（Corpseの寿命30秒）ので、食べきる前に消えることもある（肉が腐るイメージ）。
//   ・中断：再度F／移動／攻撃／死亡／死体から離れる。食べかけの量は死体ごとに記憶（戻れば続きから）。
//   ・Eの「漁る」は従来通り別操作（漁ってから食べる・食べてばらまかれた物を拾う、どちらも可能）。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(DevourPool))]
[RequireComponent(typeof(DemonCore))]
public class Devourer : MonoBehaviour
{
    // 捕食の通知（食べた死体・食べた肉量）。魂ポイント（DemonSoul）等が購読する。
    //   Devourer自身は魂システムを知らない（イベントで疎結合）。
    public event System.Action<Corpse, float> OnDevoured;

    [SerializeField] private Key devourKey = Key.F;        // 捕食の開始/中断キー
    [SerializeField] private float range = 2.5f;           // 食べられる距離（開始判定）
    [SerializeField] private float breakRange = 3.5f;      // これ以上離れたら中断（開始よりゆるめ＝ちらつき防止）
    [SerializeField] private float pointsPerSecond = 10f;  // 食べている間の毎秒ポイント
    [SerializeField] private float nutritionPerCorpse = 30f; // 死体1体ぶんの肉量（食べ尽くしで消滅）
    [SerializeField] private float moveBreakDistance = 0.3f; // 食べ始めからこれ以上動いたら中断
    [SerializeField] private MapItemFactory mapItemFactory;  // 食べ尽くした時のばらまき用
    [SerializeField] private float scatterRadius = 1.2f;     // ばらまきの散らばり半径
    [SerializeField] private float spawnHeight = 1f;         // ばらまきの落下開始高さ

    private DevourPool pool;
    private DemonCore core;
    private Attack attack;
    private Corpse target;          // 食べている死体（null=食べていない）
    private float eaten;            // 今の死体から食べた量
    private Vector3 eatStartPos;    // 食べ始めの自分の位置（移動中断の基準）
    private readonly Dictionary<Corpse, float> eatenMap = new Dictionary<Corpse, float>(); // 食べかけの記憶

    public bool IsEating => target != null;
    public Corpse Target => target; // 食べている死体（モーション等が読む。null=食べていない）

    private void Awake()
    {
        pool = GetComponent<DevourPool>();
        core = GetComponent<DemonCore>();
        attack = GetComponent<Attack>();
    }

    private void Update()
    {
        if (core.IsDead) { StopEating(); return; }

        var kb = Keyboard.current;
        if (kb != null && kb[devourKey].wasPressedThisFrame)
        {
            if (IsEating) StopEating();
            else TryStartEating();
        }

        if (!IsEating) return;

        // 中断条件：死体が消えた（腐った）／離れた／動いた／攻撃した。
        if (target == null) { StopEating(); return; }
        if ((target.transform.position - transform.position).sqrMagnitude > breakRange * breakRange) { StopEating(); return; }
        if ((transform.position - eatStartPos).sqrMagnitude > moveBreakDistance * moveBreakDistance) { StopEating(); return; }
        if (attack != null && attack.IsAttacking) { StopEating(); return; }

        // むしゃむしゃ（毎秒加算）。
        float bite = pointsPerSecond * Time.deltaTime;
        pool.Add(bite);
        eaten += bite;
        eatenMap[target] = eaten;
        OnDevoured?.Invoke(target, bite); // 食べたぶんを通知（魂ポイント等の獲得源）

        // 食べ尽くした：中身をばらまいて死体を消す。
        if (eaten >= nutritionPerCorpse)
        {
            SpillContents(target);
            eatenMap.Remove(target);
            Destroy(target.gameObject);
            StopEating();
            Debug.Log("[Devourer] 食べ尽くした（中身はばらまき）");
        }
    }

    // 範囲内の最寄りの死体を探して食べ始める。食べかけなら続きから。
    //   検索は自己申告レジストリ（Corpse.All）から＝物理検索を使わない（自分の部位コライダーが混ざらない）。
    private void TryStartEating()
    {
        Corpse nearest = NearestFinder.Find(Corpse.All, transform.position, range);
        if (nearest == null) return;

        target = nearest;
        eatenMap.TryGetValue(nearest, out eaten); // 食べかけなら続きから（無ければ0）
        eatStartPos = transform.position;
        Debug.Log($"[Devourer] 捕食開始（残り肉量 {nutritionPerCorpse - eaten:F0}）");
    }

    private void StopEating()
    {
        target = null;
    }

    // 死体の中身（瓶の記録＋未投入分）をマップにばらまく（ItemPicker.DropToMapと同じ流儀）。
    private void SpillContents(Corpse corpse)
    {
        if (mapItemFactory == null) return;
        var holder = corpse.GetComponent<InventoryHolder>();
        if (holder == null) return;

        var all = new List<ItemData>();
        foreach (var r in holder.Records) if (r.data != null) all.Add(r.data);
        foreach (var p in holder.PendingItems) if (p != null) all.Add(p);

        foreach (var data in all)
        {
            Vector2 r = Random.insideUnitCircle * scatterRadius;
            Vector3 pos = corpse.transform.position + new Vector3(r.x, spawnHeight, r.y);
            var core = mapItemFactory.Create(data, pos);
            if (core != null) core.Initialize(data);
        }
    }
}
