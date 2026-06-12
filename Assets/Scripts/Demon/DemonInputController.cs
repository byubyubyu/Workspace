// 保存先: Assets/Scripts/Demon/DemonInputController.cs
// 魔族プレイヤーの入力司令塔（人間のPlayerHandStateに相当する魔族版）。
//   ・入力を読んで実体のAPIを呼ぶだけ（マルチプレイ方針：入力とロジックの分離）。
//   ・技ボタン：左クリック＝技0、Rキー＝技1（※右クリックはTPSCameraの回転ドラッグと衝突するためRを採用）。
//     解放されていない技番号はAttack.StartAttackが範囲ガードで無視する。
//   ・デバッグキー：F9＝捕食ポイント+50／F10＝魂ポイント+50（検証用チート）。
//   ・死亡中・大ひるみ中は攻撃できない（移動は現状ロックしない＝M1の簡易仕様）。
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(DemonCore))]
[RequireComponent(typeof(Attack))]
public class DemonInputController : MonoBehaviour
{
    private DemonCore core;
    private Attack attack;

    private void Awake()
    {
        core = GetComponent<DemonCore>();
        attack = GetComponent<Attack>();
    }

    private void Update()
    {
        if (core.IsDead) return;

        // 画面系UIを開いている間は技を出さない（進化行・タブ等のクリックと左クリック=技0の衝突防止）。
        if (TabMenuController.Instance != null && TabMenuController.Instance.IsOpen) return;
        if (BottleUIController.Instance != null && BottleUIController.Instance.IsOpen) return;
        if (MerchantUIController.Instance != null && MerchantUIController.Instance.IsOpen) return;

        var keyboard = Keyboard.current;

        // デバッグ：捕食ポイント+50（進化はC画面から行う。検証を速くするためのチートキー）。
        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
        {
            core.DevourPool?.Add(50f);
            Debug.Log($"[Debug] 捕食ポイント+50 → {core.DevourPool?.Current:F0}");
            return;
        }

        // デバッグ：魂ポイント+50（転生＝死亡時の素体選択の検証を速くするためのチートキー）。
        if (keyboard != null && keyboard.f10Key.wasPressedThisFrame)
        {
            var soul = GetComponent<DemonSoul>();
            if (soul != null)
            {
                soul.Add(50f);
                Debug.Log($"[Debug] 魂ポイント+50 → {soul.Points:F0}");
            }
            return;
        }

        // 大ひるみ中は技を出せない。
        if (core.IsStaggered) return;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) attack.StartAttack(0);  // 技0
        else if (keyboard != null && keyboard.rKey.wasPressedThisFrame) attack.StartAttack(1); // 技1（解放前は範囲外で無視）
    }
}
