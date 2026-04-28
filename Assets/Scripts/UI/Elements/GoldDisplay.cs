using UnityEngine;
using TMPro;
using IdleGame.Core;
using IdleGame.Utils;

namespace IdleGame.UI
{
    public class GoldDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _goldText;

        private TextMeshProUGUI _jewelText;

        private void Start()
        {
            // Find optional sibling JewelText
            Transform parent = transform.parent;
            if (parent != null)
            {
                Transform jt = parent.Find("JewelText");
                if (jt != null) _jewelText = jt.GetComponent<TextMeshProUGUI>();
            }

            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnGoldChanged  += UpdateGoldDisplay;
                CurrencyManager.Instance.OnJewelChanged += UpdateJewelDisplay;
                UpdateGoldDisplay(CurrencyManager.Instance.Gold);
                UpdateJewelDisplay(CurrencyManager.Instance.Jewel);
            }
        }

        private void UpdateGoldDisplay(double gold)
        {
            if (_goldText != null)
                _goldText.text = $"골드: {NumberFormatter.Format(gold)}";
        }

        private void UpdateJewelDisplay(double jewel)
        {
            if (_jewelText != null)
                _jewelText.text = $"보석: {NumberFormatter.Format(jewel)}";
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.OnGoldChanged  -= UpdateGoldDisplay;
            CurrencyManager.Instance.OnJewelChanged -= UpdateJewelDisplay;
        }
    }
}
