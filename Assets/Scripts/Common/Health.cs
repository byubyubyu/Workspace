// 保存先: Assets/Scripts/Common/Health.cs
// HP用途のラッパー。数値の器(Resource)に、HP固有の振る舞い「ダメージで減る・0で破壊検知」を乗せる。
//   ・CostPool / Construction と対称（用途ラッパーが内部にResourceを持つ構造）。
//   ・MonoBehaviourでない純粋C#クラス。Core(MinionCore/BuildingCore)が内部にnewして持つ。
//   ・責務は「破壊の検知・通知(IsEmpty)」まで。破壊後の始末(消滅・全消滅・中立化)はCore/Behaviorが担う。
//   ・ダメージ計算式は今は素通し(受け取った量をそのまま引く)。将来DamageCalculatorを差し込む受け皿。
public class Health
{
    private readonly Resource resource;

    // max: 最大HP。生成時は満タンから始める。
    public Health(float max)
    {
        resource = new Resource(max, max);
    }

    public float Current => resource.Current;
    public float Max => resource.Max;

    // HPが0になったか。兵士はStateMachineが毎フレーム参照、建物はTakeDamage直後に参照する。
    public bool IsEmpty => resource.IsEmpty;

    // ダメージを受けてHPを減らす。計算式は今は素通し（将来：DamageCalculator経由に差し替え）。
    //   Consume(アトミック=払えなければ拒否)ではなくAdd(-amount)を使う。
    //   HPは「受けた分だけ減らし、0で止める(クランプ)」のが正しい。
    //   例: 残30に50ダメージ → Consumeだと拒否され減らない(バグ)。Add(-50)なら0になる。
    public void TakeDamage(float amount)
    {
        resource.Add(-amount);
    }
}
