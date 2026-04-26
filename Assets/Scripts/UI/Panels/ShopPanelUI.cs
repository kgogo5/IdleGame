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
        private Transform _listContent;
        private bool _built = false;
        private bool _isBuyTab = true;

        private Image _buyTabImg;
        private Image _sellTabImg;
        private TextMeshProUGUI _buyTabTxt;
        private TextMeshProUGUI _sellTabTxt;

        private void Start()
        {
            BuildLayout();
            _built = true;
            ShowTab(true);
            InventoryManager.Instance.OnInventoryChanged += Refresh;
        }

        private void OnEnable()
        {
            if (_built) Refresh();
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
        }

        private void BuildLayout()
        {
            // 제목
            UIHelper.MakeText(transform, "상점", 36, TextAnchor.UpperCenter,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(0, -68), offsetMax: Vector2.zero);

            // 서브탭 바
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(transform, false);
            RectTransform tabRt = tabBar.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0, 1);
            tabRt.anchorMax = new Vector2(1, 1);
            tabRt.offsetMin = new Vector2(10, -130);
            tabRt.offsetMax = new Vector2(-10, -72);

            HorizontalLayoutGroup hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            GameObject buyTabObj = CreateTabButton(tabBar.transform, "구매");
            GameObject sellTabObj = CreateTabButton(tabBar.transform, "판매");

            _buyTabImg = buyTabObj.GetComponent<Image>();
            _sellTabImg = sellTabObj.GetComponent<Image>();
            _buyTabTxt = buyTabObj.GetComponentInChildren<TextMeshProUGUI>();
            _sellTabTxt = sellTabObj.GetComponentInChildren<TextMeshProUGUI>();

            buyTabObj.GetComponent<Button>().onClick.AddListener(() => ShowTab(true));
            sellTabObj.GetComponent<Button>().onClick.AddListener(() => ShowTab(false));

            // 스크롤뷰
            GameObject scrollObj = UIHelper.MakeScrollView(transform, out _listContent);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(10, 10);
            scrollRt.offsetMax = new Vector2(-10, -134);
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
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28;
            tmp.color = new Color(0.75f, 0.75f, 0.75f);
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
            _buyTabImg.color  = _isBuyTab  ? new Color(0.2f, 0.55f, 0.9f) : new Color(0.18f, 0.18f, 0.22f);
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

            if (_isBuyTab)
                RefreshBuy(items);
            else
                RefreshSell(items);
        }

        private void RefreshBuy(ItemData[] items)
        {
            foreach (var item in items)
            {
                if (item == null) continue;
                CreateBuyRow(item);
            }
        }

        private void RefreshSell(ItemData[] items)
        {
            bool hasAnything = false;
            foreach (var item in items)
            {
                if (item == null || !InventoryManager.Instance.IsOwned(item) || item.sellPrice <= 0) continue;
                hasAnything = true;
                CreateSellRow(item);
            }
            if (!hasAnything)
                UIHelper.MakeText(_listContent, "판매할 아이템 없음", 24, TextAnchor.MiddleCenter,
                    anchorMin: new Vector2(0, 0.4f), anchorMax: new Vector2(1, 0.6f),
                    color: new Color(0.5f, 0.5f, 0.5f));
        }

        private void CreateBuyRow(ItemData item)
        {
            GameObject row = MakeRow(item.name);

            int owned = InventoryManager.Instance.GetCount(item);
            string ownedStr = owned > 0 ? $"  [보유: {owned}]" : "";
            UIHelper.MakeText(row.transform, $"{item.itemName}{ownedStr}", 28, TextAnchor.MiddleLeft,
                new Vector2(12, 14), new Vector2(-145, 14));
            UIHelper.MakeText(row.transform, item.description, 22, TextAnchor.MiddleLeft,
                new Vector2(12, -22), new Vector2(-145, -22), new Color(0.75f, 0.75f, 0.75f));

            bool canBuy = InventoryManager.Instance.CanBuy(item);
            GameObject buyBtn = UIHelper.MakeButton(row.transform,
                $"구매\n{NumberFormatter.Format(item.buyPrice)}G", 21,
                canBuy ? new Color(0.2f, 0.55f, 0.2f) : new Color(0.3f, 0.3f, 0.3f));
            SetBtnPos(buyBtn, -6, 128, 96);

            var b = buyBtn.GetComponent<Button>();
            if (canBuy) b.onClick.AddListener(() => InventoryManager.Instance.Buy(item));
            else b.interactable = false;
        }

        private void CreateSellRow(ItemData item)
        {
            GameObject row = MakeRow(item.name);

            int owned = InventoryManager.Instance.GetCount(item);
            UIHelper.MakeText(row.transform, $"{item.itemName}  [보유: {owned}]", 28, TextAnchor.MiddleLeft,
                new Vector2(12, 14), new Vector2(-145, 14));
            UIHelper.MakeText(row.transform, item.description, 22, TextAnchor.MiddleLeft,
                new Vector2(12, -22), new Vector2(-145, -22), new Color(0.75f, 0.75f, 0.75f));

            GameObject sellBtn = UIHelper.MakeButton(row.transform,
                $"판매\n{NumberFormatter.Format(item.sellPrice)}G", 21,
                new Color(0.6f, 0.3f, 0.1f));
            SetBtnPos(sellBtn, -6, 128, 96);
            sellBtn.GetComponent<Button>().onClick.AddListener(() => InventoryManager.Instance.Sell(item));
        }

        private GameObject MakeRow(string itemName)
        {
            GameObject row = new GameObject(itemName + "_Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 135);
            row.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 1f);
            return row;
        }

        private static void SetBtnPos(GameObject btn, float anchoredX, float width, float height)
        {
            RectTransform rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(anchoredX, 0);
            rt.sizeDelta = new Vector2(width, height);
        }
    }
}
