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
        private const int ROW_H      = 160;
        private const int SET_ROW_H  = 130;
        private const int BTN_W      = 150;
        private const int BTN_H      = 110;
        private const float NAME_F   = 32f;
        private const float DESC_F   = 26f;

        private Transform _listContent;
        private bool _built;

        private void Start()
        {
            UIHelper.MakeText(transform, "장비", 42, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, -100), offsetMax: new Vector2(0, -20));

            GameObject scrollObj = UIHelper.MakeScrollView(transform, out _listContent);
            RectTransform sr = scrollObj.GetComponent<RectTransform>();
            sr.anchorMin = Vector2.zero;
            sr.anchorMax = Vector2.one;
            sr.offsetMin = new Vector2(10, 10);
            sr.offsetMax = new Vector2(-10, -85);

            _built = true;
            Refresh();
            InventoryManager.Instance.OnInventoryChanged += Refresh;
            InventoryManager.Instance.OnEquipChanged     += Refresh;
        }

        private void OnEnable()  { if (_built) Refresh(); }
        private void OnDestroy()
        {
            if (InventoryManager.Instance == null) return;
            InventoryManager.Instance.OnInventoryChanged -= Refresh;
            InventoryManager.Instance.OnEquipChanged     -= Refresh;
        }

        private void Refresh()
        {
            if (_listContent == null || InventoryManager.Instance == null) return;
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            // 장비 (비스택)
            var equips = new List<ItemData>();
            var consumables = new List<ItemData>();
            foreach (var item in InventoryManager.Instance.ShopItems)
            {
                if (item == null || !InventoryManager.Instance.IsOwned(item)) continue;
                if (item.isStackable) consumables.Add(item);
                else equips.Add(item);
            }

            equips.Sort((a, b) =>
            {
                bool aEq = InventoryManager.Instance.IsEquipped(a);
                bool bEq = InventoryManager.Instance.IsEquipped(b);
                if (aEq != bEq) return bEq.CompareTo(aEq);
                return ((int)b.rarity).CompareTo((int)a.rarity);
            });

            if (equips.Count == 0)
                AddEmptyLabel("보유 장비 없음");
            else
                foreach (var item in equips) CreateItemRow(item);

            // 소모품
            if (consumables.Count > 0)
            {
                AddSectionHeader("── 소모품 ──");
                foreach (var item in consumables) CreateConsumableRow(item);
            }

            if (InventoryManager.Instance.SetBonuses != null)
            {
                bool hasSet = false;
                foreach (var setData in InventoryManager.Instance.SetBonuses)
                {
                    int count = CountEquipped(setData);
                    if (count == 0) continue;
                    if (!hasSet)
                    {
                        hasSet = true;
                        AddSectionHeader("── 세트 보너스 ──");
                    }
                    CreateSetBonusRow(setData, count);
                }
            }
        }

        private void CreateItemRow(ItemData item)
        {
            bool equipped = InventoryManager.Instance.IsEquipped(item);
            Color bgColor = equipped ? new Color(0.10f, 0.22f, 0.14f) : new Color(0.13f, 0.13f, 0.18f);

            GameObject row = MakeRow(bgColor, ROW_H);

            string hex = ColorUtility.ToHtmlStringRGB(item.rarity.ToColor());
            string nameText = $"<color=#888888>[{item.slot.ToKorean()}]</color> <color=#{hex}>[{item.rarity.ToKorean()}]</color> {item.itemName}";

            RowLabel(row.transform, nameText, NAME_F, Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-(BTN_W + 18), -4),
                richText: true);

            RowLabel(row.transform, GetModsText(item), DESC_F, new Color(0.70f, 0.85f, 0.70f),
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-(BTN_W + 18), -80));

            Color btnColor = equipped ? new Color(0.55f, 0.15f, 0.15f) : new Color(0.15f, 0.45f, 0.25f);
            string btnLabel = equipped ? "해제" : "장착";
            System.Action onClick = equipped
                ? () => InventoryManager.Instance.Unequip(item)
                : () => InventoryManager.Instance.Equip(item);
            MakeBtn(row.transform, btnLabel, btnColor, BTN_W, BTN_H, 30f, onClick);
        }

        private void CreateConsumableRow(ItemData item)
        {
            int count = InventoryManager.Instance.GetCount(item);
            GameObject row = MakeRow(new Color(0.13f, 0.13f, 0.18f), ROW_H);

            RowLabel(row.transform, $"{item.itemName}  x{count}", NAME_F, new Color(0.85f, 0.85f, 0.85f),
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -74), offsetMax: new Vector2(-14, -4));

            RowLabel(row.transform, GetModsText(item), DESC_F, new Color(0.70f, 0.85f, 0.70f),
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-14, -80));
        }

        private void CreateSetBonusRow(SetBonusData setData, int equippedCount)
        {
            var step = setData.GetActiveStep(equippedCount);
            GameObject row = MakeRow(new Color(0.18f, 0.14f, 0.06f), SET_ROW_H);

            RowLabel(row.transform,
                $"{setData.setName}  ({equippedCount}/{setData.itemNames.Length})",
                NAME_F, new Color(1f, 0.85f, 0.3f),
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -66), offsetMax: new Vector2(-14, -4));

            RowLabel(row.transform, step?.description ?? "", DESC_F, new Color(0.95f, 0.8f, 0.4f),
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
                color: new Color(0.5f, 0.6f, 0.7f));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
        }

        private void AddEmptyLabel(string text)
        {
            var go = UIHelper.MakeText(_listContent, text, 28, TextAnchor.MiddleCenter,
                anchorMin: Vector2.zero, anchorMax: Vector2.one,
                offsetMin: Vector2.zero, offsetMax: Vector2.zero,
                color: new Color(0.4f, 0.4f, 0.4f));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 65);
        }

        private GameObject MakeRow(Color bgColor, int height)
        {
            GameObject row = new GameObject("Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            row.AddComponent<Image>().color = bgColor;
            return row;
        }

        private static void RowLabel(Transform parent, string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            bool richText = false, bool wrap = false)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = fontSize;
            tmp.color            = color;
            tmp.richText         = richText;
            tmp.alignment        = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = wrap ? TMPro.TextWrappingModes.Normal : TMPro.TextWrappingModes.NoWrap;
            tmp.overflowMode     = TextOverflowModes.Ellipsis;
            tmp.raycastTarget    = false;
        }

        private static void MakeBtn(Transform parent, string label, Color color,
            float width, float height, float fontSize, System.Action onClick)
        {
            GameObject go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-10, 0);
            rt.sizeDelta = new Vector2(width, height);
            go.AddComponent<Image>().color = color;
            go.AddComponent<Button>().onClick.AddListener(() => onClick());

            GameObject tGo = new GameObject("Label");
            tGo.transform.SetParent(go.transform, false);
            RectTransform trt = tGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.color     = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
    }
}
