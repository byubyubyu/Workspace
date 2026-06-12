// 保存先: Assets/Scripts/Player/SwordTrail.cs
// 剣先トレイル（斬撃軌跡）。武器の剣先に置いたTrailRendererを、攻撃の判定フェーズ（Active）中だけ発光させる。
//   Attack.CurrentPhase（読み取り専用公開）をポーリングするだけ＝既存コードへの変更なし・疎結合。
//   剣先の子オブジェクト（TrailRenderer同居）に付ける。attack参照は未設定なら親から自動取得。
//   ※ 実際の振りのボーン軌道をそのままなぞるので、モーションと軌跡が必ず一致する。
using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class SwordTrail : MonoBehaviour
{
    [SerializeField] private Attack attack;               // 未設定なら親（Player本体）から取得
    [SerializeField] private bool includeWindup = false;  // 振りかぶり中も光らせるか（既定＝判定中のみ）

    private TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        if (attack == null) attack = GetComponentInParent<Attack>();
        trail.emitting = false;
    }

    private void Update()
    {
        if (attack == null || trail == null) return;
        var phase = attack.CurrentPhase;
        bool active = phase == Attack.Phase.Active || (includeWindup && phase == Attack.Phase.Windup);
        if (trail.emitting != active) trail.emitting = active;
    }
}
