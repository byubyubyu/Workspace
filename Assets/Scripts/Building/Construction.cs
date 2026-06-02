// 保存先: Assets/Scripts/Building/Construction.cs
// 建設進捗の器＋「満タンで完成イベント」を担う。数値の器部分はResourceに委ねる。
//   ・現在値・最大値・加算・満タン判定 → Resource
//   ・満タンで完成イベント・建設方式(Manual/Auto) → Construction固有（ここに残す）
using System;
using UnityEngine;

public class Construction : MonoBehaviour
{
    private Resource resource;
    private BuildStrategy buildStrategy;
    private bool initialized = false;
    public bool IsCompleted { get; private set; }
    public bool IsManual => buildStrategy is ManualBuildStrategy; // 兵士が建設対象にするか（Manual建設のみ）
    public event Action OnCompleted;

    // ゲージ表示用（BuildGaugeSourceが読む）。クラス名Constructionが種別の文脈を与えるためCurrent/Maxに統一。
    public float Current => initialized ? resource.Current : 0f;
    public float Max => initialized ? resource.Max : 0f;

    public void Initialize(IBuildingData data, BuildStrategy strategy)
    {
        // 開始時は現在値0から。完成に必要な建設ポイントをmaxにする。
        resource = new Resource(data.Stat.needBuildPoint, 0f);
        buildStrategy = strategy;
        initialized = true;
    }

    // 初期配置の建物用: 生成直後に完成状態にする。
    public void CompleteImmediately()
    {
        if (!initialized) return;
        resource.Add(resource.Max); // 満タンまで一気に足す（Resourceがmaxでクランプ）
        if (!IsCompleted)
        {
            IsCompleted = true;
            OnCompleted?.Invoke();
        }
    }

    // 建設ポイントを足す。クラス名Constructionが文脈を与えるためメソッド名はAddに統一。
    public void Add(float amount)
    {
        if (IsCompleted) return;
        resource.Add(amount);
        if (resource.IsFull)
        {
            IsCompleted = true;
            OnCompleted?.Invoke();
        }
    }

    private void Update()
    {
        if (!initialized) return;
        buildStrategy.UpdateBuildPoint(this, Time.deltaTime);
    }
}
