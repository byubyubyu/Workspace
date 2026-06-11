// 保存先: Assets/Scripts/Common/ActivePlayer.cs
// 「今このクライアントが操作しているプレイヤー」への参照（人間Player／魔族DemonPlayerのどちらか）。
//   陣営選択（FactionSelectUI）が選択時にSetし、UI系（瓶・ミニマップ・商人）が読む。
//   ※ マルチプレイ原則との整合：これは「世界にプレイヤーは1人」ではなく
//     「このクライアントの操作対象（ローカルプレイヤー）は1人」というローカル概念。
//     マルチ化しても各クライアントに1つ存在する正当な参照（サーバー側の世界状態には使わない）。
using UnityEngine;

public static class ActivePlayer
{
    public static GameObject Go { get; private set; }
    public static Transform Transform => Go != null ? Go.transform : null;
    public static InventoryHolder Holder { get; private set; }
    public static IBattleInfo Battle { get; private set; } // Team・位置（人間=PlayerCombatCore／魔族=DemonCore）

    public static bool Exists => Go != null;
    public static Team Team => Battle != null ? Battle.Team : Team.None;

    // 陣営選択で操作プレイヤーが決まった時に呼ぶ。
    public static void Set(GameObject player)
    {
        Go = player;
        Holder = player != null ? player.GetComponent<InventoryHolder>() : null;
        Battle = player != null ? player.GetComponent<IBattleInfo>() : null;
    }
}
