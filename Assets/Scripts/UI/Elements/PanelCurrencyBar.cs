using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;
using IdleGame.Utils;

namespace IdleGame.UI
{
    public class PanelCurrencyBar : MonoBehaviour
    {
        private TextMeshProUGUI _goldText;
        private TextMeshProUGUI _jewelText;

        private void Awake() => BuildLayout();

        private void OnEnable()
        {
            if (CurrencyManager.Instance == null) return;
            CurrencyManager.Instance.OnGoldChanged  += UpdateGold;
            CurrencyManager.Instance.OnJewelChanged += UpdateJewel;
            UpdateGold(CurrencyManager.Instance.Gold);
            UpdateJewel(CurrencyManager.Instance.Jewel);
        }

        private void OnDisable()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnGoldChanged  -= UpdateGold;
                CurrencyManager.Instance.OnJewelChanged -= UpdateJewel;
            }
        }

        private void BuildLayout()
        {
            Image bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.09f, 1f);
            bg.raycastTarget = false;

            // 구분선
            GameObject sep = new GameObject("Sep");
            sep.transform.SetParent(transform, false);
            RectTransform srt = sep.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.15f);
            srt.anchorMax = new Vector2(0.5f, 0.85f);
            srt.sizeDelta = new Vector2(1, 0);
            sep.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.35f, 1f);

            // 골드 (왼쪽)
            _goldText = MakeLabel("GoldText", new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                new Vector2(16, 0), new Vector2(-8, 0),
                new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Midline);

            // 보석 (오른쪽)
            _jewelText = MakeLabel("JewelText", new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                new Vector2(8, 0), new Vector2(-16, 0),
                new Color(0.5f, 0.85f, 1f), TextAlignmentOptions.Midline);
        }

        private TextMeshProUGUI MakeLabel(string id,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax,
            Color color, TextAlignmentOptions align)
        {
            GameObject go = new GameObject(id);
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 28;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void UpdateGold(double v)  => _goldText.text  = $"골드  {NumberFormatter.Format(v)}";
        private void UpdateJewel(double v) => _jewelText.text = $"보석  {NumberFormatter.Format(v)}";
    }
}
