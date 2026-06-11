// 保存先: Assets/Scripts/Minion/MinionCore.cs
using System;
using UnityEngine;

public class MinionCore : MonoBehaviour, IBattleInfo, IHealth
{
    private Health health;
    private float defense; // 受け手側が持つ防御力（VitalityData.defense）
    private StateMachine stateMachine;
    private IState[] states;
    private Vision vision; // 状態選択の直前にRefreshを駆動するため保持
    public Team Team { get; private set; }
    public bool IsDead => health != null && health.IsEmpty;
    public Vector3 Position => transform.position; // IBattleInfo

    // IHealth（ゲージ表示用）。中身はHealthに委譲する。
    public float Current => health != null ? health.Current : 0f;
    public float Max => health != null ? health.Max : 0f;

    public event Action OnDestroyed;
    public event Action OnArrived;

    public void Initialize(IMinionData data, Team team)
    {
        Team = team;

        // VitalityData は必須（HP・防御の土台）。欠けていたら名指しで報告し、即死を避けるため保険を入れる。
        if (data.Vitality == null)
        {
            Debug.LogError($"[MinionCore] VitalityData が欠けています: {name}");
            health = new Health(1f);
            defense = 0f;
        }
        else
        {
            health = new Health(data.Vitality.hp);
            defense = data.Vitality.defense;
        }

        // B-2: データ由来の初期設定を各コンポーネントに流し込む。
        //   コンポーネント基準でSO欠けを検出する（コンポーネントありSO欠けは黙殺せず名指し）。
        var movement = GetComponent<Movement>();
        if (movement != null)
        {
            if (data.Movement == null) Debug.LogError($"[MinionCore] Movementあり、MovementData欠け: {name}");
            else movement.Initialize(data.Movement, this);
        }

        vision = GetComponent<Vision>();
        if (vision != null)
        {
            if (data.Vision == null) Debug.LogError($"[MinionCore] Visionあり、VisionData欠け: {name}");
            else vision.Initialize(data.Vision, team);
        }

        var attack = GetComponent<Attack>();
        if (attack != null)
        {
            if (data.Attack == null) Debug.LogError($"[MinionCore] Attackあり、AttackData欠け: {name}");
            else attack.Initialize(data.Attack);
        }

        var builder = GetComponent<Builder>();
        if (builder != null)
        {
            if (data.Builder == null) Debug.LogError($"[MinionCore] Builderあり、BuilderData欠け: {name}");
            else builder.Initialize(data.Builder);
        }

        var stamina = GetComponent<Stamina>();
        if (stamina != null)
        {
            if (data.Stamina == null) Debug.LogError($"[MinionCore] Staminaあり、StaminaData欠け: {name}");
            else stamina.Initialize(data.Stamina, team);
        }

        var dodge = GetComponent<Dodge>();
        if (dodge != null)
        {
            if (data.Dodge == null) Debug.LogError($"[MinionCore] Dodgeあり、DodgeData欠け: {name}");
            else dodge.Initialize(data.Dodge);
        }

        // 子のHurtbox（部位制なら複数）に自分(IBattleInfo)を渡す（HitboxがここからCoreを取得してダメージを渡す）。
        foreach (var hurtbox in GetComponentsInChildren<Hurtbox>(true))
            hurtbox.SetOwner(this);

        // 初期インベントリ（瓶の中身）を InventoryHolder に渡す（持つ兵士のみ・死体を漁る用）。
        var holder = GetComponent<InventoryHolder>();
        if (holder != null) holder.SetInitialItems(data.InitialItems);

        states = GetComponents<IState>();
        stateMachine = new StateMachine(states);
        foreach (var state in states)
            state.Initialize(stateMachine);
    }

    public void TakeDamage(BattleInfo info)
    {
        // 受け手が自分のdefenseでダメージを計算する（防御計算は受け手側の責務）。
        float damage = DamageCalculator.Calc(info.attackPower, defense);
        health.TakeDamage(damage);

        // ひるみを発動する。Staggerコンポーネントを持つ兵士のみ（持たない兵士はひるまない）。
        if (info.staggerDuration > 0f)
        {
            var stagger = GetComponent<Stagger>();
            if (stagger != null) stagger.Apply(info.staggerDuration);
        }

        // HPを減らすのみ。HP0(IsEmpty)になると次フレームDeadState(最優先)が選ばれDie()する。
    }

    public void Die()
    {
        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }

    public void NotifyArrived()
    {
        OnArrived?.Invoke();
    }

    private void Update()
    {
        // 状態選択の直前にVisionの検出を駆動する（プル型を明示・更新順の非保証を排除）。
        //   生成直後の初回フレームから敵を検出済みにできる＝湧いた瞬間に敵がいれば戦闘に入る。
        vision?.Refresh();
        stateMachine?.Update();
    }
}
