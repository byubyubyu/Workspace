using UnityEngine;

// 見た目モデル用：親階層のCharacterControllerの水平速度をAnimatorの"Speed"に流す。
// ビジュアル子オブジェクト（例：Player/SoldierVisual）にAnimatorと一緒に付ける。
[RequireComponent(typeof(Animator))]
public class LocomotionAnimatorDriver : MonoBehaviour
{
    [SerializeField] private float damping = 0.15f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private Animator animator;
    private CharacterController controller;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        controller = GetComponentInParent<CharacterController>();
    }

    private void Update()
    {
        if (controller == null) return;
        Vector3 v = controller.velocity;
        v.y = 0f;
        animator.SetFloat(SpeedHash, v.magnitude, damping, Time.deltaTime);
    }
}
