// 保存先: Assets/Scripts/UI/StatBar.cs
using UnityEngine;
using UnityEngine.UI;

// 汎用ゲージ。種別(gaugeType)に対応する IGaugeSource を表示する。
//   ・GaugeAttacher が Prefab から生成し、SetGaugeType で種別を指定して使う。
//   ・単体でPrefab/シーンに置く場合は Inspector の gaugeType がそのまま使われる。
//   ・外枠＝国色(IGaugeSource.Team)、ゲージ(fill)＝緑、常時表示、頭上ビルボード。
public class StatBar : MonoBehaviour
{
    [SerializeField] private GaugeType gaugeType = GaugeType.Hp; // 表示する種別
    [SerializeField] private Image frameImage;  // 外枠（国色）
    [SerializeField] private Image fillImage;   // ゲージ本体（緑・Image Type=Filled, Horizontal）
    [SerializeField] private Color fillColor = Color.green;
    [SerializeField] private Color redTeamColor = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color blueTeamColor = new Color(0.2f, 0.4f, 0.9f);
    [SerializeField] private Color neutralColor = new Color(0.6f, 0.6f, 0.6f);

    private IGaugeSource source;
    private Camera cam;
    private bool resolved;

    // GaugeAttacher から種別を設定する（生成後に呼ぶ）。
    public void SetGaugeType(GaugeType type)
    {
        gaugeType = type;
        resolved = false; // 再解決させる
    }

    private void Start()
    {
        cam = Camera.main;
        if (fillImage != null) fillImage.color = fillColor;
        ResolveSource();
    }

    // 親方向から、指定種別の IGaugeSource を探す（Prefabは対象オブジェクトの子に生成される前提）。
    private void ResolveSource()
    {
        source = null;
        var sources = GetComponentsInParent<IGaugeSource>(true);
        foreach (var s in sources)
        {
            if (s.Type == gaugeType) { source = s; break; }
        }
        resolved = true;
    }

    private void LateUpdate()
    {
        if (!resolved) ResolveSource();
        if (source == null) return;

        if (fillImage != null)
        {
            float ratio = source.Max > 0f ? Mathf.Clamp01(source.Current / source.Max) : 0f;
            fillImage.fillAmount = ratio;
        }

        if (frameImage != null)
            frameImage.color = TeamColor(source.Team);

        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }

    private Color TeamColor(Team team)
    {
        switch (team)
        {
            case Team.Red: return redTeamColor;
            case Team.Blue: return blueTeamColor;
            default: return neutralColor;
        }
    }
}
