using UnityEngine;

// 一時テスト用：生成兵士モデル（Humanoid）の可動域確認。
// Animator の Humanoid ボーン参照を取り、サイン波で歩行風に振る。
public class SoldierMotionTest : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    [SerializeField] private float armSwing = 45f;
    [SerializeField] private float legSwing = 35f;
    [SerializeField] private float kneeBend = 25f;
    [SerializeField] private float headTurn = 25f;

    private Transform armL, armR, foreL, foreR, thighL, thighR, shinL, shinR, head, spine;
    private Quaternion iArmL, iArmR, iForeL, iForeR, iThighL, iThighR, iShinL, iShinR, iHead, iSpine;

    private void Start()
    {
        var anim = GetComponent<Animator>();
        armL = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        armR = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        foreL = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        foreR = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        thighL = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        thighR = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        shinL = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        shinR = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        head = anim.GetBoneTransform(HumanBodyBones.Head);
        spine = anim.GetBoneTransform(HumanBodyBones.Spine);

        iArmL = armL.localRotation; iArmR = armR.localRotation;
        iForeL = foreL.localRotation; iForeR = foreR.localRotation;
        iThighL = thighL.localRotation; iThighR = thighR.localRotation;
        iShinL = shinL.localRotation; iShinR = shinR.localRotation;
        iHead = head.localRotation; iSpine = spine.localRotation;
    }

    private void LateUpdate()
    {
        float t = Time.time * speed;
        float s = Mathf.Sin(t);
        float right = transform.eulerAngles.y; // ルートの向き基準でワールド軸回転

        // 腕：前後スイング（左右逆位相）＋Tポーズから下ろす
        Rotate(armL, iArmL, Vector3.right, s * armSwing, Vector3.forward, 60f);
        Rotate(armR, iArmR, Vector3.right, -s * armSwing, Vector3.forward, -60f);
        // 肘：軽く曲げ
        Rotate(foreL, iForeL, Vector3.right, Mathf.Max(0, s) * 30f, Vector3.zero, 0f);
        Rotate(foreR, iForeR, Vector3.right, Mathf.Max(0, -s) * 30f, Vector3.zero, 0f);
        // 脚：前後スイング（腕と逆）＋膝
        Rotate(thighL, iThighL, Vector3.right, -s * legSwing, Vector3.zero, 0f);
        Rotate(thighR, iThighR, Vector3.right, s * legSwing, Vector3.zero, 0f);
        Rotate(shinL, iShinL, Vector3.right, Mathf.Max(0, -s) * kneeBend, Vector3.zero, 0f);
        Rotate(shinR, iShinR, Vector3.right, Mathf.Max(0, s) * kneeBend, Vector3.zero, 0f);
        // 頭：左右に見回す／背骨：軽くひねり
        Rotate(head, iHead, Vector3.up, Mathf.Sin(t * 0.5f) * headTurn, Vector3.zero, 0f);
        Rotate(spine, iSpine, Vector3.up, s * 8f, Vector3.zero, 0f);
    }

    // ワールド軸基準で初期ローカル回転にオフセットを乗せる（ボーンローカル軸の差異を吸収）
    private void Rotate(Transform bone, Quaternion init, Vector3 axis1, float deg1, Vector3 axis2, float deg2)
    {
        bone.localRotation = init;
        Quaternion offset = Quaternion.AngleAxis(deg1, transform.TransformDirection(axis1));
        if (deg2 != 0f) offset = Quaternion.AngleAxis(deg2, transform.TransformDirection(axis2)) * offset;
        bone.rotation = offset * bone.rotation;
    }
}
