// 保存先: Assets/Scripts/Item/BottleZone.cs
// 瓶のゾーン（内側ゾーン・外側ゾーン）のトリガー検知を Bottle へ中継する小コンポーネント。
//   設計方針「検知はBottle側が持つ／BottleItemCoreは受け身」に沿い、ゾーンの当たりはここで拾って
//   Bottle に渡す（Bottleが状態で分岐する）。ゾーン用の子GameObject（Collider2D・isTrigger）に付ける。
//   2D物理空間で動かすため Collider2D は 2D。
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BottleZone : MonoBehaviour
{
    public enum ZoneKind
    {
        Inside,  // 内側ゾーン：入った収納前アイテムを収納済みにする
        Outside, // 外側ゾーン：口から出たアイテムを検知（Bottleが状態で分岐）
    }

    [SerializeField] private ZoneKind kind;

    private Bottle bottle;

    // Bottleがコードからゾーンを生成する際に種類を設定する。
    public void SetKind(ZoneKind zoneKind) => kind = zoneKind;

    public void Initialize(Bottle owner)
    {
        bottle = owner;
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (bottle == null) return;
        var item = other.GetComponentInParent<BottleItemCore>();
        if (item == null) return;

        if (kind == ZoneKind.Inside) bottle.OnEnterInside(item);
        else bottle.OnEnterOutside(item);
    }
}
