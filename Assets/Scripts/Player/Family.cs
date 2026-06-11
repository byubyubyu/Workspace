// 保存先: Assets/Scripts/Player/Family.cs
// 家系＝結婚・世代交代のオーケストレーション（GDDセクション15・人間側）。
//   ・配偶者は「個体値のスナップショット（float[]）」で保管する＝市民の実体参照を持たない
//     （市民が占拠等で消えても家系は影響を受けない。将来の家系図もスナップショットの蓄積で表現）。
//   ・求婚＝結納金（コイン）を払って成立（LineageDataの式：基本額＋相手の個体値合計×単価）。
//     支払いは瓶の記録から自動徴収（暫定。商人式の物理払いにするかは将来判断）。
//   ・結婚と同時に跡継ぎ誕生（確定仕様。出産までの時間・複数の子は将来の拡張余地）。
//   ・世代交代は PlayerSkills.SetInheritance / Age.ResetTo の公開APIを呼ぶだけ（Coreは触らない＝
//     遺伝値・年齢の変化は既存のOnChanged購読で自動再計算される）。
//   ・旧体は消滅（同一GameObjectの使い回し＝「乗り移り」の実装上の表現）。乗り移りは任意のみ（死亡では発動しない）。
//   ・婚活UI＝CitizenProfileUIController（Eで市民に話しかける）。デバッグ：F7＝最寄りの市民と無料で結婚／F8＝世代交代。
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerSkills))]
[RequireComponent(typeof(Age))]
public class Family : MonoBehaviour
{
    [SerializeField] private LineageData data;      // 遺伝減衰率・子の開始年齢・結納金の式
    [SerializeField] private float marryRange = 3f; // 求婚できる距離（デバッグF7の検索範囲）

    private PlayerSkills skills;
    private Age age;
    private PlayerCombatCore core;  // 死亡中の操作ガード用（参照のみ・変更しない）
    private InventoryHolder holder; // 結納金の支払い元（コイン集計・徴収）
    private float[] spouseValues;   // 配偶者の個体値スナップショット（null=未婚。遺伝式の母側）
    private bool hasChild;          // 跡継ぎがいるか（結婚と同時に誕生）

    public bool HasSpouse => spouseValues != null;
    public bool HasChild => hasChild;
    public event Action OnGenerationChanged; // 世代交代の通知（演出・将来の家系図用）

    private void Awake()
    {
        skills = GetComponent<PlayerSkills>();
        age = GetComponent<Age>();
        core = GetComponent<PlayerCombatCore>();
        holder = GetComponent<InventoryHolder>();
        if (data == null) Debug.LogError($"[Family] LineageData未設定: {name}");
    }

    // 結納金：基本額＋相手の個体値合計×単価（切り上げ）。
    public float PriceFor(CitizenSkills partner)
    {
        if (data == null || partner == null) return 0f;
        float total = 0f;
        for (int i = 0; i < partner.SkillCount; i++) total += partner.GetValue(i);
        return Mathf.Ceil(data.bridePriceBase + total * data.bridePricePerPoint);
    }

    // 所持コイン（瓶の記録＋未投入分のcurrencyValue合計。商人UIの所持金集計と同じ流儀）。
    public float CoinTotal
    {
        get
        {
            if (holder == null) return 0f;
            float total = 0f;
            foreach (var r in holder.Records) if (r.data != null) total += r.data.CurrencyValue;
            foreach (var p in holder.PendingItems) if (p != null) total += p.CurrencyValue;
            return total;
        }
    }

    // 求婚する：結納金が払えれば徴収して結婚（婚活UIから呼ばれる）。既婚中は不可（世代交代でリセット）。
    public bool TryPropose(CitizenSkills partner)
    {
        if (partner == null || HasSpouse) return false;
        float price = PriceFor(partner);
        if (!PayCoins(price))
        {
            Debug.Log($"[Family] 結納金 {price:F0} が払えない（所持 {CoinTotal:F0}）");
            return false;
        }
        Marry(partner);
        Debug.Log($"[Family] 結納金 {price:F0} を払った");
        return true;
    }

