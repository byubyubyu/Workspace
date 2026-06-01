// 保存先: Assets/Scripts/Minion/Attack.cs
using UnityEngine;

public class Attack : MonoBehaviour
{
    private float attackPower;
    private float attackInterval;
    private float attackRange;
    private float attackTimer;
    private IBattleInfo target;

    public float AttackRange => attackRange;

    public void Initialize(IMinionData data)
    {
        attackPower = data.Stat.attackPower;
        attackInterval = data.Stat.attackInterval;
        attackRange = data.Stat.attackRange;
    }

    public void SetTarget(IBattleInfo target)
    {
        this.target = target;
    }

    // 破壊済み(Destroyされた)対象を掴んでいないか判定する。
    // IBattleInfo はインターフェースなので、UnityEngine.Object にキャストして Unity の null 判定に通す。
    private bool IsTargetAlive()
    {
        if (target == null) return false;
        var obj = target as Object;
        if (obj == null) return false; // Destroy済みは Unity の == null で true になる
        return true;
    }

    public bool IsInRange()
    {
        if (!IsTargetAlive()) return false;
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = target.Position; b.y = 0f;
        return Vector3.Distance(a, b) <= attackRange;
    }

    private void Update()
    {
        if (!IsTargetAlive()) { target = null; return; } // 死んだ対象は捨てる
        if (!IsInRange()) { attackTimer = 0f; return; }

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            // DEBUG: 誰が何を攻撃しているか（Team確認）
            string myTeam = GetComponent<MinionCore>() != null ? GetComponent<MinionCore>().Team.ToString() : "?";
            string tgtTeam = "?";
            var tObj = target as Component;
            if (tObj != null)
            {
                var bc = tObj.GetComponent<BuildingCore>();
                var mc = tObj.GetComponent<MinionCore>();
                if (bc != null) tgtTeam = "Building/" + bc.Team;
                else if (mc != null) tgtTeam = "Minion/" + mc.Team;
            }
            Debug.Log($"[Combat] {name}(Team={myTeam}) -> {tObj?.name}({tgtTeam})"); // DEBUG
            target.TakeDamage(new BattleInfo { attackPower = attackPower });
        }
    }
}