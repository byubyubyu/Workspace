// 保存先: Assets/Scripts/Item/BottleStorage.cs
// 瓶の中身を「種類(ItemData)・瓶ローカル位置・Z回転角」の一覧として記録・復元する役（保持データ）。
//   物理オブジェクト(BottleItemCore)そのものとは別に、ただのデータとして保持する（閉じても残る）。
//
//   Save() : 今の中身（収納済み）を記録し、BottleItemCoreを破棄する（共有方式：閉じたら物理は消す）。
//   Load() : 記録から同じ位置・向きで BottleItemCore を再生成する（BottleItemFactoryに生成を依頼）。
//
//   Save/Load は受け身。いつ呼ぶか（閉じる時の静止待ち等）は BottleUIController(⑥) が制御する。
//   生成は BottleItemFactory(⑧) に依頼する（設計：生成はFactory／初期化は呼び出し側）。
using System.Collections.Generic;
using UnityEngine;

// 記録1件：種類・瓶ローカル位置・Z回転角。
[System.Serializable]
public struct StoredItem
{
    public ItemData data;
    public Vector2 localPosition;
    public float angle; // Z回転（度）

    public StoredItem(ItemData data, Vector2 localPosition, float angle)
    {
        this.data = data;
        this.localPosition = localPosition;
        this.angle = angle;
    }
}

public class BottleStorage : MonoBehaviour
{
    [SerializeField] private Bottle bottle;
    [SerializeField] private BottleItemFactory factory;

    private readonly List<StoredItem> records = new List<StoredItem>();

    public IReadOnlyList<StoredItem> Records => records;

    public void Initialize(Bottle ownerBottle, BottleItemFactory itemFactory)
    {
        bottle = ownerBottle;
        factory = itemFactory;
    }

    // 今の中身を記録し、物理オブジェクトを破棄する（閉じる時・静止確定後に呼ばれる）。
    //   静止確定後なので、瓶の中のアイテムは原則すべて記録・破棄する。
    //   例外：こぼれ中(Spilling)はマップへ向かう途中なので保存対象外（瓶から出る扱い）。
    //   ※ 状態がStoredかどうかで選別すると、静止しているのにStoredになっていないアイテムが
    //     破棄されず残り、次に開いたとき二重になる。それを防ぐため「Spilling以外は全部」とする。
    public void Save()
    {
        if (bottle == null)
        {
            Debug.LogError($"[BottleStorage] Bottle が未設定です: {name}");
            return;
        }

        records.Clear();

        // リストはコピーしてから回す（破棄でBottle側のリストが変化するため）。
        var items = new List<BottleItemCore>(bottle.Items);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null) continue;
            if (item.State == ItemState.Spilling) continue; // こぼれ中は保存しない（瓶から出る扱い）

            // 瓶ローカル座標・Z回転に変換して記録。
            Vector3 localPos = bottle.transform.InverseTransformPoint(item.transform.position);
            float angle = item.transform.localEulerAngles.z;
            records.Add(new StoredItem(item.Data, new Vector2(localPos.x, localPos.y), angle));

            // 破棄（共有方式：閉じたら物理は消す）。
            bottle.Unregister(item);
            Destroy(item.gameObject);
        }
    }

    // 記録から中身を再生成する（開く時に呼ばれる）。
    public void Load()
    {
        if (bottle == null || factory == null)
        {
            Debug.LogError($"[BottleStorage] Bottle/Factory が未設定です: {name}");
            return;
        }

        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            if (rec.data == null) continue;

            // 瓶ローカル位置→ワールド位置に戻す。
            Vector3 worldPos = bottle.transform.TransformPoint(new Vector3(rec.localPosition.x, rec.localPosition.y, 0f));

            // ★回転は最後に適用する。
            //   Initialize内の見た目スケール合わせ(ItemViewScaler)は現在のboundsを測るため、
            //   先に回転を付けるとboundsが斜めで膨らみ、スケールが狂う（小さくなる）。
            //   なので「回転ゼロで生成→Initialize（スケール確定）→回転適用」の順にする。
            BottleItemCore item = factory.Create(rec.data, worldPos, Quaternion.identity, bottle.transform);
            if (item == null) continue;
            item.Initialize(rec.data);

            // スケール確定後に回転を適用。
            item.transform.localRotation = Quaternion.Euler(0f, 0f, rec.angle);

            item.MarkStored(); // 復元直後は収納済み（安定状態）として始める
            bottle.Register(item);
        }
    }
}
