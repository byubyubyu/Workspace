// 保存先: Assets/Scripts/Player/PlayerCombatCore.cs
// プレイヤーの戦闘実体のCore（兵士のMinionCoreに相当するプレイヤー版）。
//   ・IBattleInfoを実装し、Attack(Hitbox)の所有者になる（敵味方判定に使われる）。
//   ・被ダメ実体あり（GDDセクション15で実体化。旧・案Bのnoopは廃止）：
//     HP・防御＝VitalityData(SO)の基礎値＋装備Σ＋スキルΣ。子のHurtboxが被弾の入口（Dodgeのi-frameが実機能になる）。
//   ・IHealth実装＝頭上ゲージ・Visionの「HP0は狙わない」ガードがそのまま効く。
//   ・死＝復活（魂は死なない・GDDセクション15）。OnDiedを発火するだけで、装備・持ち物のドロップは
//     PlayerCorpseDropperが購読して行う（Coreは落とし方を知らない＝疎結合）。
//   ・スキル（PlayerSkills）はOnChangedを購読して実効値を再計算する（Skills側はCoreを参照しない）。
//   ・プレイヤー用SOでAttack/Dodge/Staminaを初期化し、入力で発火する（動かす主体が入力＝兵士はAI）。
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Attack))]
[RequireComponent(typeof(Dodge))]
[RequireComponent(typeof(Stamina))]
public class PlayerCombatCore : MonoBehaviour, IBattleInfo, IHealth
{
    [SerializeField] private Team team = Team.Red;      // プレイヤーの所属（同Teamは殴らない）
    [SerializeField] private VitalityData vitality;     // HP・防御の基礎値（SO。旧baseDefense直書きを廃止）
    [SerializeField] private float respawnDelay = 3f;   // 死亡から復活までの秒数（仮）
    [SerializeField] private DodgeData dodgeData;
    [SerializeField] private StaminaData staminaData;
    [SerializeField] private Transform cameraTransform; // 攻撃の向き・回避方向の基準（未設定ならCamera.main）
    [SerializeField] private PlayerHandState handState;  // 回避時に抜刀をキャンセルする（任意）
    [SerializeField] private EquipmentHolder equipmentHolder; // 装備の武器/防御を反映する

    private Attack attack;
    private Dodge dodge;
    private Stamina stamina;
    private PlayerSkills skills;       // スキルΣ（HP・攻撃・被ダメ軽減）の供給元（任意・無くても動く）
    private Age age;                   // 加齢倍率の供給元（任意・無くても動く＝倍率1.0）
    private PlayerMovement movement;   // 死亡中は移動を止める
    private Health health;
    private readonly ModifiableStat defense = new ModifiableStat(); // 実効防御力＝基礎＋装備補正（加齢対象外）
    private float damageCut;           // スキルの被ダメ軽減Σ（RecalcStatsでキャッシュ）
    private bool hasWeapon; // 武器を装備中か（武器なし＝技が無く攻撃不可）
    private Vector3 spawnPosition;
    private bool dead;

    // --- IBattleInfo / IHealth ---
    public Vector3 Position => transform.position;
    public Team Team => team;
    public float Current => health != null ? health.Current : 0f;
    public float Max => health != null ? health.Max : 0f;
    public bool IsDead => dead;

    public event Action OnDied;               // 死亡通知（PlayerCorpseDropperがドロップを行う）
    public event Action<BattleInfo> OnDamaged; // 被弾通知（PlayerSkillsが防御・肉体XPに使う）

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        attack  = GetComponent<Attack>();
        dodge   = GetComponent<Dodge>();
        stamina = GetComponent<Stamina>();
        skills  = GetComponent<PlayerSkills>();
        age     = GetComponent<Age>();
        movement = GetComponent<PlayerMovement>();
        spawnPosition = transform.position;

        // 兵士のMinionCoreがやっている初期化を、プレイヤー版で薄く行う。
        // Attackの所有者はAttack内のGetComponent<IBattleInfo>()＝このPlayerCombatCoreになる。
        if (staminaData != null) stamina.Initialize(staminaData, team);
        else Debug.LogError($"[PlayerCombatCore] StaminaData未設定: {name}");

        if (dodgeData != null) dodge.Initialize(dodgeData);
        else Debug.LogError($"[PlayerCombatCore] DodgeData未設定: {name}");

        if (vitality == null) Debug.LogError($"[PlayerCombatCore] VitalityData未設定: {name}");
        defense.SetBase(vitality != null ? vitality.defense : 0f);

        // 子のHurtboxに自分を渡す（被ダメの入口。Dodgeのi-frameはHurtbox無効化で効く）。
        var hurtboxes = GetComponentsInChildren<Hurtbox>(true);
        foreach (var h in hurtboxes) h.SetOwner(this);
        if (hurtboxes.Length == 0) Debug.LogWarning($"[PlayerCombatCore] Hurtboxがありません（被弾しない）: {name}");

