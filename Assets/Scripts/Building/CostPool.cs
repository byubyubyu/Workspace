// 保存先: Assets/Scripts/Building/CostPool.cs
// コストの器＋時間回復＋複数Barrack共有を担う。数値の器部分はResourceに委ねる。
//   ・現在値・最大値・消費・残高確認 → Resource
//   ・時間回復・複数Barrackからの共有参照 → CostPool固有（ここに残す）
using UnityEngine;

public class CostPool : MonoBehaviour
{
    private Resource resource;
    private float recoverySpeed;
    private bool initialized = false;

    // startCurrent: 開始時の現在コスト（Baseごとに変えられる）。Resourceがmaxにクランプする。
    public void Initialize(float max, float speed, float startCurrent)
    {
        resource = new Resource(max, startCurrent);
        recoverySpeed = speed;
        initialized = true;
    }

    public bool CanAfford(float amount) => initialized && resource.CanAfford(amount);

    // 払えるなら消費してtrue、払えなければ消費せずfalse（Resourceのアトミック消費を素通し）。
    public bool Consume(float amount) => initialized && resource.Consume(amount);

    private void Update()
    {
        if (!initialized) return;
        // 時間回復はCostPool固有。器(Resource)にAddするだけ（上限クランプはResource側で行う）。
        resource.Add(recoverySpeed * Time.deltaTime);
    }
}
