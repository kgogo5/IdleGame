using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI.Panels
{
    public class ShopPanelUI : MonoBehaviour
    {
        private const int   ROW_H  = 160;
        private const int   BTN_W  = 150;
        private const int   BTN_H  = 110;
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
            var tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(transform, false);
            var tabRt = tabBar.AddComponent<RectTransform>();
            tabRt.anchorMin = new Vector2(0, 1); tabRt.anchorMax = new Vector2(1, 1);
            tabRt.offsetMin = new Vector2(10, -155); tabRt.offsetMax = new Vector2(-10, -88);
            var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;

            var buyTabObj  = CreateTabButton(tabBar.transform, "구매");
            var sellTabObj = CreateTabButton(tabBar.transform, "판매");
            _buyTabImg  = buyTabObj.GetComponent<Image>();
            _sellTabImg = sellTabObj.GetComponent<Image>();
            _buyTabTxt  = buyTabObj.GetComponentInChildren<TextMeshProUGUI>();
            _sellTabTxt = sellTabObj.GetComponentInChildren<TextMeshProUGUI>();
            buyTabObj.GetComponent<Button>().onClick.AddListener(() => ShowTab(true));
            sellTabObj.GetComponent<Button>().onClick.AddListener(() => ShowTab(false));

            // 스크롤뷰
            var scrollObj = UIHelper.MakeScrollView(transform, out _listContent);
            var scrollRt  = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(10, 10); scrollRt.offsetMax = new Vector2(-10, -160);
        }

        private GameObject CreateTabButton(Transform parent, string label)
        {
            var obj = new GameObject(label + "Tab");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var img = obj.AddComponent<Image>();
            img.color = UITheme.TabInactive;
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(obj.transform, false);
            var trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 32;
            tmp.color     = UITheme.TxtTabInactive;
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
            _buyTabImg.color  = _isBuyTab  ? UITheme.TabActiveBuy  : UITheme.TabInactive;
            _sellTabImg.color = !_isBuyTab ? UITheme.TabActiveSell : UITheme.TabInactive;
            _buyTabTxt.color  = _isBuyTab  ? Color.white : UITheme.TxtTabInactive;
            _sellTabTxt.color = !_isBuyTab ? Color.white : UITheme.TxtTabInactive;
        }

        private Coroutine _holdSellRoutine;
        private bool      _holdActivated;

        private void OnDisable() => StopHoldSell();

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
            bool hasNormal = false;
            foreach (var item in items)
            {
                if (item == null || item.rarity != ItemRarity.Normal || !InventoryManager.Instance.IsOwned(item)) continue;
                if (!InventoryManager.Instance.IsEquipped(item)) { hasNormal = true; break; }
            }
            CreateSellAllNormalRow(hasNormal);

            bool hasAny = false;
            foreach (var item in items)
            {
                if (item == null || !InventoryManager.Instance.IsOwned(item) || item.sellPrice <= 0) continue;
                hasAny = true;
                CreateSellRow(item);
            }
            if (!hasAny) EmptyLabel("판매할 아이템 없음");
        }

        private void CreateSellAllNormalRow(bool hasNormal)
        {
            var row = new GameObject("SellAllNormal_Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 80);
            row.AddComponent<Image>().color = UITheme.BgRowSellAll;

            var btn = UIHelper.MakeButton(row.transform, "노말 아이템 전부 판매", 30,
                hasNormal ? UITheme.BtnSellAll : UITheme.BtnDisabled);
            var brt = btn.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(14, 10); brt.offsetMax = new Vector2(-14, -10);

            var b = btn.GetComponent<Button>();
            if (hasNormal) b.onClick.AddListener(() => InventoryManager.Instance.SellAllByRarity(ItemRarity.Normal));
            else           b.interactable = false;
        }

        private void CreateBuyRow(ItemData item)
        {
            var row   = MakeRow(item.name);
            int owned = InventoryManager.Instance.GetCount(item);
            string ownedStr  = owned > 0 ? $"  [보유: {owned}]" : "";
            string rarityHex = ColorUtility.ToHtmlStringRGB(item.rarity.ToColor());
            string nameText  = item.isStackable
                ? $"{item.itemName}{ownedStr}"
                : $"<color=#888888>[{item.slot.ToKorean()}]</color> <color=#{rarityHex}>[{item.rarity.ToKorean()}]</color> {item.itemName}{ownedStr}";

            RowLabel(row.transform, nameText, NAME_F, Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-(BTN_W + 18), -4),
                richText: true);
            RowLabel(row.transform, item.description, DESC_F, UITheme.TxtDesc,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-(BTN_W + 18), -80));

            bool canBuy = InventoryManager.Instance.CanBuy(item);
            var btn = UIHelper.MakeButton(row.transform,
                $"구매\n{NumberFormatter.Format(item.buyPrice)}G", (int)BTN_F,
                canBuy ? UITheme.BtnShopBuyable : UITheme.BtnShopCantAfford);
            SetBtnPos(btn, BTN_W, BTN_H);
            var b = btn.GetComponent<Button>();
            if (canBuy) b.onClick.AddListener(() => InventoryManager.Instance.Buy(item));
            else        b.interactable = false;
        }

        private void CreateSellRow(ItemData item)
        {
            bool isEquipped = !item.isStackable && InventoryManager.Instance.IsEquipped(item);
            var row = MakeRow(item.name);
            row.GetComponent<Image>().color = isEquipped ? UITheme.BgRowEquipped : UITheme.BgRowDefault;

            int owned = InventoryManager.Instance.GetCount(item);
            string equippedTag = isEquipped ? $"  <color={UITheme.HexEquipped}>[장착 중]</color>" : "";
            string rarityHex   = ColorUtility.ToHtmlStringRGB(item.rarity.ToColor());
            string nameText    = item.isStackable
                ? $"{item.itemName}  [보유: {owned}]"
                : $"<color=#{rarityHex}>[{item.rarity.ToKorean()}]</color> {item.itemName}{equippedTag}  [보유: {owned}]";

            RowLabel(row.transform, nameText, NAME_F, Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-(BTN_W + 18), -4),
                richText: true);
            RowLabel(row.transform, item.description, DESC_F, UITheme.TxtDesc,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-(BTN_W + 18), -80));

            if (isEquipped)
            {
                var disBtn = UIHelper.MakeButton(row.transform, "장착 중", (int)BTN_F, UITheme.BtnDisabled);
                SetBtnPos(disBtn, BTN_W, BTN_H);
                disBtn.GetComponent<Button>().interactable = false;
                return;
            }

            var sellBtn = UIHelper.MakeButton(row.transform,
                $"판매\n{NumberFormatter.Format(item.sellPrice)}G", (int)BTN_F, UITheme.BtnSell);
            SetBtnPos(sellBtn, BTN_W, BTN_H);

            var et = sellBtn.AddComponent<EventTrigger>();
            AddPtrEvent(et, EventTriggerType.PointerDown,
                _ => { _holdActivated = false; _holdSellRoutine = StartCoroutine(HoldSell(item)); });
            AddPtrEvent(et, EventTriggerType.PointerUp,
                _ => { if (!_holdActivated && InventoryManager.Instance.IsOwned(item)) InventoryManager.Instance.Sell(item); StopHoldSell(); });
            AddPtrEvent(et, EventTriggerType.PointerExit, _ => StopHoldSell());
        }

        private static void AddPtrEvent(EventTrigger et, EventTriggerType type,
            UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(action);
            et.triggers.Add(entry);
        }

        private void StopHoldSell()
        {
            if (_holdSellRoutine != null) { StopCoroutine(_holdSellRoutine); _holdSellRoutine = null; }
            _holdActivated = false;
        }

        private IEnumerator HoldSell(ItemData item)
        {
            yield return new WaitForSeconds(0.5f);
            _holdActivated = true;
            float interval = 0.5f;
            while (InventoryManager.Instance.IsOwned(item))
            {
                InventoryManager.Instance.Sell(item);
                yield return new WaitForSeconds(interval);
                interval = Mathf.Max(interval * 0.8f, 0.05f);
            }
        }

        private GameObject MakeRow(string id)
        {
            var row = new GameObject(id + "_Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, ROW_H);
            row.AddComponent<Image>().color = UITheme.BgRowDefault;
            return row;
        }

        private static void RowLabel(Transform parent, string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool richText = false)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = fontSize;
            tmp.color            = color;
            tmp.richText         = richText;
            tmp.alignment        = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode     = TextOverflowModes.Ellipsis;
            tmp.raycastTarget    = false;
        }

        private static void SetBtnPos(GameObject btn, float width, float height)
        {
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-10, 0);
            rt.sizeDelta = new Vector2(width, height);
        }

        private void EmptyLabel(string text)
        {
            UIHelper.MakeText(_listContent, text, 30, TextAnchor.MiddleCenter,
                anchorMin: new Vector2(0, 0.4f), anchorMax: new Vector2(1, 0.6f),
                color: UITheme.TxtEmpty);
        }
    }
}
