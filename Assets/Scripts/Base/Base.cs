// 保存先: Assets/Scripts/Base/Base.cs
using System.Collections.Generic;
using UnityEngine;

public class Base : MonoBehaviour
{
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private List<Path> paths;

    public Vector2Int GridSize => gridSize;
    public List<Path> Paths => paths;

    // マス座標(Vector2Int) → ワールド座標(Vector3) の変換。
    // Base 自身の位置をグリッド左下の基準点とし、指定マスの「中心」のワールド座標を返す。
    // グリッドの行方向(y)はワールドの z 軸に対応させる（高さ y は 0 のまま）。
    // ※ 原点をグリッド中央に寄せたい場合は、ここで基準点をずらす（将来調整可）。
    public Vector3 GridToWorld(Vector2Int cell)
    {
        float x = (cell.x + 0.5f) * cellSize;
        float z = (cell.y + 0.5f) * cellSize;
        return transform.position + new Vector3(x, 0f, z);
    }

    // 自分の Cityhall を生成したとき、隣接 Base にその OnTeamChanged を購読させる（分散型）。
    // 初期配置でも占拠の後付けでも、Cityhall 生成時にこれが呼ばれる。
    public void AnnounceCityhall(CityhallBehavior cityhall)
    {
        foreach (var path in paths)
        {
            foreach (var neighborBase in path.ConnectedBases)
            {
                if (neighborBase == this) continue;
                neighborBase.SubscribeNeighborCityhall(this, cityhall);
            }
        }
    }

    // 隣接 Base から依頼を受け、その Cityhall の Team 変化を購読する。
    // 発火時に自分の BaseAI の neighborTeams を更新する（隣の占拠・破壊に気づく）。
    public void SubscribeNeighborCityhall(Base neighborBase, CityhallBehavior cityhall)
    {
        var baseAI = GetComponent<BaseAI>();
        if (baseAI == null) return;
        cityhall.OnTeamChanged += (team) => baseAI.UpdateNeighborTeam(neighborBase, team);
    }
}