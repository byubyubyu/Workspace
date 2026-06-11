// 保存先: Assets/Scripts/Demon/DevourMotionDriver.cs
// 捕食モーションのつなぎ役。Devourer（親）の状態をプル型で読み、食べている間だけ
//   ・SineSwayMotion をON（頭のむしゃむしゃ）
//   ・体を死体の方へなめらかに向ける（食事中は移動すると中断なので回転が競合しない）
//   ・（任意）一定間隔で食べかすエフェクトを出す（null可＝未設定なら何も出ない）
// 捕食固有の知識はここだけ＝揺れ部品（SineSwayMotion）は寝る・待機等に使い回せる。
// 頭の部位prefabに同梱する（部位がモーションを持ち運ぶ既存原則。ForelegSlamMotionと同型のGetComponentInParent参照）。
using UnityEngine;

public class DevourMotionDriver : MonoBehaviour
{
    [SerializeField] private SineSwayMotion sway;    // 揺れ部品（未設定なら自分から取得）
    [SerializeField] private float turnSpeed = 6f;   // 死体の方へ向く速さ
    [SerializeField] private GameObject munchEffect; // ひと噛みごとのエフェクト（任意・null可）
    [SerializeField] private float munchInterval = 0.6f; // エフェクトの間隔（秒）

    private Devourer devourer;
    private float effectTimer;

    private void Awake()
    {
        devourer = GetComponentInParent<Devourer>(); // 魔族の体に組み込まれた時だけ見つかる（野生モンスター等ではnull＝何もしない）
        if (sway == null) sway = GetComponent<SineSwayMotion>();
    }

    private void LateUpdate()
    {
        bool eating = devourer != null && devourer.IsEating && devourer.Target != null;
        if (sway != null) sway.SetActive(eating);
        if (!eating)
        {
            effectTimer = 0f;
            return;
        }

        // 体（魔族のルート）を死体の方へY軸だけなめらかに向ける。
        var root = devourer.transform;
        Vector3 dir = devourer.Target.transform.position - root.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            root.rotation = Quaternion.Slerp(root.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);

        // ひと噛みエフェクト（任意）。
        if (munchEffect != null)
        {
            effectTimer += Time.deltaTime;
            if (effectTimer >= munchInterval)
            {
                effectTimer = 0f;
                Instantiate(munchEffect, transform.position, Quaternion.identity);
            }
        }
    }
}
