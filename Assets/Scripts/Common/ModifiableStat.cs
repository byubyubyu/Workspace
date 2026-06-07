// 保存先: Assets/Scripts/Common/ModifiableStat.cs
// 「基礎値＋補正＝実効値」を計算する数値プリミティブ（Resourceと並ぶ共通の器）。
//   ステータス（防御力・移動速度など）を base＋bonus で表し、Value で実効値を返す。
//   補正源（装備・将来のバフ等）は bonus に集約する。MonoBehaviourではない純粋C#。
//   ※ クランプ（Max(0)等）や乗算補正は必要になったら足す（今は単純加算）。
public class ModifiableStat
{
    private float baseValue; // 基礎値（素のステータス）
    private float bonus;     // 加算補正の合計（装備・バフ等）

    public ModifiableStat(float baseValue = 0f) { this.baseValue = baseValue; }

    public float Base => baseValue;
    public float Bonus => bonus;
    public float Value => baseValue + bonus; // 実効値

    public void SetBase(float v) { baseValue = v; }
    public void SetBonus(float b) { bonus = b; }
}
