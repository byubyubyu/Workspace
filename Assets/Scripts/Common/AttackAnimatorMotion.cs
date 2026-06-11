using System;
using UnityEngine;

// 見た目モデル用：親のAttackのフェーズを監視し、技に対応する攻撃アニメを
// 上半身レイヤーで再生する。Attack本体は時間タイマー駆動のまま（読むだけ・無変更）。
// クリップ再生速度を技のTotalTimeに合わせるので、当たり判定とアニメが同期する。
[RequireComponent(typeof(Animator))]
public class AttackAnimatorMotion : MonoBehaviour
{
    [Serializable]
    public class MotionEntry
    {
        public string motionId;  // AttackMove.motionId と一致したらこのステートを使う
        public string stateName; // Animator上半身レイヤーのステート名
    }

    [SerializeField] private string layerName = "UpperBody";
    [SerializeField] private string emptyState = "UpperEmpty";
    [SerializeField] private string stanceState = "WeaponStance"; // 攻撃終了後、武器構え中ならここへ戻る
    [SerializeField] private string fallbackState = "Attack_OneHand"; // motionId不一致・空のときの既定
    [SerializeField] private MotionEntry[] entries;
    [SerializeField] private float crossFadeIn = 0.05f;
    [SerializeField] private float crossFadeOut = 0.15f;

    private static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed");

    private Animator animator;
    private Attack attack;
    private PlayerHandState handState; // 構え戻り判定用（プレイヤー以外はnullのまま）
    private int layerIndex = -1;
    private Attack.Phase prevPhase = Attack.Phase.None;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        attack = GetComponentInParent<Attack>();
        handState = GetComponentInParent<PlayerHandState>();
    }

    private void Start()
    {
        layerIndex = animator.GetLayerIndex(layerName);
    }

    private void Update()
    {
        if (attack == null || layerIndex < 0) return;

        Attack.Phase phase = attack.CurrentPhase;
        if (prevPhase == Attack.Phase.None && phase != Attack.Phase.None)
            Play(attack.CurrentMove);
        else if (prevPhase != Attack.Phase.None && phase == Attack.Phase.None)
        {
            // 攻撃終了＝上半身レイヤーを空ける（構え中の全身はベースのGSLocomotionが担当）。
            animator.CrossFadeInFixedTime(emptyState, crossFadeOut, layerIndex);
        }
        prevPhase = phase;
    }

    private void Play(AttackMove move)
    {
        string state = fallbackState;
        if (move != null && !string.IsNullOrEmpty(move.motionId) && entries != null)
        {
            foreach (MotionEntry e in entries)
            {
                if (e.motionId == move.motionId) { state = e.stateName; break; }
            }
        }

        float total = move != null ? move.TotalTime : 0f;
        float clipLen = FindClipLength(state);
        animator.SetFloat(AttackSpeedHash, total > 0f && clipLen > 0f ? clipLen / total : 1f);
        animator.CrossFadeInFixedTime(state, crossFadeIn, layerIndex, 0f);
    }

    private float FindClipLength(string clipName)
    {
        foreach (AnimationClip c in animator.runtimeAnimatorController.animationClips)
        {
            if (c.name == clipName) return c.length;
        }
        return 0f;
    }
}
