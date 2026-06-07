// 保存先: Assets/Scripts/Player/PlayerCombatCore.cs
// プレイヤーの戦闘実体の薄いCore（兵士のMinionCoreに相当するプレイヤー版）。
//   ・IBattleInfoを実装し、Attack(Hitbox)の所有者になる（敵味方判定に使われる）。
//   ・案B：まだ被ダメ実体にしない。TakeDamageはnoop、Hurtboxも持たない（i-frameの恩恵は当面なし）。
//   ・プレイヤー用SOでAttack/Dodge/Staminaを初期化し、入力で発火する（動かす主体が入力＝兵士はAI）。
//   ・RequireComponentでAttack/Dodge/Staminaを自動追加。Hitbox子・PlayerMovementは別途（Prefab構成）。
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Attack))]
[RequireComponent(typeof(Dodge))]
[RequireComponent(typeof(Stamina))]
public class PlayerCombatCore : MonoBehaviour, IBattleInfo
{
    [SerializeField] private Team team = Team.Red;      // プレイヤーの所属（同Teamは殴らない）
    [SerializeField] private float baseDefense;         // 素の防御力（装備補正と合算。プレイヤーは現状被ダメnoopなので将来用）
    [SerializeField] private DodgeData dodgeData;
    [SerializeField] private StaminaData staminaData;
    [SerializeField] private Transform cameraTransform; // 攻撃の向き・回避方向の基準（未設定ならCamera.main）
    [SerializeField] private PlayerHandState handState;  // 回避時に抜刀をキャンセルする（任意）
    [SerializeField] private EquipmentHolder equipmentHolder; // 装備の武器/防御を反映する

    private Attack attack;
    private Dodge dodge;
    private Stamina stamina;
    private readonly ModifiableStat defense = new ModifiableStat(); // 実効防御力＝base＋装備補正（将来TakeDamageで使用）
    private bool hasWeapon; // 武器を装備中か（武器なし＝技が無く攻撃不可）

    // --- IBattleInfo ---
    public Vector3 Position => transform.position;
    public Team Team => team;
    public void TakeDamage(BattleInfo info) { /* 案B：プレイヤーはまだ被ダメしない（noop） */ }

    private void Awake()
    {
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        attack  = GetComponent<Attack>();
        dodge   = GetComponent<Dodge>();
        stamina = GetComponent<Stamina>();

        // 兵士のMinionCoreがやっている初期化を、プレイヤー版で薄く行う。
        // Attackの所有者はAttack内のGetComponent<IBattleInfo>()＝このPlayerCombatCoreになる。
        if (staminaData != null) stamina.Initialize(staminaData, team);
        else Debug.LogError($"[PlayerCombatCore] StaminaData未設定: {name}");

        // 攻撃(Attack)は武器装備時に初期化する（武器なし＝技が無く攻撃不可）。ここでは初期化しない。
        if (dodgeData != null) dodge.Initialize(dodgeData);
        else Debug.LogError($"[PlayerCombatCore] DodgeData未設定: {name}");

        // 装備の反映（武器→技・防御力補正）。装備が変わるたび再反映する。
        defense.SetBase(baseDefense);
        if (equipmentHolder != null)
        {
            equipmentHolder.OnEquipmentChanged += ApplyEquipment;
            ApplyEquipment(); // 初期状態を反映（初期は武器なし＝攻撃不可）
        }
    }

    private void OnDestroy()
    {
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged -= ApplyEquipment;
    }

    // 装備に応じて武器（技）と防御力補正を反映する。
    private void ApplyEquipment()
    {
        // 武器：右手の武器の技セットでAttackを初期化。無ければ攻撃不可。
        AttackData weapon = equipmentHolder != null ? equipmentHolder.GetWeaponAttack() : null;
        if (weapon != null)
        {
            attack.Initialize(weapon);
            hasWeapon = true;
        }
        else
        {
            hasWeapon = false;
        }

        // 防御力補正（プレイヤーは現状被ダメnoopなので実効は将来）。
        defense.SetBonus(equipmentHolder != null ? equipmentHolder.TotalDefenseBonus : 0f);
    }

    private void Update()
    {
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
        if (attack == null) return;
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
