using System;
using System.Collections.Generic;
using UnityEngine;

// 装備の見た目を体に着せる（段階3の見た目担当）。
//   EquipmentHolder.OnEquipmentChanged を購読し、スロットごとのマウント位置
//   （ボーン追従Transform＝HandBoneFollowerの先）へ ItemData.Prefab を生成する。
//   武器（RightHand/LeftHand）は PlayerHandState が担当するため、ここでは Head/Armor/Feet を扱う。
public class EquipmentVisualizer : MonoBehaviour
{
    [Serializable]
    public struct Mount
    {
        public EquipmentSlot slot;
        public Transform[] points; // 装着位置（Feetは左右2点、それ以外は1点）
    }

    [SerializeField] private EquipmentHolder equipmentHolder;
    [SerializeField] private List<Mount> mounts = new List<Mount>();

    private readonly List<GameObject> views = new List<GameObject>();

    private void OnEnable()
    {
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (equipmentHolder != null) equipmentHolder.OnEquipmentChanged -= Refresh;
    }

    private void Refresh()
    {
        foreach (var v in views)
        {
            if (v != null) Destroy(v);
        }
        views.Clear();

        if (equipmentHolder == null) return;

        foreach (var mount in mounts)
        {
            ItemData item = equipmentHolder.Get(mount.slot);
            if (item == null || item.Prefab == null || mount.points == null) continue;

            foreach (var point in mount.points)
            {
                if (point == null) continue;
                var view = Instantiate(item.Prefab, point);
                view.transform.localPosition = Vector3.zero;
                view.transform.localRotation = Quaternion.identity;
                views.Add(view);
            }
        }
    }
}