    // 結婚する（個体値をスナップショットで保管。結婚と同時に跡継ぎ誕生）。F7デバッグは無料でここを直接呼ぶ。
    public bool Marry(CitizenSkills partner)
    {
        if (partner == null) return false;
        spouseValues = new float[partner.SkillCount];
        for (int i = 0; i < partner.SkillCount; i++) spouseValues[i] = partner.GetValue(i);
        hasChild = true; // 結婚と同時に跡継ぎ誕生（確定仕様）
        Debug.Log($"[Family] 結婚した（跡継ぎ誕生）。相手の個体値: {DumpSpouse()}");
        return true;
    }

    // 世代交代：子に乗り移る（任意のみ。死亡では発動しない）。
    //   遺伝値[i] = (父=自分の実効値 + 母=配偶者スナップショット) ÷ 2 × 減衰率。鍛えた分はゼロから。
    public bool SucceedToChild()
    {
        if (data == null || skills == null || age == null) return false;
        if (core != null && core.IsDead) return false; // 死亡中は不可（死=復活であって世代交代ではない）
        if (!HasSpouse || !hasChild)
        {
            Debug.Log("[Family] 世代交代できない（配偶者と跡継ぎが必要）");
            return false;
        }

        var inheritedValues = new float[skills.SkillCount];
        for (int i = 0; i < skills.SkillCount; i++)
        {
            float father = skills.GetValue(i);                                       // 乗り移る瞬間の実効値＝遺伝＋教育
            float mother = i < spouseValues.Length ? spouseValues[i] : 0f;
            inheritedValues[i] = (father + mother) / 2f * data.inheritDecay;
        }

        skills.SetInheritance(inheritedValues); // 遺伝値設定＋鍛えた分ゼロ（OnChangedでCoreが自動再計算）
        age.ResetTo(data.childStartAge);        // 若返り（同上）
        spouseValues = null;                    // 新しい人生＝再婚活から
        hasChild = false;

        Debug.Log($"[Family] 世代交代：子（{data.childStartAge:F0}歳）に乗り移った。鍛えた分はゼロから");
        OnGenerationChanged?.Invoke();
        return true;
    }

    // 結納金の支払い：コイン（currencyValue>0）を記録→未投入分の順に、価値が額に達するまで取り除く。
    //   暫定：自動徴収（商人式の物理ドラッグ払いにするかは将来判断）。瓶を開いたまま呼ばない想定（UI側が他UIと排他）。
    private bool PayCoins(float price)
    {
        if (price <= 0f) return true;
        if (holder == null || CoinTotal < price) return false;

        float paid = 0f;
        for (int i = holder.Records.Count - 1; i >= 0 && paid < price; i--)
        {
            var d = holder.Records[i].data;
            if (d == null || d.CurrencyValue <= 0) continue;
            paid += d.CurrencyValue;
            holder.Records.RemoveAt(i);
        }
        for (int i = holder.PendingItems.Count - 1; i >= 0 && paid < price; i--)
        {
            var d = holder.PendingItems[i];
            if (d == null || d.CurrencyValue <= 0) continue;
            paid += d.CurrencyValue;
            holder.PendingItems.RemoveAt(i);
        }
        return true;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || (core != null && core.IsDead)) return;

        // デバッグ：F7＝最寄りの市民（個体値持ち）と無料で結婚。
        if (kb.f7Key.wasPressedThisFrame)
        {
            var partner = FindNearestCitizen();
            if (partner != null) Marry(partner);
            else Debug.Log($"[Family] 近く（{marryRange}m）に市民がいない");
        }

        // デバッグ：F8＝世代交代。
        if (kb.f8Key.wasPressedThisFrame)
            SucceedToChild();
    }

    // 範囲内の最寄りの市民（CitizenSkills持ち）を探す（デバッグ用）。
    private CitizenSkills FindNearestCitizen()
    {
        CitizenSkills nearest = null;
        float best = float.MaxValue;
        foreach (var c in FindObjectsByType<CitizenSkills>(FindObjectsSortMode.None))
        {
            float d = (c.transform.position - transform.position).sqrMagnitude;
            if (d < marryRange * marryRange && d < best) { best = d; nearest = c; }
        }
        return nearest;
    }

    private string DumpSpouse()
    {
        if (spouseValues == null) return "-";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < spouseValues.Length; i++)
            sb.Append(spouseValues[i].ToString("F0")).Append(i < spouseValues.Length - 1 ? "/" : "");
        return sb.ToString();
    }
}
