// 保存先: Assets/Scripts/Item/Bottle.cs
// 瓶クラス（1つにまとめる）。裏の2D物理空間として、ワールドの遠く離れた場所に置く想定
//   （瓶専用カメラでここを撮ってUIに映す＝ミニマップ流儀。InventorySystemが配置・カメラを管理する）。
//
//   責務：
//     ・2D物理空間に瓶の壁（BoxCollider2D 3枚＝底・左・右。分厚く・透明。すり抜け対策）。上は開口。
//     ・BottleDataのサイズを適用。
//     ・アイテムを入れる位置（口の上の落下開始位置）を提供。
//     ・中のBottleItemCoreのリスト保持・追加・除外。
//     ・内側ゾーン：入ったアイテムを収納済みにする（落下中→Stored）。
//     ・外側ゾーン：口から出たアイテムを状態で分岐（ドラッグ中→使用 / 収納済み→こぼれ / 落下中→無視）。
//     ・静止判定：全アイテムのRigidbody2Dが眠った/速度ほぼ0か。
//
//   後段で繋ぐ（このクラスにフックだけ用意）：
//     ・こぼれ時のマップ復帰依頼（ItemPicker）… OnSpilled イベントで通知する。
//     ・静止確定後の記録・破棄依頼（BottleStorage）… ④で購読する。
//     ・ドラッグ中状態の付与（BottleDragger）… ⑤で BottleItemCore.MarkDragging を呼ぶ。
using System;
using System.Collections.Generic;
using UnityEngine;

public class Bottle : MonoBehaviour
{
    [SerializeField] private BottleData data;

    [Header("壁（技術値・ゲーム性に影響しない）")]
    [SerializeField] private float wallThickness = 1f; // 分厚くしてすり抜けを防ぐ

    [Header("壁の見た目")]
    [SerializeField] private Sprite wallSprite;            // 壁用Sprite（未割り当てなら単色矩形）。後で瓶の絵に差し替え可
    [SerializeField] private Color wallColor = new Color(0.5f, 0.35f, 0.2f, 1f); // 壁の色（仮：茶系）
    [SerializeField] private int wallSortingOrder = 0;     // 描画順（アイテムより奥に置くなら小さく）

    [Header("背景板の見た目（瓶の中身の背面）")]
    [SerializeField] private Sprite backgroundSprite;      // 背景用Sprite（未割り当てなら単色矩形）
    [SerializeField] private Color backgroundColor = new Color(0.15f, 0.12f, 0.1f, 1f); // 背景色（仮：暗い茶）
    [SerializeField] private int backgroundSortingOrder = -10; // 一番奥
    [SerializeField] private float backgroundZ = 0.5f;     // アイテムより奥に置くためのZオフセット（+で奥）

    [Header("落下開始位置（口の上・内側高さからの追加オフセット）")]
    [SerializeField] private float dropHeightMargin = 1f;

    [Header("静止判定")]
    [SerializeField] private float restVelocityThreshold = 0.05f; // この速度未満を静止とみなす

    private readonly List<BottleItemCore> items = new List<BottleItemCore>();

    // こぼれた（収納済みが口から出た）アイテムを外へ知らせる。ItemPickerが購読してマップに戻す。
    public event Action<ItemData> OnSpilled;

    // 取り出された（ドラッグ中のアイテムが口の外に出た）。PlayerHandStateが購読して手に持つ。
    //   ※ 以前は即使用していたが、段階2から「手に持つ→左クリックで使う」に変更。
    public event Action<ItemData> OnItemTakenOut;

    public BottleData Data => data;
    public IReadOnlyList<BottleItemCore> Items => items;

    // 壁・ゾーンを組み立てる。InventorySystemが生成時に呼ぶ（DIでdataを渡す形も可）。
    public void Build(BottleData bottleData)
    {
        if (bottleData != null) data = bottleData;
        if (data == null)
        {
            Debug.LogError($"[Bottle] BottleData が null です: {name}");
            return;
        }

        BuildBackground();
        BuildWalls();
        BuildZones();
    }

    // --- 構築 ---

    // 瓶の中身の背面（背景板）。瓶の内側全体を覆う1枚。アイテムより奥に置く。
    private void BuildBackground()
    {
        float w = data.InnerWidth;
        float h = data.InnerHeight;

        var go = new GameObject("Background");
        go.transform.SetParent(transform, false);
        // 中身の中央（高さの中ほど）に、Zを奥にずらして配置。
        go.transform.localPosition = new Vector3(0f, h * 0.5f, backgroundZ);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = backgroundColor;
        sr.sortingOrder = backgroundSortingOrder;

        if (backgroundSprite != null)
        {
            sr.sprite = backgroundSprite;
            Vector2 spriteSize = backgroundSprite.bounds.size;
            float sx = spriteSize.x > 0.0001f ? w / spriteSize.x : 1f;
            float sy = spriteSize.y > 0.0001f ? h / spriteSize.y : 1f;
            go.transform.localScale = new Vector3(sx, sy, 1f);
        }
        else
        {
            sr.sprite = GetOrCreateUnitSprite();
            go.transform.localScale = new Vector3(w, h, 1f);
        }
    }

    private void BuildWalls()
    {
        float w = data.InnerWidth;
        float h = data.InnerHeight;
        float t = wallThickness;

        // 底（中央下）。
        CreateWall("Wall_Bottom",
            new Vector2(0f, -t * 0.5f),
            new Vector2(w + t * 2f, t));
        // 左壁。
        CreateWall("Wall_Left",
            new Vector2(-(w * 0.5f) - t * 0.5f, h * 0.5f),
            new Vector2(t, h + t));
        // 右壁。
        CreateWall("Wall_Right",
            new Vector2((w * 0.5f) + t * 0.5f, h * 0.5f),
            new Vector2(t, h + t));
        // 上は開口（壁なし）。
    }

