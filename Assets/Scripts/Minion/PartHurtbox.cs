// 保存先: Assets/Scripts/Minion/PartHurtbox.cs
// 部位Hurtbox。被ダメ倍率・部位HP・部位破壊（→蓄積ひるみボーナス）・時間再生を持つ。
//   Hurtboxを継承するので、Hitbox・Core側（SetOwner）は部位を意識しない
//   （AccumulatedStagger : Stagger と同じ「継承で拡張、既存は無変更」の前例）。
//   設定はPartData(SO)を参照。同じSOを複数部位（脚×4）で共有でき、実行時の部位HPはこちらが個別に持つ。
//   部位破壊の効果は蓄積ひるみへのボーナスのみ（段階1）。破壊中も倍率は適用され続ける。
using System;
using UnityEngine;

public class PartHurtbox : Hurtbox
{
    [SerializeField] private PartData partData;

    private float currentPartHp; // 実行時の部位HP（SOは共有なのでインスタンス側が持つ）
    private float regenTimer;    // 破壊中の残り再生時間

    public bool IsBroken { get; private set; }
    public PartData Data => partData;
    public event Action OnPartBroken; // 部位破壊の瞬間（演出・将来のワザ封印用）

    private void Start()
    {
        if (partData == null)
        {
            Debug.LogError($"[PartHurtbox] PartDataが未設定です: {transform.root.name}/{name}");
            return;
        }
        currentPartHp = partData.partHp;
    }

    // 部位データを差し替える（魔族の部位進化でDemonCoreが押し込む。実行時状態もリセット＝部位は全快）。
    //   野生モンスターはプレハブのSerializeFieldのまま（押し込み不要）。
    public void SetData(PartData data)
    {
        partData = data;
        IsBroken = false;
        regenTimer = 0f;
        currentPartHp = data != null ? data.partHp : 0f;
    }

    public override void TakeHit(BattleInfo info)
    {
        if (partData != null)
        {
            // 部位倍率を攻撃力に乗算してから本体へ渡す（防御計算は従来どおり受け手Coreの責務）。
            //   BattleInfoは1ヒットごとにHitboxが新規生成するため、書き換えてよい。
            info.attackPower *= partData.damageMultiplier;

            // 部位HPを削る（倍率適用後・防御適用前の攻撃力ぶん減る簡易仕様）。0で部位破壊。
            if (partData.partHp > 0f && !IsBroken)
            {
                currentPartHp -= info.attackPower;
                if (currentPartHp <= 0f) Break();
            }
        }
        base.TakeHit(info); // Ownerへダメージを渡す（素通し部分は共通）
    }

    private void Break()
    {
        IsBroken = true;
        regenTimer = partData.regenTime;

        // 蓄積ひるみへボーナス（AccumulatedStaggerなら閾値到達で即大ひるみ）。
        //   持たない体では何もしない。通常Staggerの体に載せた場合は「ボーナス秒ぶんのけぞる」扱いになる点に注意。
        var stagger = GetComponentInParent<Stagger>();
        if (stagger != null) stagger.Apply(partData.breakStaggerBonus);

        OnPartBroken?.Invoke();
    }

    private void Update()
    {
        // 時間再生（遅い）。将来：睡眠で短縮。
        if (!IsBroken) return;
        regenTimer -= Time.deltaTime;
        if (regenTimer <= 0f)
        {
            IsBroken = false;
            currentPartHp = partData.partHp;
        }
    }
}
