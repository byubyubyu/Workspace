using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IDasher
{
    [Header("移動速度（仮・後調整）")]
    [SerializeField] private float moveSpeed = 5f;      // 通常歩き（手ぶら）
    [SerializeField] private float runSpeed = 8f;       // 走り（Shift）
    [SerializeField] private float weaponSpeed = 3f;    // 武器構え中（遅い）

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private PlayerHandState handState; // 状態参照（武器構えなら遅く／走ると納刀）

    private CharacterController characterController;
    private Attack attack; // 攻撃中の移動入力ロック判定用

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
        else if (handState != null && handState.State == HandState.Weapon)
        {
            speed = weaponSpeed; // 武器構え中は遅い（走っていないとき）
        }

        // カメラの向きを地面に投影して移動方向を決める
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 move = (forward * v + right * h).normalized;
        characterController.SimpleMove(move * speed);
    }
}
