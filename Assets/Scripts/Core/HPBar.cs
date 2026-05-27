using UnityEngine;
using UnityEngine.UI;
using Workspace.Patterns;

namespace Workspace.Core
{
    public class HPBar : MonoBehaviour
    {
        [SerializeField] private FloatEventChannel onHPChanged;
        [SerializeField] private Image fillImage;

        private float maxHP;

        public void SetMaxHP(float max)
        {
            maxHP = max;
        }

        void OnEnable()
        {
            if (onHPChanged != null)
                onHPChanged.AddListener(UpdateBar);
        }

        void OnDisable()
        {
            if (onHPChanged != null)
                onHPChanged.RemoveListener(UpdateBar);
        }

        private void UpdateBar(float currentHP)
        {
            if (fillImage == null) return;
            fillImage.fillAmount = Mathf.Clamp01(currentHP / maxHP);
        }

        public void SetColor(Color color)
        {
            if (fillImage == null) return;
            fillImage.color = color;
        }
    }
}