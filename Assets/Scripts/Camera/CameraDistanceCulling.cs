// 保存先: Assets/Scripts/Camera/CameraDistanceCulling.cs
// 本編カメラに付ける。指定レイヤーのオブジェクトを「カメラから一定距離より遠い」と描画しない。
//   ・描画だけ間引く（GameObjectは存在し続ける）ので、BaseAIの判断も兵士の移動・戦闘もそのまま動く。
//   ・ミニマップカメラには付けないこと（ミニマップは全部見せたいため）。
//   ・レイヤーごとに距離を設定できる：地形タイル＝遠く、Base＝中間、兵士＝近め、のように使い分け可。
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraDistanceCulling : MonoBehaviour
{
    // レイヤー名 ＋ そのレイヤーを描画する最遠距離（0 = カメラのFar Clipまで＝特別なカリングなし）
    [System.Serializable]
    public struct LayerCull
    {
        public string layerName;  // 例: "Ground"（地形タイル）, "CullBase", "CullMinion"
        public float distance;    // この距離を超えたら描画しない。0なら効果なし（Far Clip任せ）
    }

    [SerializeField] private LayerCull[] entries;
    [SerializeField] private bool spherical = true; // true＝カメラからの半径距離で判定（Player基準に近い）

    private Camera cam;

    private void Awake() { cam = GetComponent<Camera>(); }
    private void Start() { Apply(); }

    // Inspectorで距離を変えたら、実行中でもこれを呼べば反映できる。
    public void Apply()
    {
        if (cam == null) cam = GetComponent<Camera>();

        // layerCullDistancesは必ず32要素。0は「Far Clipを使う＝特別カリングなし」。
        float[] dist = new float[32];
        if (entries != null)
        {
            foreach (var e in entries)
            {
                int layer = LayerMask.NameToLayer(e.layerName);
                if (layer < 0 || layer > 31) continue; // 存在しないレイヤー名は無視
                dist[layer] = Mathf.Max(0f, e.distance);
            }
        }
        cam.layerCullDistances = dist;
        cam.layerCullSpherical = spherical;
    }
}
