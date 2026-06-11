// 保存先: Assets/Scripts/Player/Age.cs
// 加齢の実体（GDDセクション15・人間側）。年齢の進行と能力倍率の供給「だけ」を担う。
//   ・誰の能力も直接触らない：倍率が一定刻み（0.01）変わった時にOnChangedを発火し、
//     PlayerCombatCoreが購読して実効値を再計算する（一方向・疎結合）。
//   ・スキル値は落とさない（落ちるのは身体＝倍率の適用先はCore側が決める）。
//   ・死んでも歳は戻らない（戻るのは世代交代のみ・将来）。魔族は加齢なし（このコンポーネントを持たない）。
//   ・F12＝+10歳のデバッグキー（検証用）。
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class Age : MonoBehaviour
{
    [SerializeField] private AgingData data;

    private float ageYears;
    private float lastNotifiedMultiplier = 1f;
    private string currentStage = "";

    public float CurrentAge => ageYears;
    public float Multiplier { get; private set; } = 1f;
    public string StageLabel => currentStage;
    public event Action OnChanged; // 倍率が意味のある量（0.01）変わった時に発火

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"[Age] AgingData未設定: {name}");
            return;
        }
        ageYears = data.startAge;
        Multiplier = data.multiplierByAge.Evaluate(ageYears);
        lastNotifiedMultiplier = Multiplier;
        currentStage = StageFor(ageYears);
    }

    private void Update()
    {
        if (data == null) return;

        // デバッグ：F12＝+10歳。
        var kb = Keyboard.current;
        if (kb != null && kb.f12Key.wasPressedThisFrame)
        {
            ageYears += 10f;
            Debug.Log($"[Age] +10歳 → {ageYears:F1}歳");
        }

        ageYears += Time.deltaTime / Mathf.Max(0.01f, data.secondsPerYear);
        Multiplier = data.multiplierByAge.Evaluate(ageYears);

        // 段階ラベルの変化（壮年→中年など）はログで知らせる（UI表示は将来）。
        string stage = StageFor(ageYears);
        if (stage != currentStage)
        {
            currentStage = stage;
            Debug.Log($"[Age] {ageYears:F0}歳：{stage}になった（能力倍率 {Multiplier:P0}）");
        }

        // 毎フレームの再計算を避け、倍率が一定刻み動いた時だけ通知する（HP再構築等は重め）。
        if (Mathf.Abs(Multiplier - lastNotifiedMultiplier) >= 0.01f)
        {
            lastNotifiedMultiplier = Multiplier;
            OnChanged?.Invoke();
        }
    }

    // 年齢を設定し直す（世代交代＝子に乗り移った時。Familyから呼ばれる）。倍率を即時再評価して通知する。
    public void ResetTo(float years)
    {
        if (data == null) return;
        ageYears = years;
        Multiplier = data.multiplierByAge.Evaluate(ageYears);
        lastNotifiedMultiplier = Multiplier;
        currentStage = StageFor(ageYears);
        OnChanged?.Invoke();
    }

    // 年齢に対応する段階ラベルを返す（SOはデータのみ・判断はコンポーネント側）。
    private string StageFor(float age)
    {
        string label = "";
        if (data.stages == null) return label;
        foreach (var s in data.stages)
            if (s != null && age >= s.fromAge) label = s.label;
        return label;
    }
}
