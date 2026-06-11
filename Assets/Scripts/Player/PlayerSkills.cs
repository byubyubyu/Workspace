// 保存先: Assets/Scripts/Player/PlayerSkills.cs
// 人間スキルの実体（GDDセクション15・UO型）。スキルごとの実行時値と成長・キャップ処理を持つ。
//   ・実効値 ＝ 遺伝値（下限・キャップ外。④世代交代で設定。今は0） ＋ 鍛えた分（合計≦キャップ）。
//   ・成長は使用ベース：PlayerCombatCore.OnDamaged（被弾）と Hitbox.OnDealtHit（命中）を購読し、
//     各SkillDataの gainOnHit / gainOnDamaged で伸ばす（このクラスは「何のスキルがあるか」を知らない）。
//   ・キャップ到達時は上げ下げ指定（Raise/Lock/Lower）：↑が伸びるぶん↓から削る（UO式）。
//   ・悪用対策：値が高いほど成長が鈍化＋同一相手とのやり取りは逓減。
//   ・効果のΣ（HP・攻撃・被ダメ軽減）を公開し、PlayerCombatCoreがOnChangedを購読して再計算する
//     （こちらからCoreは参照しない＝一方向）。
//   ・F11＝スキル値のログ出力（デバッグ）。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSkills : MonoBehaviour
{
    public enum GrowthMode { Raise, Lock, Lower } // ↑伸ばす／🔒固定／↓下げてよい

    [SerializeField] private SkillCatalog catalog;
    [SerializeField] private EquipmentHolder equipmentHolder; // 武器が替わるとHitboxの購読を張り直す用（任意）

    private float[] trained;     // 鍛えた分（合計がキャップ対象）
    private float[] inherited;   // 遺伝値（下限・キャップ外。④世代交代で設定）
    private GrowthMode[] modes;
    private PlayerCombatCore core;
    private Hitbox hitbox;       // 命中XPの購読先（自分の子のHitbox）
    private readonly Dictionary<IBattleInfo, int> familiarity = new Dictionary<IBattleInfo, int>(); // 同一相手の逓減カウント

    public event Action OnChanged;

    public int SkillCount => catalog != null ? catalog.Skills.Count : 0;
    public float GetValue(int skillId) =>
        catalog != null && skillId >= 0 && skillId < SkillCount ? inherited[skillId] + trained[skillId] : 0f;
    public GrowthMode GetMode(int skillId) => modes != null && skillId < modes.Length ? modes[skillId] : GrowthMode.Raise;
    public void SetMode(int skillId, GrowthMode mode) { if (modes != null && skillId < modes.Length) modes[skillId] = mode; }

    // 表示用の読み取り口（ステータスUIが使う）。
    public float GetInherited(int skillId) =>
        inherited != null && skillId >= 0 && skillId < inherited.Length ? inherited[skillId] : 0f;
    public string GetSkillName(int skillId) =>
        catalog != null && skillId >= 0 && skillId < SkillCount && catalog.Skills[skillId] != null ? catalog.Skills[skillId].skillName : "";
    public float TotalTrainCap => catalog != null ? catalog.TotalTrainCap : 0f;

    // 世代交代：遺伝値を設定し、鍛えた分をゼロに戻す（新しい人生＝キャップが丸ごと空く。Familyから呼ばれる）。
    //   遺伝値は下限・下げ不可・キャップ外（A確定の二層モデル）。
    public void SetInheritance(float[] inheritedValues)
    {
        if (trained == null) return;
        for (int i = 0; i < SkillCount; i++)
        {
            inherited[i] = inheritedValues != null && i < inheritedValues.Length
                ? Mathf.Clamp(inheritedValues[i], 0f, catalog.MaxSkillValue) : 0f;
            trained[i] = 0f;
            modes[i] = GrowthMode.Raise;
        }
        familiarity.Clear(); // 相手の慣れも新しい人生でリセット
        OnChanged?.Invoke();
    }

    // 効果のΣ（実効値×SkillDataの係数）。PlayerCombatCoreが読む。
    //   bodilyMultiplier＝肉体系スキルだけに掛かる倍率（加齢の老衰など。技術系は満額）。
    //   倍率の出どころ（加齢かバフか）をこのクラスは関知しない＝疎結合。
    public float TotalHpBonus => GetHpBonus(1f);
    public float TotalAttackBonus => GetAttackBonus(1f);
    public float TotalDamageCut => GetDamageCut(1f);
    public float GetHpBonus(float bodilyMultiplier) => Sum(s => s.hpPerPoint, bodilyMultiplier);
    public float GetAttackBonus(float bodilyMultiplier) => Sum(s => s.attackPerPoint, bodilyMultiplier);
    public float GetDamageCut(float bodilyMultiplier) => Sum(s => s.damageCutPerPoint, bodilyMultiplier);

    private float Sum(Func<SkillData, float> selector, float bodilyMultiplier)
    {
        if (catalog == null || trained == null) return 0f;
        float total = 0f;
        for (int i = 0; i < SkillCount; i++)
        {
            var skill = catalog.Skills[i];
            if (skill == null) continue;
            float mult = skill.isBodily ? bodilyMultiplier : 1f;
            total += GetValue(i) * selector(skill) * mult;
        }
        return total;
    }

    private void Awake()
    {
        if (catalog == null) { Debug.LogError($"[PlayerSkills] SkillCatalog未設定: {name}"); return; }
        trained = new float[SkillCount];
        inherited = new float[SkillCount];
        modes = new GrowthMode[SkillCount]; // 既定は全部↑（キャップに届くまでは全部伸びる）
        core = GetComponent<PlayerCombatCore>();
        if (equipmentHolder == null) equipmentHolder = GetComponent<EquipmentHolder>();
    }

    private void Start()
    {
        if (core != null) core.OnDamaged += HandleDamaged;
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged += RehookHitbox;
        RehookHitbox();
    }

    private void OnDestroy()
    {
        if (core != null) core.OnDamaged -= HandleDamaged;
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged -= RehookHitbox;
        if (hitbox != null) hitbox.OnDealtHit -= HandleDealtHit;
    }

    // 自分の子のHitboxへ命中イベントを張り直す（武器替えでHitboxが変わる可能性に備える）。
    private void RehookHitbox()
    {
        var current = GetComponentInChildren<Hitbox>(true);
        if (current == hitbox) return;
        if (hitbox != null) hitbox.OnDealtHit -= HandleDealtHit;
        hitbox = current;
        if (hitbox != null) hitbox.OnDealtHit += HandleDealtHit;
    }

    // 命中で伸びる（剣技・肉体）。建物相手は鍛錬にならない（兵士・魔族・人間のみ）。
    private void HandleDealtHit(IBattleInfo victim)
    {
        if (victim is BuildingCore) return;
        GainAll(s => s.gainOnHit, victim);
    }

    // 被弾で伸びる（防御・肉体）。攻撃者で逓減する。
    private void HandleDamaged(BattleInfo info)
    {
        GainAll(s => s.gainOnDamaged, info != null ? info.attacker : null);
    }

    // 全スキルに成長イベントを配る（伸び量はSkillDataの数値が決める＝コード分岐なし）。
    private void GainAll(Func<SkillData, float> gainSelector, IBattleInfo opponent)
    {
        if (catalog == null || trained == null) return;

        // 同一相手との繰り返しは逓減（×0.85^回数）＝わざと殴られ続ける悪用の対策。
        float opponentFactor = 1f;
        if (opponent != null)
        {
            familiarity.TryGetValue(opponent, out int count);
            familiarity[opponent] = count + 1;
            opponentFactor = Mathf.Pow(0.85f, count);
        }

        bool changed = false;
        for (int i = 0; i < SkillCount; i++)
        {
            var skill = catalog.Skills[i];
            if (skill == null || modes[i] != GrowthMode.Raise) continue;
            float baseGain = gainSelector(skill);
            if (baseGain <= 0f) continue;

            // 値が高いほど自然に鈍化（実効値/上限で減衰）。
            float slow = 1f - Mathf.Clamp01(GetValue(i) / catalog.MaxSkillValue);
            float amount = baseGain * slow * opponentFactor;
            if (amount <= 0f) continue;
            if (TryTrain(i, amount)) changed = true;
        }
        if (changed) OnChanged?.Invoke();
    }

    // 鍛えた分を増やす。スキル上限・合計キャップを守り、キャップ超過分は↓指定のスキルから削る（UO式）。
    private bool TryTrain(int skillId, float amount)
    {
        // スキル個別上限（実効値≦MaxSkillValue）。
        amount = Mathf.Min(amount, catalog.MaxSkillValue - GetValue(skillId));
        if (amount <= 0f) return false;

        // 合計キャップの空き。足りなければ↓指定のスキルから削って空ける。
        float used = 0f;
        for (int i = 0; i < SkillCount; i++) used += trained[i];
        float free = catalog.TotalTrainCap - used;
        if (free < amount)
        {
            float need = amount - free;
            for (int i = 0; i < SkillCount && need > 0f; i++)
            {
                if (i == skillId || modes[i] != GrowthMode.Lower || trained[i] <= 0f) continue;
                float take = Mathf.Min(trained[i], need); // 遺伝値(inherited)は削らない＝下限保護
                trained[i] -= take;
                need -= take;
            }
            amount -= Mathf.Max(0f, need); // 空け切れなかったぶんは伸びない
            if (amount <= 0f) return false;
        }

        trained[skillId] += amount;
        return true;
    }

    private void Update()
    {
        // デバッグ：F11＝スキル値のダンプ。
        var kb = Keyboard.current;
        if (kb != null && kb.f11Key.wasPressedThisFrame && catalog != null && trained != null)
        {
            float used = 0f;
            for (int i = 0; i < SkillCount; i++) used += trained[i];
            var sb = new System.Text.StringBuilder($"[PlayerSkills] キャップ {used:F1}/{catalog.TotalTrainCap:F0}\n");
            for (int i = 0; i < SkillCount; i++)
                sb.AppendLine($"  {catalog.Skills[i]?.skillName}: 実効{GetValue(i):F1}（遺伝{inherited[i]:F1}＋鍛錬{trained[i]:F1}）[{modes[i]}]");
            Debug.Log(sb.ToString());
        }
    }
}
