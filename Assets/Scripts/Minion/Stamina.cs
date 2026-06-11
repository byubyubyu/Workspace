// 保存先: Assets/Scripts/Minion/Stamina.cs
// スタミナの実体。時間回復する「数値の器(Resource)」を内部に持つ自走コンポーネント。
//   ・回避(Dodge・C-2)等がConsumeで消費する。消費後recoveryDelay秒は回復しない（案2）。
//   ・プレイヤー・兵士で共通の実体（同じコンポーネントを両方に載せられる）。
//   ・自分のUpdateで回復する（Attack/Vision/Staggerと同じ自走パターン）。
//   ・CostPoolと同じ「Resource＋時間回復」の構造。HealthはResource＋ダメージ。
using UnityEngine;

public class Stamina : MonoBehaviour
{
    private Resource resource;
    private float recovery;       // 毎秒の回復量
    private float recoveryDelay;  // 使用後、回復が止まる秒数
    private float delayTimer;     // 残りの回復停止時間（0以下で回復再開）
    private float baseMax;        // SO由来の素の最大値（乗数の再計算用）
    private float maxScale = 1f;  // 最大値への乗数（加齢など。1=等倍）
    private Team team;

    // ゲージ表示用（StaminaGaugeSourceが読む）。
    public float Current => resource != null ? resource.Current : 0f;
    public float Max => resource != null ? resource.Max : 0f;
    public Team Team => team;

    // SO・所属を受け取って初期化。満タンから始める。MinionCoreから呼ばれる。
    public void Initialize(StaminaData data, Team team)
    {
        this.team = team;
        recovery = data.staminaRecovery;
        recoveryDelay = data.recoveryDelay;
        baseMax = data.staminaMax;
        maxScale = 1f;
        resource = new Resource(baseMax, baseMax); // 満タン開始
        delayTimer = 0f;
    }

    // 最大値への乗数を適用する（加齢の老衰など）。現在値の割合を維持して作り直す。
    public void SetMaxScale(float scale)
    {
        if (resource == null || baseMax <= 0f) return;
        scale = Mathf.Max(0.01f, scale);
        if (Mathf.Approximately(scale, maxScale)) return;
        float ratio = Max > 0f ? Current / Max : 1f;
        maxScale = scale;
        float newMax = baseMax * maxScale;
        resource = new Resource(newMax, newMax);
        resource.Add(-newMax * (1f - ratio)); // 割合維持（Healthと同じ作り直し方）
    }

    private void Update()
    {
        if (resource == null) return;

        // 使用後の回復遅延中は回復しない（案2）。
        if (delayTimer > 0f)
        {
            delayTimer -= Time.deltaTime;
            return;
        }

        // 遅延を過ぎたら毎秒recoveryずつ回復（Resourceがmaxでクランプ）。
        if (!resource.IsFull)
            resource.Add(recovery * Time.deltaTime);
    }

    // 払えれば消費してtrue（アトミック）。成功時は回復遅延を再セットする（連続使用を抑制）。
    public bool Consume(float amount)
    {
        if (resource == null) return false;
        if (!resource.Consume(amount)) return false;
        delayTimer = recoveryDelay;
        return true;
    }

    public bool CanAfford(float amount) => resource != null && resource.CanAfford(amount);
}
