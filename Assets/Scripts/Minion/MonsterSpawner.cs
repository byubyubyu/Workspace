// 保存先: Assets/Scripts/Minion/MonsterSpawner.cs
// 世界を徘徊するモンスターの出現役。シーンに置き、自分の位置に生成する。
//   「Factoryは生成のみ・Initializeは呼び出し側」の確定事項に従う（MinionFactoryを流用）。
//   TeamはNone＝全勢力と敵対（Vision/Hitboxの「Teamが違えば敵」判定で自動成立。
//   モンスター同士は同じNoneなので争わない）。
using UnityEngine;

[RequireComponent(typeof(MinionFactory))]
public class MonsterSpawner : MonoBehaviour
{
    [SerializeField] private MinionData monsterData; // モンスターの種類SO（prefab・HP・攻撃力・視野・ドロップ）
    [SerializeField] private int count = 1;          // 出現数
    [SerializeField] private float spawnSpread = 3f; // 複数体出す時のばらし半径

    private void Start()
    {
        if (monsterData == null)
        {
            Debug.LogError($"[MonsterSpawner] monsterData が未設定です: {name}");
            return;
        }
        var factory = GetComponent<MinionFactory>();
        for (int i = 0; i < count; i++)
        {
            Vector2 r = i == 0 ? Vector2.zero : Random.insideUnitCircle * spawnSpread;
            Vector3 pos = transform.position + new Vector3(r.x, 0f, r.y);
            var core = factory.Create(monsterData, pos);
            if (core != null) core.Initialize(monsterData, Team.None);
        }
    }
}
