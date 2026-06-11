// 保存先: Assets/Scripts/Demon/DemonSoul.cs
// 魂ポイント（魔族の恒久成長・GDDセクション15）。死んでも消えない（転生を跨いで保持）。
//   ・捕食ptがラン内リソース（死で喪失）なのに対し、こちらは魂のリソース＝素体の解放に使う。
//   ・獲得：Devourer.OnDevoured を購読し、SoulData の換算・倍率で加算する
//     （Devourerは魂システムの存在を知らない＝イベントで疎結合）。
//   ・消費はしない（解放は「累計の到達」で判定。消費式にするかは将来の死の恐怖対策レバーで検討）。
using System;
using UnityEngine;

public class DemonSoul : MonoBehaviour
{
    [SerializeField] private SoulData data; // 獲得ルール（換算レート・出自別倍率）

    private Devourer devourer;

    public float Points { get; private set; }
    public event Action OnChanged;

    // 素体が解放済みか（累計ポイントが必要値に到達しているか）。
    public bool IsUnlocked(BodyData body) => body != null && Points >= body.RequiredSoulPoints;

    private void Awake()
    {
        if (data == null) Debug.LogError($"[DemonSoul] SoulDataが未設定です: {name}");
        devourer = GetComponent<Devourer>();
        if (devourer != null) devourer.OnDevoured += HandleDevoured;
    }

    private void OnDestroy()
    {
        if (devourer != null) devourer.OnDevoured -= HandleDevoured;
    }

    // 捕食通知→魂ポイントへ換算（倍率の適用判断はコンポーネント側の責務。SOは数値のみ）。
    private void HandleDevoured(Corpse corpse, float nutrition)
    {
        if (data == null) return;
        bool isHuman = corpse != null && corpse.SourceTeam != Team.None; // 国に属する死体＝人間側
        float weight = isHuman ? data.humanWeight : data.wildWeight;
        Add(nutrition * data.pointsPerNutrition * weight);
    }

    // 直接加算（デバッグ・将来の獲得源追加用の公開口）。
    public void Add(float amount)
    {
        if (amount <= 0f) return;
        Points += amount;
        OnChanged?.Invoke();
    }
}
