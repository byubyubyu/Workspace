// 保存先: Assets/Scripts/Item/BottleUIController.cs
// 瓶UIの制御役（ミニマップのMinimapControllerに相当）。
//   ・開閉（キー入力。新Input System直接読み）。拾った時は OpenBottle() を外（ItemPicker）から呼んで自動で開く。
//   ・開いている間だけ瓶専用カメラを有効化（撮影カメラ→RenderTexture→RawImageで表示）。
//   ・開いている間だけ BottleDragger を有効化。
//   ・開く時：BottleStorage.Load() で前回の積み方を復元。
//   ・閉じる時：見た目だけ閉じる（プレイヤーは行動再開＝無防備が延びない）。
//     裏の物理空間は静止するまで動かし続け、静止 or タイムアウトで BottleStorage.Save()（記録＋破棄）。
//     ＝「閉じても物理結果は確定する（逃げ得防止）」。
//
//   表示の配置（RawImageの大きさ・余白＝周囲から背後の世界が見える）はCanvas側で調整（実装時）。
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class BottleUIController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private GameObject bottlePanel;   // 瓶UIのパネル（RawImageを含む）
    [SerializeField] private Camera bottleCamera;      // 瓶の2D物理空間を撮る専用カメラ
    [SerializeField] private Bottle bottle;            // 瓶（物理空間）
    [SerializeField] private BottleStorage storage;    // 記録・復元
    [SerializeField] private BottleDragger dragger;    // 漁る操作役

    [Header("入力")]
    [SerializeField] private Key toggleKey = Key.I;    // 開閉キー（仮：Inventory）

    [Header("閉じる時の静止待ち")]
    [SerializeField] private float closeTimeout = 3f;  // 静止しなくても強制確定するまでの秒数（保険）

    private bool open;
    private bool closing; // 閉じ処理（静止待ち）中か
    public bool IsOpen => open;

    private void Start()
    {
        // 初期は閉じる（見た目オフ・カメラオフ・Dragger無効）。
        ApplyVisual(false);
        open = false;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb[toggleKey].wasPressedThisFrame)
        {
            if (open) CloseBottle();
            else OpenBottle();
        }
    }

    // 開く（キー or 拾った時に外から呼ぶ）。
    public void OpenBottle()
    {
        if (open) return;

        // 閉じ処理（静止待ち）の途中だった場合：コルーチンを止める前に、その場でSaveを完了させる。
        //   静止待ち中はまだSaveが走っておらず瓶に実体が残っている。ここでSaveせずにLoadすると
        //   「残った実体＋復元分」で二重になる。なので開く直前に強制Save（今の状態を記録・全破棄）する。
        if (closing)
        {
            StopAllCoroutines();
            closing = false;
            if (storage != null) storage.Save(); // 静止を待たず即記録・破棄（瓶を空にする）
        }

        open = true;
        ApplyVisual(true);

        // 前回の積み方を復元。
        if (storage != null) storage.Load();
    }

    // 閉じる（見た目だけ閉じ、物理は静止まで回してから記録）。
    public void CloseBottle()
    {
        if (!open) return;
        open = false;

        // 見た目を閉じる（プレイヤーは行動再開）。物理空間(Bottle)は動かし続ける。
        ApplyVisual(false);

        // 静止 or タイムアウトを待ってから記録・破棄。
        if (storage != null && bottle != null)
        {
            StopAllCoroutines();
            StartCoroutine(WaitRestThenSave());
        }
    }

    private IEnumerator WaitRestThenSave()
    {
        closing = true;
        float t = 0f;
        // 全アイテムが静止するか、タイムアウトまで待つ。
        while (t < closeTimeout && !bottle.IsAllAtRest())
        {
            t += Time.deltaTime;
            yield return null;
        }
        storage.Save(); // 記録＋破棄（共有方式：閉じたら物理は消す）
        closing = false;
    }

    // 見た目・カメラ・DraggerのまとめてON/OFF。
    private void ApplyVisual(bool value)
    {
        if (bottlePanel != null) bottlePanel.SetActive(value);
        if (bottleCamera != null) bottleCamera.enabled = value;
        if (dragger != null) dragger.enabled = value;
    }
}
