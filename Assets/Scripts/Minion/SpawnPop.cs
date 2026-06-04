// 保存先: Assets/Scripts/Minion/SpawnPop.cs
// 生成された瞬間、小さいScaleから本来のサイズへ拡大する「ポップイン」。
//   兵士Prefabに付ける。MinionCore等には依存しない（自己完結・コードのみ・素材不要）。
//   overshoot=true で、少し大きくなってから戻る（ぷるん）。
//   ※ 拡大中はCollider/NavMeshAgentも一時的に小さくなるが、ごく短時間なので実用上問題ない。
using UnityEngine;

public class SpawnPop : MonoBehaviour
{
    [SerializeField] private float duration = 0.15f;                  // 拡大にかける時間（秒）
    [SerializeField, Range(0f, 1f)] private float startScale = 0.1f;  // 開始時の倍率（本来サイズ比）
    [SerializeField] private bool overshoot = true;                   // true＝少し超えて戻る（ぷるん）

    private Vector3 targetScale; // 本来のサイズ（Prefabの設定スケール）
    private float timer;
    private bool running;

    private void Start()
    {
        targetScale = transform.localScale;                // Prefabの設定スケールを本来サイズとして記憶
        transform.localScale = targetScale * startScale;   // 小さく始める
        timer = 0f;
        running = true;
    }

    private void Update()
    {
        if (!running) return;

        timer += Time.deltaTime;
        float t = duration > 0f ? Mathf.Clamp01(timer / duration) : 1f;

        // 0→1 のイージング。overshootなら終わりで1を少し超えてから収束する（easeOutBack）。
        float e = overshoot ? EaseOutBack(t) : Mathf.SmoothStep(0f, 1f, t);
        float f = Mathf.LerpUnclamped(startScale, 1f, e); // startScale から本来サイズへ（e>1で少し超える）
        transform.localScale = targetScale * f;

        if (t >= 1f)
        {
            transform.localScale = targetScale; // 最後はぴったり本来サイズに戻す
            running = false;
            enabled = false;                     // 以降Updateを止める
        }
    }

    // easeOutBack：終わり際に少しオーバーシュートしてから1へ収束する。
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float p = t - 1f;
        return 1f + c3 * p * p * p + c1 * p * p;
    }
}
