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
    [SerializeField] private AttackData attackData;
    [SerializeField] private DodgeData dodgeData;
    [SerializeField] private StaminaData staminaData;
    [SerializeField] private Transform cameraTransform; // 攻撃の向き・回避方向の基準（未設定ならCamera.main）

    private Attack attack;
    private Dodge dodge;
    private Stamina stamina;

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

        if (attackData != null) attack.Initialize(attackData);
        else Debug.LogError($"[PlayerCombatCore] AttackData未設定: {name}");

        if (dodgeData != null) dodge.Initialize(dodgeData);
        else Debug.LogError($"[PlayerCombatCore] DodgeData未設定: {name}");
    }

    private void Update()
    {
        // 攻撃の左クリックは PlayerHandState が司令塔として読み、状態に応じて TryAttack() を呼ぶ。
        //   （インベントリ中は攻撃しない・アイテム所持中は使う等の振り分けは PlayerHandState が担う）
        //   回避（スペース）は状態に絡まないのでここで直接読む。
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            dodge.StartDodge(DodgeDirection());
    }

    // 攻撃を試みる（PlayerHandStateから呼ばれる）。カメラ前方を向いてから振る（ロックオンなし）。
    public void TryAttack()
    {
        if (attack == null) return;
        FaceCameraForward();
        attack.StartAttack();
    }

    // カメラ前方を地面に投影した向き（未設定時は自分の前方）。
    private Vector3 CameraForward()
    {
        if (cameraTransform == null) return transform.forward;
        Vector3 f = cameraTransform.forward; f.y = 0f;
        return f.sqrMagnitude > 0.0001f ? f.normalized : transform.forward;
    }

    private void FaceCameraForward()
    {
        Vector3 f = CameraForward();
        if (f.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(f);
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
