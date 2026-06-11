// 保存先: Assets/Scripts/Minion/Attack.cs
// アクション戦闘の攻撃実体（プレイヤー・兵士・モンスター共通の「振る」動作）。
//   StartAttack()で開始し、3フェーズ（前隙→判定→後隙）を自分のUpdateで自走する。
//   今の向きに振る（引数なし・自動で吸い付かない）。判定フェーズ中だけHitboxを有効化する。
//   フェーズ管理は初期=時間タイマー（方法A）。将来アニメーション駆動（方法B）へ差し替え可。
//   実体とStateの独立性：進行中の攻撃はState遷移では止まらない（ひるみ/回避後隙のForceCancelを除く）。
//
//   技システム：AttackData.moves（AttackMove SOのリスト）を全件保持し、StartAttack(技番号)で
//   技を指定して振れる。威力倍率・フェーズ時間・間合い・スタミナコスト・ひるませ値・モーションは技ごと。
//   引数なし StartAttack() は技0＝従来互換（兵士・プレイヤーは無変更で動く）。
//   モーション連動用に CurrentMove / CurrentPhase / PhaseProgress を読み取り専用で公開する。
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    public enum Phase { None, Windup, Active, Recovery } // モーション連動（Motionコンポーネント）が読むためpublic

    [SerializeField] private Hitbox hitbox; // 子のHitbox（Inspectorでアサイン推奨。未設定ならInChildrenで取得）

    private float baseAttackPower;      // AttackData.attackPower（実威力 = これ × 技のpowerMultiplier）
    private List<AttackMove> moves;     // 技リスト（SO参照。値の実体はSOが持ち、ここはどの技を振るかだけ管理）
    private AttackMove currentMove;     // 進行中（直近）の技

    private Phase phase = Phase.None;
    private float phaseTimer;
    private IBattleInfo target; // 立ち回り用の参照（攻撃自体は今の向きに振るので命中には使わない）
    private Dodge dodge;        // 回避中は攻撃不可の判定用（兄弟コンポーネント・無い兵士はnull）
    private Stamina stamina;    // 攻撃時にスタミナを消費する（兄弟コンポーネント・持たないエンティティはnull）

    // AI判断用の間合いは技0基準（従来互換。複数技の使い分けは将来Strategy側で技ごとのreachを読む）。
    public float AttackRange => moves != null && moves.Count > 0 && moves[0] != null ? moves[0].reach : 1f;
    public bool IsAttacking => phase != Phase.None;
    public bool CanCancel => phase == Phase.Recovery; // 後隙のみキャンセル可

    // --- モーション連動用の読み取り専用公開（挙動には影響しない） ---
    public Phase CurrentPhase => phase;
    public AttackMove CurrentMove => currentMove;
    // 現在フェーズの進行率(0〜1)。Motionコンポーネントが振りかぶり/振り下ろしの補間に使う。
    public float PhaseProgress
    {
        get
        {
            if (currentMove == null) return 0f;
            float duration = phase switch
            {
                Phase.Windup => currentMove.windupTime,
                Phase.Active => currentMove.activeTime,
                Phase.Recovery => currentMove.recoveryTime,
                _ => 0f,
            };
            return duration > 0f ? Mathf.Clamp01(phaseTimer / duration) : 1f;
        }
    }

    public void Initialize(AttackData data)
    {
        Initialize(data.attackPower, data.moves);
    }

    // 技リストを直接渡す版（魔族＝部位から集約したワザを注入する。兵士・人間はAttackData版のまま）。
    public void Initialize(float attackPower, List<AttackMove> moveList)
    {
        baseAttackPower = attackPower;

        // 体の差し替え（魔族の進化＝旧Hitboxは親から外れて破棄予定）があるため、自分の子でない参照は拾い直す。
        if (hitbox == null || !hitbox.transform.IsChildOf(transform))
            hitbox = GetComponentInChildren<Hitbox>(true);
        if (hitbox != null) hitbox.Setup(GetComponent<IBattleInfo>()); // 兵士=MinionCore / 人間=PlayerCombatCore / 魔族=DemonCore
        dodge = GetComponent<Dodge>();
        stamina = GetComponent<Stamina>();

        moves = moveList;
        if (moves == null || moves.Count == 0 || moves[0] == null)
        {
            // 保険：技が未設定でも素振りできるデフォルト技を作る（旧フォールバック値と同じ）。
            Debug.LogError($"[Attack] AttackData.moves が空です: {name}");
            var fallback = ScriptableObject.CreateInstance<AttackMove>();
            fallback.powerMultiplier = 1f;
            fallback.reach = 1f;
            fallback.windupTime = 0.2f;
            fallback.activeTime = 0.1f;
            fallback.recoveryTime = 0.5f;
            moves = new List<AttackMove> { fallback };
        }
    }

    // 立ち回り用に対象を渡す（向き直りに使う）。命中判定自体はHitboxの物理で行う。
    public void SetTarget(IBattleInfo target)
    {
        this.target = target;
    }

    // 攻撃を開始する（従来互換＝技0）。
    public void StartAttack() => StartAttack(0);

    // 技を指定して攻撃を開始する。今の向きに振る（自動で吸い付かない）。攻撃中は受け付けない。
    public void StartAttack(int moveIndex)
    {
        if (IsAttacking) return;
        if (dodge != null && dodge.IsDodging) return; // 回避中は攻撃不可
        if (moves == null || moveIndex < 0 || moveIndex >= moves.Count || moves[moveIndex] == null) return;

        AttackMove move = moves[moveIndex];
        // スタミナを消費できなければ振らない（staminaCost=0なら消費処理ごとスキップ＝回復遅延も入れない）
        if (move.staminaCost > 0f && stamina != null && !stamina.Consume(move.staminaCost)) return;

        currentMove = move;
        phase = Phase.Windup;
        phaseTimer = 0f;
    }

    // 強制中断（ひるみ・回避の後隙キャンセルで使う。塊3-Bで本格利用）。
    public void ForceCancel()
    {
        if (hitbox != null) hitbox.Deactivate();
        phase = Phase.None;
        phaseTimer = 0f;
    }

    private void Update()
    {
        if (phase == Phase.None || currentMove == null) return;

        phaseTimer += Time.deltaTime;

        switch (phase)
        {
            case Phase.Windup:
                if (phaseTimer >= currentMove.windupTime)
                {
                    phase = Phase.Active;
                    phaseTimer = 0f;
                    // 判定フェーズ開始：今回の技の数値を構え、多段リストclear、Collider有効化。hitEffectもHitboxへ渡す。
                    if (hitbox != null) hitbox.Activate(baseAttackPower * currentMove.powerMultiplier, currentMove.staggerDuration, currentMove.hitEffect);
                    // 攻撃側エフェクト（振った瞬間）。Hitboxの位置・向きに出す（無ければ本体）。当たり外れに関係なく出す。
                    if (currentMove.swingEffect != null)
                    {
                        Transform at = hitbox != null ? hitbox.transform : transform;
                        Instantiate(currentMove.swingEffect, at.position, at.rotation);
                    }
                }
                break;

            case Phase.Active:
                if (phaseTimer >= currentMove.activeTime)
                {
                    phase = Phase.Recovery;
                    phaseTimer = 0f;
                    if (hitbox != null) hitbox.Deactivate(); // 判定フェーズ終了：Collider無効化
                }
                break;

            case Phase.Recovery:
                if (phaseTimer >= currentMove.recoveryTime)
                {
                    phase = Phase.None;
                    phaseTimer = 0f;
                }
                break;
        }
    }
}
