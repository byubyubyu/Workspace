// 保存先: Assets/Scripts/SightBlocker.cs
// 視線遮蔽サービス（世界に1個）。地形に生えた木（Terrain Tree）の位置を集め、
//   2点間の視線が木に遮られているかを XZ 平面の「線分 × 円」判定で返す。
//   Terrain Tree のコライダーは近傍でしか動的生成されず、離れた地点間の Raycast には乗らない。
//   そのため物理に頼らず、木の位置データ（terrainData.treeInstances）から自前で判定する。
//   見た目は Terrain Tree のまま（大量描画・LOD を活かす）、判定だけをここで持つ＝見た目と判定の分離。
//
// 使い方：シーンに空 GameObject を作りこのコンポーネントを付けるだけ（Awake で自動構築）。
//   兵士の Vision が SightBlocker.Instance.IsBlocked(...) を問い合わせる（横断的な環境サービス）。
using System.Collections.Generic;
using UnityEngine;

public class SightBlocker : MonoBehaviour
{
    // 横断的な環境サービスのため、シーン唯一の参照を静的に公開する（DI で全生成経路に引き回さない）。
    public static SightBlocker Instance { get; private set; }

    [SerializeField] private float treeRadius = 0.5f; // 木1本の遮蔽半径の基準（× widthScale で実半径）
    [SerializeField] private float cellSize = 8f;      // 空間グリッドのセルサイズ（広いほどセル数は減るがセル内判定が増える）

    // 木1本の遮蔽データ（XZ 平面の円）。
    private struct Tree
    {
        public Vector2 center; // XZ 中心
        public float radius;   // 遮蔽半径
    }

    private readonly List<Tree> trees = new List<Tree>();
    // 空間グリッド：セル座標 → そのセルに中心がある木のインデックス一覧。
    private readonly Dictionary<Vector2Int, List<int>> grid = new Dictionary<Vector2Int, List<int>>();
    private float maxRadius; // 全木の最大半径（走査範囲のマージンに使う）

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SightBlocker] 既に存在するため重複を破棄: {name}");
            Destroy(this);
            return;
        }
        Instance = this;
        Build();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // シーンの全 Terrain から木を集め、空間グリッドを構築する。
    private void Build()
    {
        trees.Clear();
        grid.Clear();
        maxRadius = 0f;

        Terrain[] terrains = Terrain.activeTerrains; // 複数タイルに分割された Terrain を全て対象にする
        foreach (var terrain in terrains)
        {
            if (terrain == null || terrain.terrainData == null) continue;
            TerrainData data = terrain.terrainData;
            Vector3 origin = terrain.transform.position;
            Vector3 size = data.size;

            foreach (var instance in data.treeInstances)
            {
                // instance.position は 0〜1 の正規化座標。Terrain サイズを掛けて原点を足すとワールド座標。
                Vector3 world = origin + Vector3.Scale(instance.position, size);
                float radius = treeRadius * instance.widthScale;
                if (radius <= 0f) continue;

                int index = trees.Count;
                trees.Add(new Tree { center = new Vector2(world.x, world.z), radius = radius });
                if (radius > maxRadius) maxRadius = radius;

                Vector2Int cell = CellOf(world.x, world.z);
                if (!grid.TryGetValue(cell, out var list))
                {
                    list = new List<int>();
                    grid[cell] = list;
                }
                list.Add(index);
            }
        }

        Debug.Log($"[SightBlocker] 木 {trees.Count} 本を {grid.Count} セルに登録（cellSize={cellSize}）");
    }

    // 2点間の視線が木に遮られているか（XZ 平面で判定・高さは無視）。
    //   from→to の線分が、いずれかの木の円と交差すれば true（遮蔽）。
    public bool IsBlocked(Vector3 from, Vector3 to)
    {
        if (trees.Count == 0) return false;

        Vector2 a = new Vector2(from.x, from.z);
        Vector2 b = new Vector2(to.x, to.z);

        // 線分の AABB を最大半径ぶん広げ、その範囲のセルだけ走査する。
        //   木の円が線分に交差するなら、その中心は線分から radius 以内＝この広げた範囲に必ず入る＝取りこぼさない。
        float minX = Mathf.Min(a.x, b.x) - maxRadius;
        float maxX = Mathf.Max(a.x, b.x) + maxRadius;
        float minZ = Mathf.Min(a.y, b.y) - maxRadius;
        float maxZ = Mathf.Max(a.y, b.y) + maxRadius;

        int cx0 = Mathf.FloorToInt(minX / cellSize);
        int cx1 = Mathf.FloorToInt(maxX / cellSize);
        int cz0 = Mathf.FloorToInt(minZ / cellSize);
        int cz1 = Mathf.FloorToInt(maxZ / cellSize);

        for (int cx = cx0; cx <= cx1; cx++)
        {
            for (int cz = cz0; cz <= cz1; cz++)
            {
                if (!grid.TryGetValue(new Vector2Int(cx, cz), out var list)) continue;
                foreach (int i in list)
                {
                    Tree t = trees[i];
                    if (SegmentCircleIntersect(a, b, t.center, t.radius)) return true;
                }
            }
        }
        return false;
    }

    private Vector2Int CellOf(float x, float z)
    {
        return new Vector2Int(Mathf.FloorToInt(x / cellSize), Mathf.FloorToInt(z / cellSize));
    }

    // 線分 ab と、中心 c・半径 r の円が交差するか（点 c から線分 ab への最短距離 ≦ r）。
    private static bool SegmentCircleIntersect(Vector2 a, Vector2 b, Vector2 c, float r)
    {
        Vector2 ab = b - a;
        float abLenSq = ab.sqrMagnitude;
        Vector2 closest;
        if (abLenSq < 1e-6f)
        {
            closest = a; // from と to がほぼ同じ点（ゼロ長線分）
        }
        else
        {
            float t = Vector2.Dot(c - a, ab) / abLenSq;
            t = Mathf.Clamp01(t); // 線分の外側にはみ出さないよう端で止める
            closest = a + t * ab;
        }
        return (c - closest).sqrMagnitude <= r * r;
    }
}
