// 保存先: Assets/Scripts/Demon/DemonCore.cs
// 魔族プレイヤーのCore（人間のPlayerCombatCore・兵士のMinionCoreに相当する魔族版）。
//   ・プレイヤー自身がモンスター系の生物＝魔族（GDDセクション13・14）。憑依・テイムではない。
//   ・体＝素体（BodyData）＋部位スロット（PartData）。進化は部位単位（EvolvePart）。
//   ・素体はカタログ（BodyCatalog）から番号で参照（マルチ方針：静的SOをIDで扱う）。
//   ・転生（GDDセクション15）：死亡→転生待ち→OnAwaitReincarnationをUIが購読して転生画面を開く→
//     Reincarnate(素体番号)で復活。解放判定は魂ポイント（DemonSoul）。CoreはUIを参照しない（疎結合）。
//   ・ワザ＝スロット順に各部位のgrantedMovesを連結（技番号が全クライアントで安定＝マルチ方針）。
//   ・ステータス＝素体の基礎値＋全部位の補正Σ（部位は魔族の「装備」相当）。
//   ・人間プレイヤーと違い、被ダメ実体を持つ（HP・防御・死亡→転生）。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Attack))]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(AccumulatedStagger))]
[RequireComponent(typeof(DevourPool))]
public class DemonCore : MonoBehaviour, IBattleInfo, IHealth
{
    [SerializeField] private Team team = Team.Red;            // 魔族陣営のTeam（テスト運用：Red）
    [SerializeField] private BodyCatalog catalog;             // 素体カタログ（番号=素体ID）
    [SerializeField] private int initialBodyIndex = 0;        // 開始時の素体（カタログ番号）
    [SerializeField] private Transform visualRoot;            // 体を生成する親（未設定なら自分直下に作る）
    [SerializeField] private float respawnDelay = 3f;         // 死亡から転生待ちまでの秒数（仮実装）
    [SerializeField] private float visualGroundOffset = -1f;  // 体の接地オフセット（transformはCCカプセル中心＝地面から約1m上のため）

    private Health health;
    private float defense;            // 実効防御力＝素体＋部位Σ（組み上げ時に確定）
    private Attack attack;
    private Stamina stamina;
    private AccumulatedStagger stagger;
    private DevourPool devourPool;    // 捕食ポイント（ラン内の進化リソース。死亡で全喪失）
    private DemonSoul soul;           // 魂ポイント（恒久。素体の解放判定。無くても動く＝必要pt0の素体のみ）
    private PlayerMovement movement;  // 死亡中の移動停止＋部位の移動速度補正の注入先
    private GameObject currentVisual;
    private int currentBodyIndex;
    private PartData[] equippedParts; // スロットごとの装着部位（実行時状態。SOは静的データ）
    private Vector3 spawnPosition;
    private bool dead;

    // --- IBattleInfo / IHealth ---
    public Vector3 Position => transform.position;
    public Team Team => team;
    public float Current => health != null ? health.Current : 0f;
    public float Max => health != null ? health.Max : 0f;

    public bool IsDead => dead;
    public bool IsStaggered => stagger != null && stagger.IsStaggered;
    public BodyCatalog Catalog => catalog;
    public int CurrentBodyIndex => currentBodyIndex;
    public BodyData Body =>
        catalog != null && currentBodyIndex >= 0 && currentBodyIndex < catalog.Bodies.Count
            ? catalog.Bodies[currentBodyIndex] : null;
    public DevourPool DevourPool => devourPool;
    // 実効ステータスの読み取り口（進化画面の現在値表示用。ApplyBodyで確定した値をそのまま公開＝一方向）。
    public float Defense => defense;
    public float AttackPower { get; private set; }
    public float MoveSpeedBonus { get; private set; }
    public int SlotCount => equippedParts != null ? equippedParts.Length : 0;
    public PartData GetEquippedPart(int slotIndex) =>
        equippedParts != null && slotIndex >= 0 && slotIndex < equippedParts.Length ? equippedParts[slotIndex] : null;

    // --- 転生（魂システム） ---
    public bool AwaitingReincarnation { get; private set; }
    public event Action OnAwaitReincarnation; // 転生待ち開始の通知（転生UIが購読して画面を開く）

    private void Awake()
    {
        attack = GetComponent<Attack>();
        stamina = GetComponent<Stamina>();
        stagger = GetComponent<AccumulatedStagger>();
        devourPool = GetComponent<DevourPool>();
        soul = GetComponent<DemonSoul>();
        movement = GetComponent<PlayerMovement>();
        if (visualRoot == null)
        {
            var root = new GameObject("VisualRoot");
            root.transform.SetParent(transform, false);
            visualRoot = root.transform;
        }
        // 足元を地面に合わせる（CharacterControllerのカプセル下端へ）。
        visualRoot.localPosition = new Vector3(0f, visualGroundOffset, 0f);
    }

