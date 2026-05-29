using UnityEngine;

public class Attack : MonoBehaviour
{
    private float attackPower;
    private float attackInterval;
    private float attackTimer;
    private IBattleInfo target;

    public void Initialize(IMinionData data)
    {
        attackPower = data.Stat.attackPower;
        attackInterval = data.Stat.attackInterval;
    }

    public void SetTarget(IBattleInfo target)
    {
        this.target = target;
    }

    private void Update()
    {
        if (target == null) return;

        attackTimer += Time.deltaTime;
        if (attackTimer >= attackInterval)
        {
            attackTimer = 0f;
            target.TakeDamage(new BattleInfo { attackPower = attackPower });
        }
    }
}