    private void CreateWall(string wallName, Vector2 localCenter, Vector2 size)
    {
        var go = new GameObject(wallName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localCenter;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        // isTrigger=false（実体の壁）。

        // 見た目：SpriteRendererで壁の矩形を描く（壁Colliderと同じ位置・サイズ）。
        AddWallView(go, size);
    }

    // 壁の見た目（SpriteRenderer）を付ける。Spriteが未割り当てなら単色の矩形Spriteを生成して使う。
    private void AddWallView(GameObject wallGo, Vector2 size)
    {
        var sr = wallGo.AddComponent<SpriteRenderer>();
        sr.color = wallColor;
        sr.sortingOrder = wallSortingOrder;

        if (wallSprite != null)
        {
            sr.sprite = wallSprite;
            // SpriteをDrawMode=Sliced/Tiledにしなくても、scaleで矩形サイズに合わせる。
            //   Sprite元の1ユニットあたりサイズに対し、size になるようlocalScaleを調整。
            Vector2 spriteSize = wallSprite.bounds.size; // ワールド単位（PixelsPerUnit考慮済み）
            float sx = spriteSize.x > 0.0001f ? size.x / spriteSize.x : 1f;
            float sy = spriteSize.y > 0.0001f ? size.y / spriteSize.y : 1f;
            wallGo.transform.localScale = new Vector3(sx, sy, 1f);
        }
        else
        {
            // 未割り当て：単色の1x1矩形Spriteを生成し、scaleでsizeに合わせる。
            sr.sprite = GetOrCreateUnitSprite();
            wallGo.transform.localScale = new Vector3(size.x, size.y, 1f);
        }
    }

    // 単色矩形用の1x1 Sprite（生成して使い回す）。
    private static Sprite unitSprite;
    private static Sprite GetOrCreateUnitSprite()
    {
        if (unitSprite != null) return unitSprite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        unitSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return unitSprite;
    }

    private void BuildZones()
    {
        float w = data.InnerWidth;
        float h = data.InnerHeight;

        // 内側ゾーン：瓶の内側を覆うトリガー（少し内側に作り、落ちて中に収まったら収納済みに）。
        var inside = CreateZone("Zone_Inside",
            new Vector2(0f, h * 0.5f),
            new Vector2(w, h),
            BottleZone.ZoneKind.Inside);

        // 外側ゾーン：口の外（上）を覆うトリガー。口から出たら検知。
        var outside = CreateZone("Zone_Outside",
            new Vector2(0f, h + (dropHeightMargin + h) * 0.5f),
            new Vector2(w + wallThickness * 4f, dropHeightMargin + h),
            BottleZone.ZoneKind.Outside);

        inside.Initialize(this);
        outside.Initialize(this);
    }

    private BottleZone CreateZone(string zoneName, Vector2 localCenter, Vector2 size, BottleZone.ZoneKind kind)
    {
        var go = new GameObject(zoneName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localCenter;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        box.isTrigger = true;
        var zone = go.AddComponent<BottleZone>();
        // kindはInspectorでなくコードで設定したいので、SerializeFieldをリフレクションせず
        // シンプルにフィールドを公開する代わりに、ここでは生成直後に種類を渡す方式にする。
        zone.SetKind(kind);
        return zone;
    }

    // 落下開始位置（口の上・中央）。ItemPickerが拾ったアイテムをここから落とす。
    public Vector3 GetDropPosition()
    {
        float h = data != null ? data.InnerHeight : 0f;
        return transform.TransformPoint(new Vector3(0f, h + dropHeightMargin, 0f));
    }

    // --- 中身の管理 ---

    public void Register(BottleItemCore item)
    {
        if (item != null && !items.Contains(item)) items.Add(item);
    }

    public void Unregister(BottleItemCore item)
    {
        items.Remove(item);
    }

    // --- ゾーンからの通知（BottleZoneが呼ぶ） ---

    public void OnEnterInside(BottleItemCore item)
    {
        // 落下中→収納済み。
        item.MarkStored();
    }

    public void OnEnterOutside(BottleItemCore item)
    {
        // 口から出た。状態で分岐。
        switch (item.State)
        {
            case ItemState.Dragging:
                // 取り出し成功。即使用せず、プレイヤーの手に渡す（OnItemTakenOut）。
                //   実体は瓶から消し、ItemDataだけ手に持つ状態にする（使うのは手に持ってから左クリック）。
                OnItemTakenOut?.Invoke(item.Data);
                RemoveAndDestroy(item);
                break;

            case ItemState.Stored:
                // こぼれ＝マップに戻す（ItemPickerへ依頼）。
                item.MarkSpilling();
                OnSpilled?.Invoke(item.Data);
                RemoveAndDestroy(item);
                break;

            case ItemState.Falling:
            case ItemState.Spilling:
            default:
                // 落下中（入れている途中）・既にこぼれ中は無視。
                break;
        }
    }

    private void RemoveAndDestroy(BottleItemCore item)
    {
        Unregister(item);
        if (item != null) Destroy(item.gameObject);
    }

    // --- 静止判定（閉じても物理結果を確定するために使う。記録・破棄は④で繋ぐ） ---

    // 全アイテムが静止しているか（速度ほぼ0／眠っている）。
    public bool IsAllAtRest()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it == null) continue;
            var rb = it.GetComponent<Rigidbody2D>();
            if (rb == null) continue;
            if (!rb.IsSleeping() && rb.linearVelocity.sqrMagnitude > restVelocityThreshold * restVelocityThreshold)
                return false;
        }
        return true;
    }
}
