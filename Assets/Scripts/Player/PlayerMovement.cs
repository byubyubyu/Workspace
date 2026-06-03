using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour, IDasher
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform cameraTransform;
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

        // カメラの向きを地面に投影して移動方向を決める
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 move = (forward * v + right * h).normalized;
        characterController.SimpleMove(move * moveSpeed);
    }
}