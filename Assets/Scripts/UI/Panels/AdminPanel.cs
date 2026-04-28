using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI.Panels
{
    public class AdminPanel : MonoBehaviour
    {
        private GameObject _overlay;
        private bool _built;

        private void Start()
        {
            Build();
            _overlay.SetActive(false);
        }

        public void Show()
        {
            if (!_built) Build();
            _overlay.SetActive(true);
        }

        public void Hide() => _overlay.SetActive(false);

        private void Build()
        {
            _built = true;

            // 전체화면 반투명 오버레이
            _overlay = new GameObject("AdminOverlay");
            _overlay.transform.SetParent(transform, false);
            RectTransform overlayRt = _overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
            Image overlayImg = _overlay.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.85f);
            overlayImg.raycastTarget = true;

            // 중앙 패널
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(_overlay.transform, false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.05f, 0.15f);
            panelRt.anchorMax = new Vector2(0.95f, 0.85f);
            panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.1f, 0.15f, 1f);

            // 수직 레이아웃
            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 20, 20);
            vlg.spacing = 14f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;

            // 제목
            AddLabel(panel.transform, "⚙ 개발자 메뉴", 34, Color.white, FontStyles.Bold, 60f);
            AddDivider(panel.transform);

            // 골드 추가
            AddLabel(panel.transform, "골드 추가", 24, new Color(0.8f, 0.8f, 0.4f), FontStyles.Normal, 40f);
            AddGoldButtons(panel.transform);
            AddDivider(panel.transform);

            // 치트 기능
            AddLabel(panel.transform, "치트", 24, new Color(0.8f, 0.8f, 0.4f), FontStyles.Normal, 40f);
            AddCheatButton(panel.transform, "업그레이드 모두 최대",
                new Color(0.2f, 0.5f, 0.2f), () => UpgradeManager.Instance.MaxAllUpgrades());
            AddCheatButton(panel.transform, "아이템 모두 지급",
                new Color(0.2f, 0.4f, 0.55f), () => InventoryManager.Instance.GiveAllItems());
            AddDivider(panel.transform);

            // 닫기
            AddCheatButton(panel.transform, "닫기", new Color(0.5f, 0.15f, 0.15f), Hide, height: 70f);
        }

        private void AddGoldButtons(Transform parent)
        {
            GameObject row = new GameObject("GoldRow");
            row.transform.SetParent(parent, false);
            LayoutElement rle = row.AddComponent<LayoutElement>();
            rle.preferredHeight = 80f;
            rle.flexibleHeight = 0f;
            HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;

            (string label, double amount)[] entries =
            {
                ("+1만",  10_000),
                ("+100만",  1_000_000),
                ("+1억",  100_000_000),
                ("+100억", 10_000_000_000),
            };

            Color goldColor = new Color(0.25f, 0.55f, 0.25f);
            foreach (var (label, amount) in entries)
            {
                double captured = amount;
                AddRowButton(row.transform, label, goldColor,
                    () => CurrencyManager.Instance.AddGoldRaw(captured));
            }
        }

        private void AddCheatButton(Transform parent, string label, Color color,
            System.Action onClick, float height = 72f)
        {
            GameObject go = new GameObject(label + "_Btn");
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            Image bg = go.AddComponent<Image>();
            bg.color = color;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());

            AddTmpText(go.transform, label, 26f, Color.white, FontStyles.Bold);
        }

        private void AddRowButton(Transform parent, string label, Color color, System.Action onClick)
        {
            GameObject go = new GameObject(label + "_Btn");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            Image bg = go.AddComponent<Image>();
            bg.color = color;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());
            AddTmpText(go.transform, label, 22f, Color.white, FontStyles.Bold);
        }

        private void AddLabel(Transform parent, string text, float size, Color color,
            FontStyles style, float height)
        {
            GameObject go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleHeight = 0f;
            AddTmpText(go.transform, text, size, color, style);
        }

        private void AddDivider(Transform parent)
        {
            GameObject go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1f;
            le.flexibleHeight = 0f;
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.1f);
            img.raycastTarget = false;
        }

        private static void AddTmpText(Transform parent, string text, float size,
            Color color, FontStyles style)
        {
            GameObject go = new GameObject("TMP");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = size;
            tmp.color     = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
    }
}
