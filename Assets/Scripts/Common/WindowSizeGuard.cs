// 保存先: Assets/Scripts/Common/WindowSizeGuard.cs
// ウィンドウの最小サイズ監視役。可変ウィンドウで最小解像度(既定1280×720)を下回ったら戻す。
//   Unityには「ウィンドウの最小サイズ」を直接指定するAPIが無いため、
//   小さくされた次のフレームで Screen.SetResolution によりクランプする方式（PC向けの定番）。
//   フルスクリーン中・エディタ実行中は何もしない。
using UnityEngine;

public class WindowSizeGuard : MonoBehaviour
{
    [SerializeField] private int minWidth = 1280;
    [SerializeField] private int minHeight = 720;
    [SerializeField] private float checkInterval = 0.5f; // 監視間隔（毎フレームは不要）

    private float timer;

    private void Update()
    {
        if (Application.isEditor) return; // エディタのGame Viewには干渉しない
        if (Screen.fullScreen) return;

        timer += Time.unscaledDeltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        int w = Screen.width;
        int h = Screen.height;
        if (w >= minWidth && h >= minHeight) return;

        Screen.SetResolution(Mathf.Max(w, minWidth), Mathf.Max(h, minHeight), FullScreenMode.Windowed);
    }
}