    private void Start()
    {
        spawnPosition = transform.position;
        if (catalog == null || catalog.Bodies.Count == 0)
        {
            Debug.LogError($"[DemonCore] catalog（素体カタログ）が未設定です: {name}");
            return;
        }
        currentBodyIndex = Mathf.Clamp(initialBodyIndex, 0, catalog.Bodies.Count - 1);
        ResetParts();
        ApplyBody();
    }

    // 素体が解放済みか（必要魂ポイントに到達しているか）。
    public bool IsBodyUnlocked(int bodyIndex)
    {
        if (catalog == null || bodyIndex < 0 || bodyIndex >= catalog.Bodies.Count) return false;
        var body = catalog.Bodies[bodyIndex];
        if (body == null) return false;
        if (body.RequiredSoulPoints <= 0f) return true;
        return soul != null && soul.IsUnlocked(body);
    }

    // 全スロットを素体の初期部位に戻す（開始時・転生時の死亡ペナルティ）。
    private void ResetParts()
    {
        var slots = Body.Slots;
        equippedParts = new PartData[slots.Count];
        for (int i = 0; i < slots.Count; i++)
            equippedParts[i] = slots[i].initialPart;
    }

    // 素体＋装着部位から体を組み上げる（初期化・部位進化・転生共通）。HPは全回復する。
    public void ApplyBody()
    {
        var body = Body;
        if (body == null || equippedParts == null) return;

        // 見た目の差し替え。古い体は親から外してから破棄する
        //   （Destroyは遅延のため、外さないと同フレームのGetComponentInChildrenが古いHitbox等を拾う）。
        if (currentVisual != null)
        {
            currentVisual.transform.SetParent(null);
            Destroy(currentVisual);
        }
        currentVisual = Instantiate(body.BodyPrefab, visualRoot); // 素体骨格（部位アンカーのみ）
        visualRoot.localScale = Vector3.one * body.Scale;
        AssembleParts(); // アンカーへ装着部位のprefabを取り付ける（このあとのAttack初期化が新Hitboxを拾う）

        // 部位の補正Σとワザの集約（部位＝魔族の装備。スロット順に連結＝技番号が安定）。
        float hpBonus = 0f, defenseBonus = 0f, powerBonus = 0f, speedBonus = 0f;
        var moves = new List<AttackMove>();
        for (int i = 0; i < equippedParts.Length; i++)
        {
            var part = equippedParts[i];
            if (part == null)
            {
                Debug.LogError($"[DemonCore] スロット{i}の部位が未設定: {name}");
                continue;
            }
            hpBonus += part.hpBonus;
            defenseBonus += part.defenseBonus;
            powerBonus += part.attackPowerBonus;
            speedBonus += part.moveSpeedBonus;
            foreach (var m in part.grantedMoves)
                if (m != null) moves.Add(m);
        }

        // ステータス（HP・防御）＝素体の基礎値＋部位Σ。組み上げ＝全回復。
        if (body.Vitality != null)
        {
            health = new Health(body.Vitality.hp + hpBonus);
            defense = body.Vitality.defense + defenseBonus;
        }
        else
        {
            Debug.LogError($"[DemonCore] 素体「{body.BodyName}」のVitalityData欠け: {name}");
            health = new Health(1f);
            defense = 0f;
        }

        // 実効値の公開用キャッシュ（進化画面が読む）。
        AttackPower = body.AttackPower + powerBonus;
        MoveSpeedBonus = speedBonus;

        // 部品へ注入（兵士のMinionCore.Initializeと同じ「押し込む」方式）。
        attack.Initialize(AttackPower, moves); // 新しい体のHitboxを拾い直す
        if (body.Stamina != null) stamina.Initialize(body.Stamina, team);
        else Debug.LogError($"[DemonCore] 素体「{body.BodyName}」のStaminaData欠け: {name}");
        stagger.Configure(body.StaggerThreshold, body.BigStaggerDuration);
        if (movement != null) movement.SetMoveSpeedBonus(speedBonus);

        // 新しい体のHurtbox（部位制なら複数）に自分を渡す（部位データは組み立て時に押し込み済み）。
        var hurtboxes = GetComponentsInChildren<Hurtbox>(true);
        foreach (var h in hurtboxes) h.SetOwner(this);
        if (hurtboxes.Length == 0) Debug.LogWarning($"[DemonCore] 素体「{body.BodyName}」の体にHurtboxがありません: {name}");

        Debug.Log($"[DemonCore] 体組み上げ: {body.BodyName}（技{moves.Count}個・HP{Max:F0}・防御{defense:F0}）");
    }

