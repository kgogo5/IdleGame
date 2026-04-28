using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI.Panels
{
    public class ShopPanelUI : MonoBehaviour
    {
        private const int ROW_H    = 160;
        private const int BTN_W    = 150;
        private const int BTN_H    = 110;
        private const float NAME_F = 32f;
        private const float DESC_F = 26f;
        private const float BTN_F  = 28f;

        private Transform _listContent;
        private bool _built = false;
        private bool _isBuyTab = true;

        private Image _buyTabImg, _sellTabImg;
        private TextMeshProUGUI _buyTabTxt, _sellTabTxt;

        private void Start()
        {
            BuildLayout();
            _built = true;
            ShowTab(true);
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        }

        private void OnEnable()  { if (_built) Refresh(); }
        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }

        private void BuildLayout()
        {
            UIHelper.MakeText(transform, "상점", 42, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, -100), offsetMax: new Vector2(0, -20));

            // 탭 바
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(transform, false);
            RectTransform tabRt = tabBar.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0, 1);
            tabRt.anchorMax = new Vector2(1, 1);
            tabRt.offsetMin = new Vector2(10, -155);
            tabRt.offsetMax = new Vector2(-10, -88);

            HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            GameObject buyTabObj  = CreateTabButton(tabBar.transform, "구매");
            GameObject sellTabObj = CreateTabButton(tabBar.transform, "판매");
            _buyTabImg  = buyTabObj.GetComponent<Image>();
            _sellTabImg = sellTabObj.GetComponent<Image>();
            _buyTabTxt  = buyTabObj.GetComponentInChildren<TextMeshProUGUI>();
            _sellTabTxt = sellTabObj.GetComponentInChildren<TextMeshProUGUI>();
            buyTabObj.GetComponent<Button>().onClick.AddListener(() => ShowTab(true));
            sellTabObj.GetComponent<Button>().onClick.AddListener(() => ShowTab(false));

            // 스크롤뷰
            GameObject scrollObj = UIHelper.MakeScrollView(transform, out _listContent);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(10, 10);
            scrollRt.offsetMax = new Vector2(-10, -160);
        }

        private GameObject CreateTabButton(Transform parent, string label)
        {
            GameObject obj = new GameObject(label + "Tab");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.25f);
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 32;
            tmp.color     = new Color(0.75f, 0.75f, 0.75f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return obj;
        }

        private void ShowTab(bool buyTab)
        {
            _isBuyTab = buyTab;
            UpdateTabColors();
            Refresh();
        }

        private void UpdateTabColors()
        {
            if (_buyTabImg == null) return;
            _buyTabImg.color  = _isBuyTab  ? new Color(0.2f, 0.55f, 0.9f)  : new Color(0.18f, 0.18f, 0.22f);
            _sellTabImg.color = !_isBuyTab ? new Color(0.75f, 0.35f, 0.1f) : new Color(0.18f, 0.18f, 0.22f);
            _buyTabTxt.color  = _isBuyTab  ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            _sellTabTxt.color = !_isBuyTab ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        }

        private void Refresh()
        {
            if (_listContent == null || InventoryManager.Instance == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);
            var items = InventoryManager.Instance.ShopItems;
            if (items == null) return;
            if (_isBuyTab) RefreshBuy(items);
            else           RefreshSell(items);
        }

        private void RefreshBuy(ItemData[] items)
        {
            bool hasAny = false;
            foreach (var item in items)
            {
                if (item == null || item.buyPrice <= 0) continue;
                hasAny = true;
                CreateBuyRow(item);
            }
            if (!hasAny) EmptyLabel("구매 가능한 아이템 없음");
        }

        private void RefreshSell(ItemData[] items)
        {
            bool hasAny = false;
            foreach (var item in items)
            {
                if (item == null || !InventoryManager.Instance.IsOwned(item) || item.sellPrice <= 0) continue;
                hasAny = true;
                CreateSellRow(item);
            }
            if (!hasAny) EmptyLabel("판매할 아이템 없음");
        }

        private void CreateBuyRow(ItemData item)
        {
            GameObject row = MakeRow(item.name);
            int owned = InventoryManager.Instance.GetCount(item);
            string ownedStr = owned > 0 ? $"  [보유: {owned}]" : "";
            string nameText = item.isStackable
                ? $"{item.itemName}{ownedStr}"
                : $"<color=#888888>[{item.slot.ToKorean()}]</color> <color=#{ColorUtility.ToHtmlStringRGB(item.rarity.ToColor())}>[{item.rarity.ToKorean()}]</color> {item.itemName}{ownedStr}";

            RowLabel(row.transform, nameText, NAME_F, Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-(BTN_W + 18), -4),
                richText: true);
            RowLabel(row.transform, item.description, DESC_F, new Color(0.75f, 0.75f, 0.75f),
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-(BTN_W + 18), -80));

            bool canBuy = InventoryManager.Instance.CanBuy(item);
            GameObject btn = UIHelper.MakeButton(row.transform,
                $"구매\n{NumberFormatter.Format(item.buyPrice)}G", (int)BTN_F,
                canBuy ? new Color(0.2f, 0.55f, 0.2f) : new Color(0.28f, 0.28f, 0.28f));
            SetBtnPos(btn, BTN_W, BTN_H);
            var b = btn.GetComponent<Button>();
            if (canBuy) b.onClick.AddListener(() => InventoryManager.Instance.Buy(item));
            else        b.interactable = false;
        }

        private void CreateSellRow(ItemData item)
        {
            GameObject row = MakeRow(item.name);
            int owned = InventoryManager.Instance.GetCount(item);
            string nameText = item.isStackable
                ? $"{item.itemName}  [보유: {owned}]"
                : $"<color=#{ColorUtility.ToHtmlStringRGB(item.rarity.ToColor())}>[{item.rarity.ToKorean()}]</color> {item.itemName}  [보유: {owned}]";

            RowLabel(row.transform, nameText, NAME_F, Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-(BTN_W + 18), -4),
                richText: true);
            RowLabel(row.transform, item.description, DESC_F, new Color(0.75f, 0.75f, 0.75f),
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-(BTN_W + 18), -80));

            GameObject btn = UIHelper.MakeButton(row.transform,
                $"판매\n{NumberFormatter.Format(item.sellPrice)}G", (int)BTN_F,
                new Color(0.6f, 0.3f, 0.1f));
            SetBtnPos(btn, BTN_W, BTN_H);
            btn.GetComponent<Button>().onClick.AddListener(() => InventoryManager.Instance.Sell(item));
        }

        private GameObject MakeRow(string id)
        {
            GameObject row = new GameObject(id + "_Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, ROW_H);
            row.AddComponent<Image>().color = new Color(0.13f, 0.13f, 0.18f);
            return row;
        }

        private static void RowLabel(Transform parent, string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool richText = false)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = fontSize;
            tmp.color            = color;
            tmp.richText         = richText;
            tmp.alignment        = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            tmp.overflowMode     = TextOverflowModes.Ellipsis;
            tmp.raycastTarget    = false;
        }

        private static void SetBtnPos(GameObject btn, float width, float height)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-10, 0);
            rt.sizeDelta = new Vector2(width, height);
        }

        private void EmptyLabel(string text)
        {
            UIHelper.MakeText(_listContent, text, 30, TextAnchor.MiddleCenter,
                anchorMin: new Vector2(0, 0.4f), anchorMax: new Vector2(1, 0.6f),
                color: new Color(0.5f, 0.5f, 0.5f));
        }
    }
}
