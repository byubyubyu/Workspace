// 保存先: Assets/Scripts/Minion/Dodge.cs
// 回避の実体。スタミナを消費し、一定時間ダッシュ移動しつつ先頭で無敵(i-frame)になる自走コンポーネント。
//   ・StartDodge後は自分のUpdateで振り切る（Attackと同じ実体パターン）。
//   ・i-frameは子Hurtboxのcolliderを無効化して実現（Hitbox×Hurtboxで当たらなくなる）。
//   ・プレイヤー・兵士で共通の実体。発火主体（入力/AI）はC-4で配線する。
//   ・回避中は攻撃不可・再回避不可。攻撃中(IsAttacking)は回避開始不可。
using UnityEngine;

public class Dodge : MonoBehaviour
{
    private DodgeData data;
    private Stamina stamina;
    private IDasher dasher;
    private Hurtbox hurtbox;
    private Attack attack;

    private bool dodging;
    private float timer;    // 回避開始からの経過秒
    private bool iFrameOn;  // 今、無敵中か（Hurtbox無効化中か）

    public bool IsDodging => dodging;

    // 依存を取得して初期化する。MinionCoreから呼ばれる。
    public void Initialize(DodgeData data)
    {
        this.data = data;
        stamina  = GetComponent<Stamina>();
        dasher   = GetComponent<IDasher>();
        attack   = GetComponent<Attack>();
        hurtbox  = GetComponentInChildren<Hurtbox>(true);
    }

    // 指定方向へ回避を開始する。スタミナ不足・攻撃中・回避中は何もしない。
    public void StartDodge(Vector3 direction)
    {
        if (data == null) return;
        if (dodging) return;                               // 再回避不可
        if (attack != null && attack.IsAttacking) return;  // 攻撃中は回避不可
        if (stamina == null || !stamina.Consume(data.staminaCost)) return; // スタミナ不足なら不可

        dodging = true;
        timer = 0f;

        // 無敵開始（Hurtbox無効化）。iFrameDurationが0以下なら無敵なし。
        if (data.iFrameDuration > 0f && hurtbox != null)
        {
            hurtbox.SetVulnerable(false);
            iFrameOn = true;
        }

        // ダッシュ移動開始（水平方向に正規化）。
        Vector3 dir = direction; dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();
        if (dasher != null) dasher.Dash(dir, data.dashSpeed);
    }

    private void Update()
    {
        if (!dodging) return;
        timer += Time.deltaTime;

        // 無敵時間が終わったらHurtboxを戻す。
        if (iFrameOn && timer >= data.iFrameDuration)
        {
            if (hurtbox != null) hurtbox.SetVulnerable(true);
            iFrameOn = false;
        }

        // 回避全体が終わったらダッシュ終了・通常へ。
        if (timer >= data.dodgeDuration)
            EndDodge();
    }

    private void EndDodge()
    {
        dodging = false;
        // 念のため無敵を戻す（iFrameDuration >= dodgeDuration の設定でも漏らさない）。
        if (iFrameOn && hurtbox != null) { hurtbox.SetVulnerable(true); iFrameOn = false; }
        if (dasher != null) dasher.EndDash();
    }
}
