// 保存先: Assets/Scripts/Player/MerchantUIController.cs
// 商人の売買UIの開閉（段階2は枠のみ。中身＝売買は段階3）。
//   ItemPickerが商人に近づいてEで Open(merchant) する。開いている間だけパネル表示。
//   Current に対象の商人を保持（段階3でその在庫を表示・売買する）。
using UnityEngine;

public class MerchantUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private bool open;
    public bool IsOpen => open;
    public Merchant Current { get; private set; }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        open = false;
    }

    // 商人に話しかけて売買UIを開く（ItemPickerから）。
    public void Open(Merchant merchant)
    {
        Current = merchant;
        open = true;
        if (panel != null) panel.SetActive(true);
        // 段階3：merchant の在庫・価格を表示する。
    }

    public void Close()
    {
        open = false;
        Current = null;
        if (panel != null) panel.SetActive(false);
    }
}
