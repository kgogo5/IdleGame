using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Data;
using IdleGame.Managers;

namespace IdleGame.UI.Panels
{
    public class AchievementPanelUI : MonoBehaviour
    {
        private Transform _listContent;
        private TextMeshProUGUI _progressText;
        private bool _built;

        private static readonly ItemRarity[] RARITY_ORDER =
        {
            ItemRarity.Normal, ItemRarity.Rare, ItemRarity.Unique, ItemRarity.Legendary
        };
        private static readonly string[] RARITY_NAMES = { "일반", "레어", "유니크", "레전더리" };

        private void Start()
        {
            BuildLayout();
            _built = true;
            Refresh();
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemAcquired += OnAcquired;
                InventoryManager.Instance.OnItemAutoSold += OnAutoSold;
            }
        }

        private void OnEnable()  { if (_built) Refresh(); }
        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemAcquired -= OnAcquired;
                InventoryManager.Instance.OnItemAutoSold -= OnAutoSold;
            }
        }

        private void OnAcquired(ItemData _) => Refresh();
        private void OnAutoSold(ItemData _, double __) => Refresh();

        private void BuildLayout()
        {
            UIHelper.MakeText(transform, "도감", 42, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, -100), offsetMax: new Vector2(0, -20));

            var progressGo = new GameObject("Progress");
            progressGo.transform.SetParent(transform, false);
            var progressRt = progressGo.AddComponent<RectTransform>();
            progressRt.anchorMin = new Vector2(0, 1); progressRt.anchorMax = new Vector2(1, 1);
            progressRt.offsetMin = new Vector2(20, -135); progressRt.offsetMax = new Vector2(-20, -100);
            _progressText = progressGo.AddComponent<TextMeshProUGUI>();
            _progressText.fontSize = 24;
            _progressText.color    = UITheme.TxtSubtle;
            _progressText.alignment = TextAlignmentOptions.MidlineLeft;
            _progressText.raycastTarget = false;

            var scrollGo = UIHelper.MakeScrollView(transform, out _listContent);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8, 8); scrollRt.offsetMax = new Vector2(-8, -140);
        }

        private void Refresh()
        {
            if (_listContent == null || InventoryManager.Instance == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var inv   = InventoryManager.Instance;
            var items = inv.ShopItems;
            if (items == null) return;

            // 장비(비스택)만 도감에 표시
            var byRarity = new Dictionary<ItemRarity, List<ItemData>>();
            foreach (var r in RARITY_ORDER) byRarity[r] = new List<ItemData>();

            int totalCount = 0;
            foreach (var item in items)
            {
                if (item == null || item.isStackable) continue;
                byRarity[item.rarity].Add(item);
                totalCount++;
            }

            int obtained = inv.EverObtainedCount;
            _progressText.text = $"수집 현황: {obtained} / {totalCount}";

            for (int ri = 0; ri < RARITY_ORDER.Length; ri++)
            {
                var rarity     = RARITY_ORDER[ri];
                var rarityList = byRarity[rarity];
                if (rarityList.Count == 0) continue;

                rarityList.Sort((a, b) => ((int)a.slot).CompareTo((int)b.slot));

                AddSectionHeader(RARITY_NAMES[ri], rarity.ToColor());
                foreach (var item in rarityList)
                    CreateItemRow(item, inv.HasEverObtained(item));
            }
        }

        private void AddSectionHeader(string label, Color rarityColor)
        {
            var go = new GameObject("SectionHeader");
            go.transform.SetParent(_listContent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 52);
            var img = go.AddComponent<Image>();
            img.color = new Color(rarityColor.r * 0.25f, rarityColor.g * 0.25f, rarityColor.b * 0.25f, 0.85f);

            var left = new GameObject("Bar");
            left.transform.SetParent(go.transform, false);
            var leftRt = left.AddComponent<RectTransform>();
            leftRt.anchorMin = new Vector2(0, 0); leftRt.anchorMax = new Vector2(0, 1);
            leftRt.offsetMin = Vector2.zero;       leftRt.offsetMax = new Vector2(5, 0);
            left.AddComponent<Image>().color = rarityColor;

            var tGo = new GameObject("Label");
            tGo.transform.SetParent(go.transform, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = new Vector2(16, 0); tRt.offsetMax = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = rarityColor;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        private void CreateItemRow(ItemData item, bool obtained)
        {
            Color rarityColor = item.rarity.ToColor();
            Color rowBg = obtained
                ? new Color(0.12f, 0.12f, 0.18f, 0.95f)
                : new Color(0.07f, 0.07f, 0.09f, 0.95f);

            var row = new GameObject("ItemRow");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 68);
            row.AddComponent<Image>().color = rowBg;

            // 왼쪽 등급 색 바
            var bar = new GameObject("Bar");
            bar.transform.SetParent(row.transform, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0); barRt.anchorMax = new Vector2(0, 1);
            barRt.offsetMin = Vector2.zero;       barRt.offsetMax = new Vector2(4, 0);
            bar.AddComponent<Image>().color = obtained ? rarityColor : new Color(0.25f, 0.25f, 0.25f);

            // 이름 텍스트
            var tGo = new GameObject("Name");
            tGo.transform.SetParent(row.transform, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0.5f); tRt.anchorMax = new Vector2(0.65f, 1f);
            tRt.offsetMin = new Vector2(14, 0);   tRt.offsetMax = new Vector2(0, 0);
            var nameTmp = tGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text      = obtained ? item.itemName : "???";
            nameTmp.fontSize  = 26;
            nameTmp.fontStyle = obtained ? FontStyles.Normal : FontStyles.Italic;
            nameTmp.color     = obtained ? Color.white : new Color(0.35f, 0.35f, 0.38f);
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;

            // 슬롯 + 등급 배지 (획득한 경우만)
            if (obtained)
            {
                var badgeGo = new GameObject("Badge");
                badgeGo.transform.SetParent(row.transform, false);
                var badgeRt = badgeGo.AddComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0, 0);    badgeRt.anchorMax = new Vector2(0.65f, 0.52f);
                badgeRt.offsetMin = new Vector2(14, 0);   badgeRt.offsetMax = new Vector2(0, 0);
                var badgeTmp = badgeGo.AddComponent<TextMeshProUGUI>();
                string hex = ColorUtility.ToHtmlStringRGB(rarityColor);
                badgeTmp.text      = $"<color=#888888>[{item.slot.ToKorean()}]</color>  <color=#{hex}>[{item.rarity.ToKorean()}]</color>";
                badgeTmp.fontSize  = 20;
                badgeTmp.richText  = true;
                badgeTmp.color     = Color.white;
                badgeTmp.alignment = TextAlignmentOptions.MidlineLeft;
                badgeTmp.raycastTarget = false;

                // 효과 텍스트
                if (item.modifiers != null && item.modifiers.Length > 0)
                {
                    var modGo = new GameObject("Mods");
                    modGo.transform.SetParent(row.transform, false);
                    var modRt = modGo.AddComponent<RectTransform>();
                    modRt.anchorMin = new Vector2(0.65f, 0); modRt.anchorMax = new Vector2(1f, 1f);
                    modRt.offsetMin = new Vector2(8, 4);     modRt.offsetMax = new Vector2(-12, -4);
                    var modTmp = modGo.AddComponent<TextMeshProUGUI>();
                    var sb = new System.Text.StringBuilder();
                    foreach (var mod in item.modifiers)
                    {
                        if (sb.Length > 0) sb.Append("\n");
                        sb.Append(mod.ToDisplayString());
                    }
                    modTmp.text      = sb.ToString();
                    modTmp.fontSize  = 19;
                    modTmp.color     = UITheme.TxtMod;
                    modTmp.alignment = TextAlignmentOptions.MidlineLeft;
                    modTmp.textWrappingMode = TextWrappingModes.Normal;
                    modTmp.raycastTarget = false;
                }
            }
        }
    }
}
