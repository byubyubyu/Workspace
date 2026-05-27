using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private FloatEventChannel onHPChanged;
    [SerializeField] private EventChannel onDamaged;
    [SerializeField] private EventChannel onHealed;
    [SerializeField] private EventChannel onDeath;

    private float maxHP;
    private float currentHP;
    private StatCalculator calculator;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;

    void Start()
    {
        calculator = GetComponent<StatCalculator>();

        if (calculator != null)
            maxHP = calculator.GetStat(StatType.HP);

        currentHP = maxHP;
        onHPChanged?.Raise(currentHP);
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);
        onHPChanged?.Raise(currentHP);
        onDamaged?.Raise();

        if (currentHP <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        currentHP += amount;
        currentHP = Mathf.Min(currentHP, maxHP);
        onHPChanged?.Raise(currentHP);
        onHealed?.Raise();
    }

    private void Die()
    {
        onDeath?.Raise();
        Destroy(gameObject);
    }
    public void AddDeathListener(System.Action listener)
    {
        onDeath?.AddListener(listener);
    }

    public void RemoveDeathListener(System.Action listener)
    {
        onDeath?.RemoveListener(listener);
    }
}