using UnityEngine;

// アニメ適用後（LateUpdate）に背骨へロール補正をかけ、モーキャプクリップ由来の
// 上体の左右傾きを打ち消す。補正量は速度でスケール（走りのみ補正する想定）。
[RequireComponent(typeof(Animator))]
public class PostureCorrector : MonoBehaviour
{
    [SerializeField] private float rollDegrees = -7.5f; // 負=左傾きを右へ起こす方向（要実機確認）
    [SerializeField] private float startSpeed = 6f;    // この速度から補正開始
    [SerializeField] private float fullSpeed = 10f;    // この速度で補正最大

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void LateUpdate()
    {
        float t = Mathf.InverseLerp(startSpeed, fullSpeed, animator.GetFloat(SpeedHash));
        if (t <= 0f) return;
        Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        if (spine == null) return;
        spine.rotation = Quaternion.AngleAxis(rollDegrees * t, transform.forward) * spine.rotation;
    }
}
