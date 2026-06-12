// 保存先: Assets/Scripts/Player/MerchantEmotionView.cs
// 商人の顔イラスト＋セリフの表示役（MerchantUIControllerから分離した表情コンポーネント）。
//   ・Bind(商人)＝通常顔＋挨拶を表示（顔未設定の商人なら枠ごと非表示）。
//   ・Show(顔, セリフ)＝喜び/悲しみ等を一時表示し、emotionDuration 秒後に通常へ戻す。
//   ・Unbind()＝Close時。進行中の表情ルーチンを止めて参照を捨てる。
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MerchantEmotionView : MonoBehaviour
{
    [SerializeField] private RawImage portrait;       // MerchantData の顔を表示（未設定の商人なら枠ごと非表示）
    [SerializeField] private Text speechLabel;        // 左下のセリフ（いらっしゃいませ等。未設定可）
    [SerializeField] private float emotionDuration = 2f; // 喜び/悲しみ顔・セリフを見せる秒数（その後通常へ）

    private Merchant merchant;   // 表示中の商人（通常顔・挨拶へ戻すために保持）
    private Coroutine routine;

    // 売買UIが開いた：通常顔＋挨拶を出す。
    public void Bind(Merchant target)
    {
        merchant = target;
        if (portrait != null)
        {
            portrait.texture = merchant != null ? merchant.PortraitNormal : null;
            portrait.gameObject.SetActive(merchant != null && merchant.PortraitNormal != null);
        }
        if (speechLabel != null)
        {
            string greeting = merchant != null ? merchant.LineGreeting : "";
            speechLabel.text = greeting;
            speechLabel.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(greeting));
        }
    }

    // 売買UIが閉じた：表情ルーチンを止める（パネルごと消えるので表示はそのままでよい）。
    public void Unbind()
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        merchant = null;
    }

    // 表情とセリフを一時的に切り替え、emotionDuration 秒後に通常（顔＝Normal・セリフ＝挨拶）へ戻す。
    public void Show(Texture2D tex, string line)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(EmotionRoutine(tex, line));
    }

    private IEnumerator EmotionRoutine(Texture2D tex, string line)
    {
        if (portrait != null && tex != null && portrait.gameObject.activeSelf)
            portrait.texture = tex;
        if (speechLabel != null && !string.IsNullOrEmpty(line))
            speechLabel.text = line;

        yield return new WaitForSeconds(emotionDuration);

        if (merchant != null)
        {
            if (portrait != null) portrait.texture = merchant.PortraitNormal;
            if (speechLabel != null) speechLabel.text = merchant.LineGreeting;
        }
        routine = null;
    }
}
