// 保存先: Assets/Scripts/Base/MinionManager.cs
using System.Collections.Generic;
using UnityEngine;

// 責務: 自国の生きている兵士リストの保持・追加・死亡除外のみ。
//   生成は Production の MinionFactory が担う。経路解決は BaseAI が担う。
//   （minionFactory / paths / Initialize は未使用だったため削除）
public class MinionManager : MonoBehaviour
{
    private List<MinionCore> minions = new List<MinionCore>();

    public void AddMinion(MinionCore minion)
    {
        minions.Add(minion);
        minion.OnDestroyed += () => minions.Remove(minion);
    }
}