// 保存先: Assets/Scripts/Minion/DeathPoof.cs
// 兵士が死んだ瞬間、ポッフ用パーティクルを死亡位置に出す。
//   兵士Prefabに付ける。MinionCore.OnDestroyed を購読するだけ（MinionCoreは変更しない＝疎結合）。
//   パーティクルは別オブジェクトなので、兵士本体が即Destroyされても残って自分で消える。
//     → Prefab側で ParticleSystem の Stop Action=Destroy にしておくこと（後始末コード不要）。
using UnityEngine;

[RequireComponent(typeof(MinionCore))]
public class DeathPoof : MonoBehaviour
{
    [SerializeField] private GameObject poofPrefab; // 死亡時に出すパーティクルPrefab（未設定なら何も出ない）

    private MinionCore core;

    private void Awake()
    {
        core = GetComponent<MinionCore>();
        if (core != null) core.OnDestroyed += SpawnPoof;
    }

    private void OnDestroy()
    {
        if (core != null) core.OnDestroyed -= SpawnPoof; // 念のため購読解除
    }

    // MinionCore.Die() が Destroy する直前に OnDestroyed で呼ばれる。
    private void SpawnPoof()
    {
        if (poofPrefab == null) return;
        Instantiate(poofPrefab, transform.position, Quaternion.identity);
    }
}
