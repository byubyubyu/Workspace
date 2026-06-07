// 保存先: Assets/Scripts/Item/Corpse.cs
// 兵士の死体。倒れた兵士が落とした InventoryHolder（瓶の中身）を持ち、一定時間その場に残る。
//   プレイヤーが近づいてEで開くと中身を漁れる（BottleUIControllerが対象を切り替えてLoad/Save）。
//   残存時間で消滅するが、開かれている間（漁っている最中）はタイマーを止める。
//   CorpseSpawner（兵士側・OnDestroyed購読）が生成し、中身を移譲する。中身が空でも出す（現状の方針）。
using UnityEngine;

[RequireComponent(typeof(InventoryHolder))]
public class Corpse : MonoBehaviour
{
    [SerializeField] private float lifetime = 30f; // 残存時間（秒）

    private InventoryHolder holder;
    private float timer;

    public InventoryHolder Holder => holder;

    private void Awake()
    {
        holder = GetComponent<InventoryHolder>();
    }

    private void Update()
    {
        // 漁られている間（瓶UIがこの死体を開いている間）は消えない。
        if (IsBeingViewed()) return;

        timer += Time.deltaTime;
        if (timer >= lifetime) Destroy(gameObject);
    }

    // 今、この死体が瓶UIで開かれているか（プレイヤーが漁っている最中か）。
    private bool IsBeingViewed()
    {
        var ui = BottleUIController.Instance;
        return ui != null && ui.IsOpen && ui.CurrentHolder == holder;
    }

    // 瓶UIで閉じられた時にBottleUIControllerから呼ばれる。中身(records＋pending)が空なら死体を消す。
    public void OnClosed()
    {
        if (holder == null) return;
        if (holder.Records.Count == 0 && holder.PendingItems.Count == 0)
            Destroy(gameObject);
    }
}
