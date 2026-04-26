using UnityEngine;
using TMPro;
using IdleGame.Core;
using IdleGame.Utils;

namespace IdleGame.UI
{
    public class GoldDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _goldText;

        private void Start()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnGoldChanged += UpdateGoldDisplay;
                UpdateGoldDisplay(CurrencyManager.Instance.Gold);
            }
        }

        private void UpdateGoldDisplay(double gold)
        {
            if (_goldText != null)
                _goldText.text = $"골드: {NumberFormatter.Format(gold)}";
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
    }
}
