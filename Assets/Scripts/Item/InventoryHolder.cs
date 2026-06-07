// 保存先: Assets/Scripts/Item/InventoryHolder.cs
// 瓶の中身（StoredItem一覧＝records）を所有者単位で持つデータ。プレイヤー・兵士（死体）が1個ずつ持つ。
//   物理瓶(Bottle)・瓶カメラ・UIは共有1セット。中身データだけ所有者ごとに分け、
//   BottleUIControllerが「開く対象のholder」を切り替え、BottleStorageがそのrecordsをLoad/Saveする。
//   ＝「他人（死体）の瓶を漁る」の土台。設計書13-O「他人の瓶を覗く・取り出す」に対応。
using System.Collections.Generic;
using UnityEngine;

public class InventoryHolder : MonoBehaviour
{
    [SerializeField] private List<StoredItem> records = new List<StoredItem>();
    // まだ瓶に積んでいない初期アイテム（種類のみ）。初回に開いた時、口の上から落として積む（位置は物理で決まる）。
    //   積んだ後は閉じる時のSaveでrecordsに焼かれ、pendingは空になる（兵士の初期インベントリ用）。
    private readonly List<ItemData> pendingItems = new List<ItemData>();

    // BottleStorageが読み書きする中身レコード（種類・瓶ローカル位置・Z回転角）。
    public List<StoredItem> Records => records;
    public List<ItemData> PendingItems => pendingItems;

    // 生成時に初期アイテム（種類）をセットする（MinionCoreから呼ばれる）。
    public void SetInitialItems(List<ItemData> items)
    {
        pendingItems.Clear();
        if (items != null) pendingItems.AddRange(items);
    }

    // 別のholderの中身（records＋pendingItems）をコピーして上書きする（死亡時に死体へ移譲する用）。
    public void CopyFrom(InventoryHolder other)
    {
        records.Clear();
        pendingItems.Clear();
        if (other == null) return;
        records.AddRange(other.Records);
        pendingItems.AddRange(other.PendingItems);
    }
}
