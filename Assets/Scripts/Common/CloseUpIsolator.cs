// 保存先: Assets/Scripts/Common/CloseUpIsolator.cs
// クローズアップ画面（装備C・進化C）で「自分以外を消す」共通ヘルパー。
//   仕組み：開いている間だけ、対象（プレイヤー）の見た目＝Renderer持ちGOのレイヤーを CloseUpView へ移し、
//   メインカメラのCulling Maskを CloseUpView のみにする（マスク操作は呼び出し側Controllerが行う）。
//   結果：真っ黒な空間に自分と装備だけが浮かぶ。世界（地面・草・木・兵士・市民・建物・アイテム）は映らない。
//   ・レイヤーを変えるのはRendererを持つGOだけ＝Hurtbox/Hitbox等の当たり判定GO（別GO）は触らない
//     →メニュー中も被弾する「無防備リスク」のゲームデザインを壊さない。
//   ・瓶・商人・装備スロット・進化部位などのRT表示3Dは専用カメラなので影響なし。
//   ・Refresh()：開いている間に見た目が作り直された時（装備変更・部位進化のApplyBody）に呼び直す
//     （新しいRendererは元レイヤーのままで生成される＝映らなくなるため）。
using System.Collections.Generic;
using UnityEngine;

public static class CloseUpIsolator
{
    public const string LayerName = "CloseUpView";

    private static readonly List<(GameObject go, int layer)> swapped = new List<(GameObject, int)>();
    private static GameObject root;

    public static int Layer => LayerMask.NameToLayer(LayerName);
    public static int Mask => 1 << Layer;

    // 対象の見た目をCloseUpViewレイヤーへ移す（Openで呼ぶ。呼び出し側はカメラのcullingMaskをMaskにする）。
    public static void Isolate(GameObject target)
    {
        Restore(); // 二重呼び出し保護（前の対象が残っていたら戻す）
        if (target == null) return;
        root = target;
        Apply();
    }

    // 開いている間の見た目作り直し（装備変更・部位進化）後に呼ぶ。新しいRendererを拾い直す。
    public static void Refresh()
    {
        if (root == null) return;
        RestoreLayers();
        Apply();
    }

    // すべて元のレイヤーへ戻す（Closeで呼ぶ。呼び出し側はカメラのcullingMaskを復元する）。
    public static void Restore()
    {
        RestoreLayers();
        root = null;
    }

    private static void Apply()
    {
        int layer = Layer;
        if (layer < 0)
        {
            Debug.LogError($"[CloseUpIsolator] レイヤー「{LayerName}」が存在しません");
            return;
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            var go = r.gameObject;
            swapped.Add((go, go.layer));
            go.layer = layer;
        }
    }

    private static void RestoreLayers()
    {
        foreach (var (go, layer) in swapped)
            if (go != null) go.layer = layer;
        swapped.Clear();
    }
}
