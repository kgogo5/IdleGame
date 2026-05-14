using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI.Panels
{
    public class EquipmentPanelUI : MonoBehaviour
    {
        private const int   ROW_H     = 160;
        private const int   SET_ROW_H = 130;
        private const int   BTN_W     = 150;
        private const int   BTN_H     = 110;
        private const float NAME_F    = 32f;
        private const float DESC_F    = 26f;

        private Transform _listContent;
        private TextMeshProUGUI _statC1, _statC2, _statC3;
        private bool _built;

        private EquipSlot? _activeSlotFilter = null;
        private GameObject _filterDropdown;
        private Image[]    _filterBtnImages;
        private TextMeshProUGUI _filterToggleLabel;

        private static readonly EquipSlot?[] FILTER_SLOTS =
        {
            null,
            EquipSlot.Weapon, EquipSlot.Helmet, EquipSlot.Armor, EquipSlot.Gloves,
            EquipSlot.Ring,   EquipSlot.Amulet, EquipSlot.Talisman,
        };
        private static readonly string[] FILTER_LABELS =
        { "전체", "무기", "투구", "갑옷", "장갑", "반지", "목걸이", "부적" };

        private void Start()
        {
            UIHelper.MakeText(transform, "장비", 42, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, -100), offsetMax: new Vector2(0, -20));

            // body: 타이틀 아래 ~ 하단 채움
            var body = new GameObject("Body");
            body.transform.SetParent(transform, false);
            var bodyRt = body.AddComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = Vector2.one;
            bodyRt.offsetMin = new Vector2(8, 8);
            bodyRt.offsetMax = new Vector2(-8, -90);
            var bodyVlg = body.AddComponent<VerticalLayoutGroup>();
            bodyVlg.spacing = 8;
            bodyVlg.childControlWidth = true;
            bodyVlg.childControlHeight = true;
            bodyVlg.childForceExpandWidth = true;
            bodyVlg.childForceExpandHeight = false;

            // 최종 스탯 박스
            var statBox = new GameObject("StatSummaryBox");
            statBox.transform.SetParent(body.transform, false);
            statBox.AddComponent<Image>().color = UITheme.BgStatBox;
            var statVlg = statBox.AddComponent<VerticalLayoutGroup>();
            statVlg.padding = new RectOffset(0, 0, 10, 16);
            statVlg.spacing = 6;
            statVlg.childControlWidth = true;
            statVlg.childControlHeight = true;
            statVlg.childForceExpandWidth = true;
            statVlg.childForceExpandHeight = false;

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(statBox.transform, false);
            titleGo.AddComponent<LayoutElement>().preferredHeight = 36;
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "[ 최종 스탯 ]";
            titleTmp.fontSize = 24;
            titleTmp.color = UITheme.TxtHeading;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.richText = false;
            titleTmp.raycastTarget = false;

            var colsRow = new GameObject("Cols");
            colsRow.transform.SetParent(statBox.transform, false);
            var colsHlg = colsRow.AddComponent<HorizontalLayoutGroup>();
            colsHlg.padding = new RectOffset(14, 14, 4, 0);
            colsHlg.spacing = 8;
            colsHlg.childControlWidth = true;
            colsHlg.childControlHeight = true;
            colsHlg.childForceExpandWidth = true;
            colsHlg.childForceExpandHeight = false;

            _statC1 = MakeColTmp(colsRow.transform);
            _statC2 = MakeColTmp(colsRow.transform);
            _statC3 = MakeColTmp(colsRow.transform);

            // 필터 버튼 (레이아웃 내, 항상 표시)
            var toggleRow = new GameObject("FilterToggle");
            toggleRow.transform.SetParent(body.transform, false);
            toggleRow.AddComponent<LayoutElement>().preferredHeight = 44;
            var toggleImg = toggleRow.AddComponent<Image>();
            toggleImg.color = new Color(0.16f, 0.18f, 0.24f);
            var toggleBtn = toggleRow.AddComponent<Button>();
            toggleBtn.targetGraphic = toggleImg;

            var tLabelGo = new GameObject("Label");
            tLabelGo.transform.SetParent(toggleRow.transform, false);
            var tLabelRt = tLabelGo.AddComponent<RectTransform>();
            tLabelRt.anchorMin = Vector2.zero; tLabelRt.anchorMax = Vector2.one;
            tLabelRt.offsetMin = new Vector2(14, 0); tLabelRt.offsetMax = new Vector2(-14, 0);
            _filterToggleLabel = tLabelGo.AddComponent<TextMeshProUGUI>();
            _filterToggleLabel.fontSize  = 22;
            _filterToggleLabel.color     = new Color(0.78f, 0.82f, 0.95f);
            _filterToggleLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _filterToggleLabel.raycastTarget = false;
            UpdateFilterToggleLabel();

            // 모달 오버레이 — 패널 루트에 직접 부착, 항상 최상위 렌더
            _filterDropdown = new GameObject("FilterModal");
            _filterDropdown.transform.SetParent(transform, false);
            var overlayRt = _filterDropdown.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = overlayRt.offsetMax = Vector2.zero;
            var overlayImg = _filterDropdown.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.55f);
            var overlayBtn = _filterDropdown.AddComponent<Button>();
            overlayBtn.targetGraphic = overlayImg;
            overlayBtn.onClick.AddListener(() => _filterDropdown.SetActive(false));

            // 카드 (오버레이 중앙)
            var card = new GameObject("Card");
            card.transform.SetParent(_filterDropdown.transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f); cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot     = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(340, 280);
            card.AddComponent<Image>().color = new Color(0.13f, 0.15f, 0.20f, 0.98f);

            // 카드 제목
            var cardTitle = new GameObject("Title");
            cardTitle.transform.SetParent(card.transform, false);
            var cardTitleRt = cardTitle.AddComponent<RectTransform>();
            cardTitleRt.anchorMin = new Vector2(0, 1); cardTitleRt.anchorMax = new Vector2(1, 1);
            cardTitleRt.offsetMin = new Vector2(0, -52); cardTitleRt.offsetMax = Vector2.zero;
            var cardTitleTmp = cardTitle.AddComponent<TextMeshProUGUI>();
            cardTitleTmp.text      = "슬롯 필터";
            cardTitleTmp.fontSize  = 28;
            cardTitleTmp.fontStyle = FontStyles.Bold;
            cardTitleTmp.color     = UITheme.TxtHeading;
            cardTitleTmp.alignment = TextAlignmentOptions.Center;
            cardTitleTmp.raycastTarget = false;

            // 버튼 그리드 (4×2)
            var grid = new GameObject("Grid");
            grid.transform.SetParent(card.transform, false);
            var gridRt = grid.AddComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0, 0); gridRt.anchorMax = new Vector2(1, 1);
            gridRt.offsetMin = new Vector2(12, 12); gridRt.offsetMax = new Vector2(-12, -56);
            var glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize        = new Vector2(72, 80);
            glg.spacing         = new Vector2(8, 8);
            glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment  = TextAnchor.MiddleCenter;
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;

            _filterBtnImages = new Image[FILTER_LABELS.Length];
            for (int i = 0; i < FILTER_LABELS.Length; i++)
            {
                int idx = i;
                var btnGo = new GameObject(FILTER_LABELS[i]);
                btnGo.transform.SetParent(grid.transform, false);
                var img = btnGo.AddComponent<Image>();
                img.color = GetFilterBtnColor(FILTER_SLOTS[i] == _activeSlotFilter);
                _filterBtnImages[i] = img;
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() =>
                {
                    _activeSlotFilter = FILTER_SLOTS[idx];
                    _filterDropdown.SetActive(false);
                    UpdateFilterToggleLabel();
                    UpdateFilterColors();
                    Refresh();
                });
                var tGo = new GameObject("L");
                tGo.transform.SetParent(btnGo.transform, false);
                var tRt = tGo.AddComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
                tRt.offsetMin = tRt.offsetMax = Vector2.zero;
                var tmp = tGo.AddComponent<TextMeshProUGUI>();
                tmp.text      = FILTER_LABELS[i];
                tmp.fontSize  = 21;
                tmp.color     = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
            }
            _filterDropdown.SetActive(false);
            _filterDropdown.transform.SetAsLastSibling(); // 항상 최상위

            toggleBtn.onClick.AddListener(() =>
            {
                _filterDropdown.SetActive(true);
                _filterDropdown.transform.SetAsLastSibling();
            });

            var scrollGo = UIHelper.MakeScrollView(body.transform, out _listContent);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.minHeight = 100;
            scrollLe.flexibleHeight = 1;

            _built = true;
            Refresh();
            RefreshStatSummary();
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            InventoryManager.Instance.OnEquipChanged     += Refresh;
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnStatsChanged += RefreshStatSummary;
        }

        private void OnEnable()  { if (_built) { Refresh(); RefreshStatSummary(); } }
        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnInventoryChanged -= Refresh;
                InventoryManager.Instance.OnEquipChanged     -= Refresh;
            }
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnStatsChanged -= RefreshStatSummary;
        }

        private static TextMeshProUGUI MakeColTmp(Transform parent)
        {
            var go = new GameObject("Col");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.color = UITheme.TxtStatEquip;
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private void RefreshStatSummary()
        {
            if (_statC1 == null || PlayerStats.Instance == null) return;
            var ps = PlayerStats.Instance;

            _statC1.text =
                $"클릭 데미지\n{UITheme.StatVal(NumberFormatter.Format(ps.ClickDamage))} <size=18>{UITheme.EquipPct(ps.EquipClickDamagePct)}</size>\n\n" +
                $"자동공격\n{UITheme.StatVal(NumberFormatter.Format(ps.AutoDamage))} <size=18>{UITheme.EquipPct(ps.EquipAutoDamagePct)}</size>\n\n" +
                $"크리티컬 확률\n{UITheme.StatVal($"{ps.CriticalChance * 100:F1}%")} <size=18>{UITheme.EquipPct(ps.EquipCriticalChancePct)}</size>";

            _statC2.text =
                $"공격속도\n{UITheme.StatVal($"{ps.AttackSpeed:F2}/s")} <size=18>{UITheme.EquipPct(ps.EquipAttackSpeedPct)}</size>\n\n" +
                $"자동속도\n{UITheme.StatVal($"{ps.AutoAttackSpeed:F2}/s")} <size=18>{UITheme.EquipPct(ps.EquipAutoAttackSpeedPct)}</size>\n\n" +
                $"크리티컬 배수\n{UITheme.StatVal($"x{ps.CriticalDamage:F2}")} <size=18>{UITheme.EquipPct(ps.EquipCriticalDamagePct)}</size>";

            string dropText = ps.EquipDropRatePct != 0
                ? $"\n\n드랍률\n{UITheme.EquipPct(ps.EquipDropRatePct)}" : "";
            _statC3.text =
                $"골드 배율\n{UITheme.StatGold($"x{ps.GoldMultiplier:F2}")} <size=18>{UITheme.EquipPct(ps.EquipGoldMultiplierPct)}</size>" +
                dropText;
        }

        private void Refresh()
        {
            if (_listContent == null || InventoryManager.Instance == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var equips     = new List<ItemData>();
            var talismans  = new List<ItemData>();
            var consumables = new List<ItemData>();
            foreach (var item in InventoryManager.Instance.ShopItems)
            {
                if (item == null || !InventoryManager.Instance.IsOwned(item)) continue;
                if (_activeSlotFilter != null && item.slot != _activeSlotFilter) continue;
                if (item.isStackable)                consumables.Add(item);
                else if (item.slot == EquipSlot.Talisman) talismans.Add(item);
                else                                 equips.Add(item);
            }

            System.Comparison<ItemData> equipSort = (a, b) =>
            {
                bool aEq = InventoryManager.Instance.IsEquipped(a);
                bool bEq = InventoryManager.Instance.IsEquipped(b);
                if (aEq != bEq) return bEq.CompareTo(aEq);
                return ((int)b.rarity).CompareTo((int)a.rarity);
            };

            equips.Sort(equipSort);

            if (equips.Count == 0)
            {
                // 부적 필터 중에는 equips가 당연히 비어있으므로 "없음" 메시지 미표시
                if (_activeSlotFilter != EquipSlot.Talisman)
                {
                    string emptyMsg = _activeSlotFilter.HasValue
                        ? $"보유 {_activeSlotFilter.Value.ToKorean()} 없음"
                        : "보유 장비 없음";
                    AddEmptyLabel(emptyMsg);
                }
            }
            else
                foreach (var item in equips) CreateItemRow(item);

            if (talismans.Count > 0)
            {
                // 전체 보기에서만 부적 섹션 헤더 표시 (부적 필터 시 헤더 불필요)
                if (_activeSlotFilter == null) AddSectionHeader("- 부적 -");
                talismans.Sort(equipSort);
                foreach (var item in talismans) CreateTalismanRow(item);
            }

            if (consumables.Count > 0)
            {
                AddSectionHeader("- 소모품 -");
                foreach (var item in consumables) CreateConsumableRow(item);
            }

            if (_activeSlotFilter == null && InventoryManager.Instance.SetBonuses != null)
            {
                bool hasSet = false;
                foreach (var setData in InventoryManager.Instance.SetBonuses)
                {
                    int count = CountEquipped(setData);
                    if (count == 0) continue;
                    if (!hasSet) { hasSet = true; AddSectionHeader("- 세트 보너스 -"); }
                    CreateSetBonusRow(setData, count);
                }
            }
        }

        private string GetSetName(ItemData item)
        {
            if (InventoryManager.Instance.SetBonuses == null) return null;
            foreach (var setData in InventoryManager.Instance.SetBonuses)
                if (setData.itemNames != null && System.Array.IndexOf(setData.itemNames, item.name) >= 0)
                    return setData.setName;
            return null;
        }

        private void CreateItemRow(ItemData item)
        {
            bool equipped = InventoryManager.Instance.IsEquipped(item);
            var row = MakeRow(equipped ? UITheme.BgRowEquipped : UITheme.BgRowDefault, ROW_H);

            string hex     = ColorUtility.ToHtmlStringRGB(item.rarity.ToColor());
            string setName = GetSetName(item);
            string setTag  = setName != null ? $"  <size=22><color={UITheme.HexGold}>[{setName}]</color></size>" : "";
            string nameText = $"<color=#888888>[{item.slot.ToKorean()}]</color> <color=#{hex}>[{item.rarity.ToKorean()}]</color> {item.itemName}{setTag}";

            RowLabel(row.transform, nameText, NAME_F, Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-(BTN_W + 18), -4),
                richText: true);

            RowLabel(row.transform, GetModsText(item), DESC_F, UITheme.TxtMod,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-(BTN_W + 18), -80));

            string btnLabel = equipped ? "해제" : "장착";
            Color  btnColor = equipped ? UITheme.BtnUnequip : UITheme.BtnEquip;
            System.Action onClick = equipped
                ? () => InventoryManager.Instance.Unequip(item)
                : () => InventoryManager.Instance.Equip(item);
            MakeBtn(row.transform, btnLabel, btnColor, BTN_W, BTN_H, 30f, onClick);
        }

        private void CreateTalismanRow(ItemData item)
        {
            bool equipped = InventoryManager.Instance.IsEquipped(item);
            var row = MakeRow(equipped ? new Color(0.18f, 0.25f, 0.32f) : new Color(0.10f, 0.14f, 0.20f), ROW_H);

            string hex = ColorUtility.ToHtmlStringRGB(item.rarity.ToColor());
            string nameText = $"<color=#{hex}>[{item.rarity.ToKorean()}]</color> {item.itemName}";

            RowLabel(row.transform, nameText, NAME_F, Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-(BTN_W + 18), -4),
                richText: true);

            RowLabel(row.transform, GetModsText(item), DESC_F, UITheme.TxtMod,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-(BTN_W + 18), -80));

            string btnLabel = equipped ? "해제" : "장착";
            Color  btnColor = equipped ? UITheme.BtnUnequip : UITheme.BtnEquip;
            System.Action onClick = equipped
                ? () => InventoryManager.Instance.Unequip(item)
                : () => InventoryManager.Instance.Equip(item);
            MakeBtn(row.transform, btnLabel, btnColor, BTN_W, BTN_H, 30f, onClick);
        }

        private void CreateConsumableRow(ItemData item)
        {
            int count = InventoryManager.Instance.GetCount(item);
            var row = MakeRow(UITheme.BgRowDefault, ROW_H);

            RowLabel(row.transform, $"{item.itemName}  x{count}", NAME_F, new Color(0.85f, 0.85f, 0.85f),
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-14, -4));

            RowLabel(row.transform, GetModsText(item), DESC_F, UITheme.TxtMod,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-14, -80));
        }

        private void CreateSetBonusRow(SetBonusData setData, int equippedCount)
        {
            var step = setData.GetActiveStep(equippedCount);
            var row  = MakeRow(UITheme.BgRowSetBonus, SET_ROW_H);

            RowLabel(row.transform,
                $"{setData.setName}  ({equippedCount}/{setData.itemNames.Length})",
                NAME_F, UITheme.TxtSetName,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -66), offsetMax: new Vector2(-14, -4));

            RowLabel(row.transform, step?.description ?? "", DESC_F, UITheme.TxtSetDesc,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-14, -70));
        }

        private string GetModsText(ItemData item)
        {
            if (item.modifiers == null || item.modifiers.Length == 0) return "";
            var sb = new System.Text.StringBuilder();
            foreach (var mod in item.modifiers)
            {
                if (sb.Length > 0) sb.Append("   ");
                sb.Append(mod.ToDisplayString());
            }
            return sb.ToString();
        }

        private int CountEquipped(SetBonusData setData)
        {
            int count = 0;
            if (setData.itemNames == null) return 0;
            foreach (var item in InventoryManager.Instance.GetEquippedItems())
                if (System.Array.IndexOf(setData.itemNames, item.name) >= 0) count++;
            return count;
        }

        private void AddSectionHeader(string text)
        {
            var go = UIHelper.MakeText(_listContent, text, 28, TextAnchor.MiddleCenter,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: Vector2.zero, offsetMax: Vector2.zero,
                color: UITheme.TxtSubtle);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
        }

        private void AddEmptyLabel(string text)
        {
            var go = UIHelper.MakeText(_listContent, text, 28, TextAnchor.MiddleCenter,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: Vector2.zero, offsetMax: Vector2.zero,
                color: UITheme.TxtEmpty);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
        }

        private static Color GetFilterBtnColor(bool active) =>
            active ? new Color(0.25f, 0.55f, 0.90f) : new Color(0.18f, 0.20f, 0.26f);

        private void UpdateFilterColors()
        {
            if (_filterBtnImages == null) return;
            for (int i = 0; i < _filterBtnImages.Length; i++)
                if (_filterBtnImages[i] != null)
                    _filterBtnImages[i].color = GetFilterBtnColor(FILTER_SLOTS[i] == _activeSlotFilter);
        }

        private void UpdateFilterToggleLabel()
        {
            if (_filterToggleLabel == null) return;
            string slotName = _activeSlotFilter.HasValue ? _activeSlotFilter.Value.ToKorean() : "전체";
            _filterToggleLabel.text = $"≡  슬롯 필터: {slotName}";
        }

        private GameObject MakeRow(Color bgColor, int height)
        {
            var row = new GameObject("Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            row.AddComponent<Image>().color = bgColor;
            return row;
        }

        private static void RowLabel(Transform parent, string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool richText = false, bool wrap = false)
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
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode     = TextOverflowModes.Ellipsis;
            tmp.raycastTarget    = false;
        }

        private static void MakeBtn(Transform parent, string label, Color color,
            float width, float height, float fontSize, System.Action onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-10, 0);
            rt.sizeDelta = new Vector2(width, height);
            go.AddComponent<Image>().color = color;
            go.AddComponent<Button>().onClick.AddListener(() => onClick());

            var tGo = new GameObject("Label");
            tGo.transform.SetParent(go.transform, false);
            var trt = tGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
    }
}
