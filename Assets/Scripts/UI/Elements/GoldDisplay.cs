using UnityEngine;
using TMPro;
using IdleGame.Core;

namespace IdleGame.UI
{
    public class GoldDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _goldText;

        private void Start()
        {
            CurrencyManager.Instance.OnGoldChanged += UpdateGoldDisplay;
            UpdateGoldDisplay(0);
        }

        private void UpdateGoldDisplay(double gold)
        {
            if (_goldText != null)
            {
                _goldText.text = $"Gold: {gold:F0}";
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
            }
        }
    }
}
