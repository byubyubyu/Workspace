// 保存先: Assets/Scripts/Minion/SineSwayMotion.cs
// 汎用ループモーション「ONの間、対象Transformを指定軸にサイン波で揺らす。OFFでなめらかに基準姿勢へ戻る」。
//   片側振り（0→angle→0の繰り返し）＝うなずき・呼吸・尻尾ふり等の「ループで揺れる」全般に使い回せる。
//   条件（いつ揺れるか）はこのクラスは知らない＝Driver側（DevourMotionDriver等）がSetActiveで切り替える。
//   ForelegSlamMotionと同じプロシージャル方式（Animator/Clip不使用。本物のモデル導入時に移行を検討）。
using UnityEngine;

public class SineSwayMotion : MonoBehaviour
{
    [SerializeField] private Transform target;       // 動かす対象（未設定なら自分）
    [SerializeField] private Vector3 axis = Vector3.right; // 回転軸（ローカル。Xなら前後のうなずき）
    [SerializeField] private float angle = 25f;      // 振れ角（度。負で逆方向）
    [SerializeField] private float frequency = 2f;   // 1秒あたりの往復回数
    [SerializeField] private float returnSpeed = 8f; // OFF時に基準姿勢へ戻る速さ

    private Quaternion baseRotation;
    private float time;

    public bool Active { get; private set; }

    public void SetActive(bool active)
    {
        if (Active == active) return;
        Active = active;
        if (active) time = 0f; // 揺れは常に基準姿勢から始める
    }

    private void Awake()
    {
        if (target == null) target = transform;
        baseRotation = target.localRotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (Active)
        {
            // 片側振り：0→angle→0 のループ（(1-cos)/2 で滑らかに往復）。
            time += Time.deltaTime;
            float a = angle * (0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * frequency * time));
            target.localRotation = baseRotation * Quaternion.AngleAxis(a, axis);
        }
        else
        {
            // なめらかに基準姿勢へ戻す（ForelegSlamMotionの戻しと同じ流儀）。
            target.localRotation = Quaternion.Slerp(target.localRotation, baseRotation, returnSpeed * Time.deltaTime);
        }
    }
}
