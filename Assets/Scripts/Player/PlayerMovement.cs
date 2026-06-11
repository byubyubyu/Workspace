using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IDasher
{
    [Header("移動速度（仮・後調整）")]
    [SerializeField] private float moveSpeed = 5f;      // 通常歩き（手ぶら）
    [SerializeField] private float runSpeed = 8f;       // 走り（Shift）
    [SerializeField] private float weaponSpeed = 3f;    // 武器構え中（遅い）

    [Header("向き")]
    [SerializeField] private float turnSpeed = 180f;    // 移動方向へ向き直る速さ（度/秒）。仮・後調整

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerHandState handState; // 状態参照（武器構えなら遅く／走ると納刀）
    [SerializeField] private EquipmentHolder equipmentHolder; // 装備の移動速度補正を反映

    private CharacterController characterController;
    private Attack attack; // 攻撃中の移動入力ロック判定用
    private readonly ModifiableStat speedStat = new ModifiableStat(); // 実効移動速度＝状態speed(base)＋装備補正(bonus)

    // 回避ダッシュ（IDasher）。Dodge実体が制御する。ダッシュ中は入力移動を止める。
    private bool dashing;
    private Vector3 dashDir;
    private float dashSpeed;

    public void Dash(Vector3 dir, float speed) { dashing = true; dashDir = dir; dashSpeed = speed; }
    public void EndDash() { dashing = false; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        attack = GetComponent<Attack>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Start()
    {
        // 装備の移動速度補正を反映（装備が変わるたび更新）。
        if (equipmentHolder != null)
        {
            equipmentHolder.OnEquipmentChanged += ApplyEquipment;
            ApplyEquipment();
        }
    }

    private void OnDestroy()
    {
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged -= ApplyEquipment;
    }

    private void ApplyEquipment()
    {
        speedStat.SetBonus(equipmentHolder != null ? equipmentHolder.TotalMoveSpeedBonus : 0f);
    }

    // 装備以外からの移動速度補正の公開口（魔族＝部位の補正Σ。人間は装備イベント（ApplyEquipment）経由のまま）。
    public void SetMoveSpeedBonus(float bonus) => speedStat.SetBonus(bonus);

    // 基礎速度への乗数（加齢の老衰など）。装備補正(bonus)には掛からない＝素の身体だけ衰える。
    public void SetBaseSpeedMultiplier(float multiplier) => speedStat.SetBaseMultiplier(multiplier);

    private void Update()
    {
        // 攻撃中は移動入力を受け付けない（重い一撃。GDD「攻撃中は移動不可」）。
        if (attack != null && attack.IsAttacking) return;

        // 回避ダッシュ中は入力移動をせず、ダッシュ方向へ動かす。
        if (dashing)
        {
            characterController.SimpleMove(dashDir * dashSpeed);
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // WASD / 矢印キーから入力方向を作る
        float h = 0f, v = 0f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;

        Vector3 input = new Vector3(h, 0f, v);
        bool hasInput = input.sqrMagnitude > 0.0001f;

        // 走り：Shift押しながら移動入力があるとき。
        bool runHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        bool running = runHeld && hasInput;

        // 走り出したら納刀する（武器構え中に走ろうとすると武器をしまう＝MH風）。
        if (running && handState != null) handState.Sheathe();

        // 速度を状態で決める。
        float speed = moveSpeed;
        if (running)
        {
            speed = runSpeed;
        }
        else if (handState != null &&
                 (handState.State == HandState.Weapon || handState.State == HandState.Drawing))
        {
            speed = weaponSpeed; // 武器構え中・抜刀中は遅い（走っていないとき）
        }

        // カメラの向きを地面に投影して移動方向を決める
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 move = (forward * v + right * h).normalized;

        // 移動方向へキャラを向ける（MH風。カメラ向きとは独立。攻撃中はこのUpdate冒頭でreturn済み＝今の向き維持）。
        if (hasInput)
        {
            Quaternion target = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // 実効速度＝状態speed（base）＋装備補正（bonus）。計算はModifiableStatに集約。
        speedStat.SetBase(speed);
        characterController.SimpleMove(move * speedStat.Value);
    }
}