    // スロット定義に従い、素体骨格のアンカーへ装着中の部位prefabを生成して取り付ける（組み立て）。
    //   部位prefabが見た目・PartHurtbox・（前脚なら）Hitbox・Motionを持ち運ぶ＝進化で見た目とワザが一緒に変わる。
    private void AssembleParts()
    {
        var slots = Body.Slots;
        for (int i = 0; i < slots.Count && i < equippedParts.Length; i++)
        {
            var part = equippedParts[i];
            if (part == null || slots[i].partObjectNames == null) continue;
            if (part.partPrefab == null)
            {
                Debug.LogWarning($"[DemonCore] 部位「{part.partName}」にpartPrefabがありません（素体: {Body.BodyName}）");
                continue;
            }
            foreach (var anchorName in slots[i].partObjectNames)
            {
                var anchor = FindDeep(currentVisual.transform, anchorName);
                if (anchor == null)
                {
                    Debug.LogWarning($"[DemonCore] アンカーが見つからない: {anchorName}（素体: {Body.BodyName}）");
                    continue;
                }
                var instance = Instantiate(part.partPrefab, anchor, false);
                var ph = instance.GetComponentInChildren<PartHurtbox>(true);
                if (ph != null) ph.SetData(part);
                else Debug.LogWarning($"[DemonCore] 部位「{part.partName}」のprefabにPartHurtboxがありません");
            }
        }
    }

    // 名前で子孫Transformを探す（Transform.Findは直下のみのため再帰で）。
    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    // 部位を進化させる：スロット番号＋候補番号で指定（マルチ方針：送るのは番号だけで済む）。
    //   捕食ポイントをアトミックに消費→部位差し替え→体を組み直す（進化＝全回復の既存ルール）。
    public bool EvolvePart(int slotIndex, int optionIndex)
    {
        if (dead) return false;
        var current = GetEquippedPart(slotIndex);
        if (current == null) return false;
        var options = current.evolutions;
        if (options == null || optionIndex < 0 || optionIndex >= options.Count) return false;
        var option = options[optionIndex];
        if (option == null || option.target == null) return false;
        if (option.target.tier > Body.MaxPartTier) return false; // 素体の進化上限
        if (devourPool == null || !devourPool.Consume(option.cost)) return false; // 足りなければ不成立（アトミック）

        equippedParts[slotIndex] = option.target;
        ApplyBody();
        return true;
    }

    // --- 被ダメ実体（人間プレイヤーのnoopと違い、魔族は実装する） ---
    public void TakeDamage(BattleInfo info)
    {
        if (dead || health == null) return;
        float damage = DamageCalculator.Calc(info.attackPower, defense);
        health.TakeDamage(damage);
        if (info.staggerDuration > 0f) stagger.Apply(info.staggerDuration); // 蓄積式（閾値で大ひるみ）
        if (health.IsEmpty) StartCoroutine(DieRoutine());
    }

    // 死亡→転生待ち（死亡ペナルティ：捕食ポイント全喪失＋部位初期化。魂ポイントは残る。GDDセクション15）。
    //   転生待ちになったらOnAwaitReincarnationで通知し、転生UIの選択（Reincarnate）を待つ。
    private IEnumerator DieRoutine()
    {
        dead = true;
        attack.ForceCancel();
        if (movement != null) movement.enabled = false; // 死亡中は動けない
        if (currentVisual != null) currentVisual.SetActive(false);
        Debug.Log($"[DemonCore] 死亡。{respawnDelay}秒後に転生待ち（捕食pt喪失・部位初期化。魂ptは残る）");

        yield return new WaitForSeconds(respawnDelay);

        AwaitingReincarnation = true;
        OnAwaitReincarnation?.Invoke(); // 転生UIがこれを受けて素体選択画面を開く
    }

    // 転生する：解放済みの素体を番号で選んで復活（転生UIから呼ばれる。マルチ方針：番号で扱う）。
    public bool Reincarnate(int bodyIndex)
    {
        if (!AwaitingReincarnation) return false;
        if (!IsBodyUnlocked(bodyIndex)) return false;

        // CharacterControllerはテレポートと相性が悪いので、一時無効化して位置を戻す。
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = spawnPosition;
        if (cc != null) cc.enabled = true;

        if (devourPool != null) devourPool.Clear(); // 貯めた捕食ポイントは失う（転生の代価）
        currentBodyIndex = bodyIndex;
        ResetParts();                               // 部位も新しい素体の初期に戻る
        ApplyBody();                                // 組み直し＝全回復
        if (movement != null) movement.enabled = true;
        AwaitingReincarnation = false;
        dead = false;
        Debug.Log($"[DemonCore] 転生: {Body.BodyName}（素体ID {bodyIndex}）");
        return true;
    }
}