        // 装備・スキル・加齢の変化で実効値を再計算する（各供給元はCoreを参照しない＝一方向）。
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged += RecalcStats;
        if (skills != null) skills.OnChanged += RecalcStats;
        if (age != null) age.OnChanged += RecalcStats;
        RecalcStats(fullHeal: true); // 初期状態（武器なし＝攻撃不可・HP満タン）
    }

    private void OnDestroy()
    {
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged -= RecalcStats;
        if (skills != null) skills.OnChanged -= RecalcStats;
        if (age != null) age.OnChanged -= RecalcStats;
    }

    private void RecalcStats() => RecalcStats(false);

    // 実効値の再計算：武器（技＋攻撃力）・防御・最大HP・移動速度・スタミナ
    //   （基礎SO＋装備Σ＋スキルΣ、に加齢倍率を合成。倍率の適用先＝素の身体値と肉体系スキルの効果のみ。
    //     装備・技術系スキル・防御は老いても落ちない＝GDDセクション15の加齢仕様）。
    private void RecalcStats(bool fullHeal)
    {
        float mult = age != null ? age.Multiplier : 1f; // 加齢倍率（Ageが無ければ1.0）

        // 武器：右手の武器の技セットでAttackを初期化。武器の攻撃力は加齢対象外、スキルΣは肉体系だけ倍率が掛かる。
        AttackData weapon = equipmentHolder != null ? equipmentHolder.GetWeaponAttack() : null;
        if (weapon != null)
        {
            float skillAttack = skills != null ? skills.GetAttackBonus(mult) : 0f;
            attack.Initialize(weapon.attackPower + skillAttack, weapon.moves);
            hasWeapon = true;
        }
        else
        {
            hasWeapon = false;
        }

        // 防御：装備Σ（加齢対象外）。スキルの軽減は防御計算後にフラットで引く（キャッシュ）。
        defense.SetBonus(equipmentHolder != null ? equipmentHolder.TotalDefenseBonus : 0f);
        damageCut = skills != null ? skills.GetDamageCut(mult) : 0f;

        // 最大HP：素のHP×倍率＋スキルΣ（肉体系は倍率込み）。HealthはMax変更APIを持たないため割合維持で作り直す。
        float newMax = Mathf.Max(1f, (vitality != null ? vitality.hp : 100f) * mult + (skills != null ? skills.GetHpBonus(mult) : 0f));
        float ratio = fullHeal || health == null || health.Max <= 0f ? 1f : health.Current / health.Max;
        health = new Health(newMax);
        health.TakeDamage(newMax * (1f - ratio));

        // 移動速度：基礎速度に乗数（装備補正には掛からない）。スタミナ：最大値に乗数（割合維持）。
        if (movement != null) movement.SetBaseSpeedMultiplier(mult);
        if (stamina != null) stamina.SetMaxScale(mult);
    }

    // --- 被ダメ実体（旧・案Bのnoopを廃止） ---
    public void TakeDamage(BattleInfo info)
    {
        if (dead || health == null) return;

        // 防御計算（受け手側の責務）→ スキルの被ダメ軽減（フラット・RecalcStatsでキャッシュ済み）を引く。
        float damage = DamageCalculator.Calc(info.attackPower, defense.Value);
        damage = Mathf.Max(0f, damage - damageCut);
        health.TakeDamage(damage);

        OnDamaged?.Invoke(info); // 被弾通知（スキルXP等。死の一撃でも経験は入る）
        if (health.IsEmpty) StartCoroutine(DieRoutine());
    }

    // 死＝復活（魂は死なない）。コスト＝装備・持ち物のドロップ（OnDiedをPlayerCorpseDropperが購読）。
    private IEnumerator DieRoutine()
    {
        dead = true;
        attack.ForceCancel();
        if (handState != null) handState.CancelDraw();
        if (movement != null) movement.enabled = false;
        OnDied?.Invoke(); // ここでドロップが行われる（Coreは落とし方を知らない）
        Debug.Log($"[PlayerCombatCore] 死亡。{respawnDelay}秒後にスポーン地点で復活（装備・持ち物はドロップ）");

        yield return new WaitForSeconds(respawnDelay);

        // CharacterControllerはテレポートと相性が悪いので、一時無効化して位置を戻す。
        var cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        transform.position = spawnPosition;
        if (cc != null) cc.enabled = true;

        RecalcStats(fullHeal: true); // 全回復（装備は落としたので武器なし状態で再計算される）
        if (movement != null) movement.enabled = true;
        dead = false;
    }

    private void Update()
    {
        if (dead) return;

        // 攻撃の左クリックは PlayerHandState が司令塔として読み、状態に応じて TryAttack() を呼ぶ。
        //   （インベントリ中は攻撃しない・アイテム所持中は使う等の振り分けは PlayerHandState が担う）
        //   回避（スペース）は状態に絡まないのでここで直接読む。
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (handState != null) handState.CancelDraw(); // 抜刀中なら回避でキャンセル
            dodge.StartDodge(DodgeDirection());
        }
    }

    // 攻撃を試みる（PlayerHandStateから呼ばれる）。向きは変えず、今キャラが向いている方向に振る（MH風・ロックオンなし）。
    public void TryAttack()
    {
        if (dead || attack == null) return;
        if (!hasWeapon) return; // 武器なし＝技が無いので攻撃できない
        attack.StartAttack();
    }

    // カメラ前方を地面に投影した向き（未設定時は自分の前方）。
    private Vector3 CameraForward()
    {
        if (cameraTransform == null) return transform.forward;
        Vector3 f = cameraTransform.forward; f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : transform.forward;
    }

    // 移動入力（カメラ基準）から回避方向を作る。入力が無ければカメラ前方。
    private Vector3 DodgeDirection()
    {
        var kb = Keyboard.current;
        float h = 0f, v = 0f;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
        }

        Vector3 fwd = CameraForward();
        Vector3 right = cameraTransform != null ? cameraTransform.right : transform.right;
        right.y = 0f; right.Normalize();

        Vector3 dir = fwd * v + right * h; dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : CameraForward();
    }
}
