using System.Collections.Generic;
using UnityEngine;

public class MinionManager : MonoBehaviour
{
    private MinionFactory minionFactory;
    private List<MinionCore> minions = new List<MinionCore>();
    private List<Path> paths;

    public void Initialize(MinionFactory factory, List<Path> paths)
    {
        minionFactory = factory;
        this.paths = paths;
    }

    public void AddMinion(MinionCore minion)
    {
        minions.Add(minion);
        minion.OnDestroyed += () => minions.Remove(minion);
    }
}
