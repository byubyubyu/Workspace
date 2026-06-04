// 保存先: Assets/Scripts/Editor/GrassVariationTool.cs
// 編集時に1回流す「草のムラ付け」ツール。
//   緑のベース草レイヤーの一部を、ノイズで変化用レイヤーに置き換えてまだらにする。
//   ★ベース草とその変化用レイヤーの「間だけ」を再配分する。道(Dirt)など他のレイヤーの重みは
//     一切触らない＝塗った道は安全（合計1も保たれる）。
//   ★ノイズをワールド座標で取るので、タイルをまたいでも柄が連続する（継ぎ目なし）。
//   メニュー Tools > Terrain > Grass Variation から開く。Undo(Ctrl+Z)で戻せる。
using UnityEngine;
using UnityEditor;

public class GrassVariationTool : EditorWindow
{
    [SerializeField] private TerrainLayer grassLayer;     // ベースの草（この重みを削って変化用に回す）
    [SerializeField] private TerrainLayer variationLayer; // 変化用（草/土の別テクスチャ）。無いタイルには自動追加
    [SerializeField] private float patchSize = 20f;               // まだらの大きさ（ワールド単位。大きいほど大きな塊）
    [SerializeField, Range(0f, 1f)] private float coverage = 0.3f;     // 草エリアのうちどれだけを変化にするか
    [SerializeField, Range(0.01f, 1f)] private float blend = 0.3f;     // フチのなじみ（小=くっきり、大=ゆるやか）

    [MenuItem("Tools/Terrain/Grass Variation")]
    private static void Open() => GetWindow<GrassVariationTool>("Grass Variation");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "緑のベース草の一部を、ノイズで変化用レイヤーに置き換えてまだらにします。\n" +
            "道(Dirt)など他のレイヤーは触りません。Undo(Ctrl+Z)で戻せます。",
            MessageType.Info);

        grassLayer = (TerrainLayer)EditorGUILayout.ObjectField("Grass Layer (ベース)", grassLayer, typeof(TerrainLayer), false);
        variationLayer = (TerrainLayer)EditorGUILayout.ObjectField("Variation Layer (変化用)", variationLayer, typeof(TerrainLayer), false);
        patchSize = EditorGUILayout.FloatField("Patch Size (まだらの大きさ)", patchSize);
        coverage = EditorGUILayout.Slider("Coverage (変化の量)", coverage, 0f, 1f);
        blend = EditorGUILayout.Slider("Blend (フチのなじみ)", blend, 0.01f, 1f);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(grassLayer == null || variationLayer == null || patchSize <= 0f))
        {
            if (GUILayout.Button("Apply to all terrains in scene"))
                ApplyToAll();
        }
    }

    private void ApplyToAll()
    {
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
        if (terrains.Length == 0)
        {
            Debug.LogWarning("[GrassVariation] シーンにTerrainが見つかりません。");
            return;
        }

        int done = 0;
        foreach (var terrain in terrains)
            if (ApplyToTerrain(terrain)) done++;

        Debug.Log($"[GrassVariation] 適用: {done}/{terrains.Length} タイル。");
    }

    private bool ApplyToTerrain(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        if (data == null) return false;

        // grassLayer が無いタイルはスキップ（ベースが無い＝対象外）。
        int grassIdx = IndexOfLayer(data, grassLayer);
        if (grassIdx < 0)
        {
            Debug.LogWarning($"[GrassVariation] {terrain.name}: Grass Layerが無いのでスキップ。");
            return false;
        }

        // variationLayer が無ければ追加してからインデックス取得（alphamapに0埋めの層が増える）。
        int varIdx = IndexOfLayer(data, variationLayer);
        if (varIdx < 0)
        {
            Undo.RegisterCompleteObjectUndo(data, "Grass Variation (add layer)");
            var layers = data.terrainLayers;
            var newLayers = new TerrainLayer[layers.Length + 1];
            System.Array.Copy(layers, newLayers, layers.Length);
            newLayers[layers.Length] = variationLayer;
            data.terrainLayers = newLayers;
            varIdx = layers.Length;
        }

        Undo.RegisterCompleteObjectUndo(data, "Grass Variation");

        int w = data.alphamapWidth;
        int h = data.alphamapHeight;
        float[,,] maps = data.GetAlphamaps(0, 0, w, h);

        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        float freq = 1f / patchSize;
        float threshold = 1f - coverage; // coverageが大→thresholdが下がる→変化が増える

        for (int y = 0; y < h; y++)
        {
            float nz = (h > 1) ? (float)y / (h - 1) : 0f;
            float worldZ = origin.z + nz * size.z;
            for (int x = 0; x < w; x++)
            {
                float nx = (w > 1) ? (float)x / (w - 1) : 0f;
                float worldX = origin.x + nx * size.x;

                float g = maps[y, x, grassIdx];
                float v = maps[y, x, varIdx];
                float budget = g + v; // この2層だけで再配分（道など他層は不変→合計1は保たれる）
                if (budget <= 0f) continue; // 草も変化も無い所（＝道など）は触らない

                float noise = Mathf.PerlinNoise(worldX * freq, worldZ * freq);
                float vf = Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(threshold - blend * 0.5f, threshold + blend * 0.5f, noise));
                vf = Mathf.Clamp01(vf);

                maps[y, x, varIdx] = budget * vf;
                maps[y, x, grassIdx] = budget * (1f - vf);
            }
        }

        data.SetAlphamaps(0, 0, maps);
        EditorUtility.SetDirty(data);
        return true;
    }

    private static int IndexOfLayer(TerrainData data, TerrainLayer layer)
    {
        var layers = data.terrainLayers;
        for (int i = 0; i < layers.Length; i++)
            if (layers[i] == layer) return i;
        return -1;
    }
}
