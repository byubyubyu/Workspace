// 保存先: Assets/Scripts/UI/FactionSelectUI.cs
// 世界開始時の陣営選択（人間／魔族）。GDDセクション14。
//   ・世界はプレイヤーなしで回る設計なので、選択中も止めない（タイトル画面相当のオーバーレイ）。
//   ・シーンに人間プレイヤー・魔族プレイヤーを両方非アクティブで置いておき、選ばれた方だけアクティブ化
//     （＝擬似スポーン。マルチの「参加者がスポーンする」構図と同じ。本格スポーン化は将来の独立フェーズ）。
//   ・所属Teamは各プレイヤー側のSerializeFieldで持つ（人間＝Blue／魔族＝Red・テスト運用）。
using UnityEngine;
using UnityEngine.UI;

public class FactionSelectUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;        // 全画面の選択パネル
    [SerializeField] private Button humanButton;      // 「人間」
    [SerializeField] private Button demonButton;      // 「魔族」
    [SerializeField] private GameObject humanPlayer;  // 人間プレイヤー（シーン直置き・非アクティブ）
    [SerializeField] private GameObject demonPlayer;  // 魔族プレイヤー（シーン直置き・非アクティブ）
    [SerializeField] private TPSCamera tpsCamera;     // 追従カメラ（選ばれた方を追う）

    private void Start()
    {
        // 開始時は両プレイヤーとも非アクティブ（世界だけが回っている状態）。
        if (humanPlayer != null) humanPlayer.SetActive(false);
        if (demonPlayer != null) demonPlayer.SetActive(false);

        if (humanButton != null) humanButton.onClick.AddListener(() => Choose(humanPlayer));
        if (demonButton != null) demonButton.onClick.AddListener(() => Choose(demonPlayer));
        if (panel != null) panel.SetActive(true);
    }

    // 陣営決定：選ばれたプレイヤーをアクティブ化（＝世界に参加）し、カメラを向ける。
    private void Choose(GameObject player)
    {
        if (player != null)
        {
            player.SetActive(true);
            ActivePlayer.Set(player); // UI系（瓶・ミニマップ・商人）はこれを読む
            if (tpsCamera != null) tpsCamera.SetTarget(player.transform);
        }
        if (panel != null) panel.SetActive(false);
    }
}
