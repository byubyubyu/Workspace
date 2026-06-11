using UnityEngine;

// 見た目モデル用：PlayerHandStateの状態を監視し、武器構え中は上半身レイヤーで
// 構えモーションをループ再生する（攻撃中はAttackAnimatorMotionに譲る）。
[RequireComponent(typeof(Animator))]
public class HandStanceMotion : MonoBehaviour
{
    [SerializeField] private string layerName = "UpperBody";
    [SerializeField] private string baseLocomotionState = "Locomotion";     // 通常移動（ベースレイヤー）
    [SerializeField] private string weaponLocomotionState = "GSLocomotion"; // 武器構え中の移動（ベースレイヤー）
    [SerializeField] private string stanceState = "WeaponStance";
    [SerializeField] private string drawState = "DrawSword"; // 抜刀動作（Drawing中に再生。ステート側のspeedでdrawDurationに合わせる）
    [SerializeField] private string sheathState = "SheathSword"; // 納刀動作（再生後はステート側の遷移でemptyへ）
    [SerializeField] private string emptyState = "UpperEmpty";
    [SerializeField] private float crossFade = 0.2f;

    private Animator animator;
    private PlayerHandState handState;
    private Attack attack;
    private int layerIndex = -1;
    private HandState prevState = HandState.Empty;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        handState = GetComponentInParent<PlayerHandState>();
        attack = GetComponentInParent<Attack>();
    }

    private void Start()
    {
        layerIndex = animator.GetLayerIndex(layerName);
    }

    private void Update()
    {
        if (handState == null || layerIndex < 0) return;
        HandState state = handState.State;
        if (state != prevState && (attack == null || !attack.IsAttacking))
        {
            if (state == HandState.Drawing)
                animator.CrossFadeInFixedTime(drawState, 0.05f, layerIndex, 0f); // 抜刀動作（頭から再生）
            else if (state == HandState.Weapon)
            {
                // 構え中はベースのGSLocomotion（全身）に任せ、上半身レイヤーは空ける
                // （上半身の構えループを重ねると構え歩きの上体と喧嘩するため）。
                animator.CrossFadeInFixedTime(emptyState, crossFade, layerIndex);
                animator.CrossFadeInFixedTime(weaponLocomotionState, crossFade, 0);
            }
            else if (prevState == HandState.Weapon)
            {
                animator.CrossFadeInFixedTime(sheathState, 0.05f, layerIndex, 0f); // 納刀動作（終了後はステート遷移でemptyへ）
                animator.CrossFadeInFixedTime(baseLocomotionState, crossFade, 0);  // 脚は通常移動へ戻す
            }
            else if (prevState == HandState.Drawing)
                animator.CrossFadeInFixedTime(emptyState, crossFade, layerIndex); // 抜刀キャンセルは即戻し
        }
        prevState = state;
    }
}
