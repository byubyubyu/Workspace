using UnityEngine;

// 見た目モデル用：Humanoidの手ボーンに任意のTransform（HandPoint等）を追従させる。
// PostureCorrector等のLateUpdate補正の後に走らせるため実行順を遅らせる。
[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Animator))]
public class HandBoneFollower : MonoBehaviour
{
    [SerializeField] private Transform target;            // 追従させるTransform（PlayerのHandPoint）
    [SerializeField] private HumanBodyBones bone = HumanBodyBones.RightHand;
    [SerializeField] private Vector3 positionOffset;      // 手ボーンローカルでの位置オフセット（握り調整）
    [SerializeField] private Vector3 rotationOffset;      // 同・回転オフセット（オイラー角）

    private Animator animator;
    private Transform boneTransform;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        boneTransform = animator.GetBoneTransform(bone);
    }

    private void LateUpdate()
    {
        if (target == null || boneTransform == null) return;
        target.position = boneTransform.TransformPoint(positionOffset);
        target.rotation = boneTransform.rotation * Quaternion.Euler(rotationOffset);
    }
}
