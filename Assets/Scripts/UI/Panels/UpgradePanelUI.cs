using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI.Panels
{
    public class UpgradePanelUI : MonoBehaviour
    {
        private Transform _listContent;
        private bool _built = false;

        private void Start()
        {
            BuildLayout();
            _built = true;
            Refresh();
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased += Refresh;
        }

        private void OnEnable()
        {
            if (_built) Refresh();
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= Refresh;
        }

        private void BuildLayout()
        {
            UIHelper.MakeText(transform, "스킬", 36, TextAnchor.UpperCenter,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(0, -70), offsetMax: Vector2.zero);

            GameObject scrollObj = UIHelper.MakeScrollView(transform, out _listContent);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8, 8);
            scrollRt.offsetMax = new Vector2(-8, -75);
        }

        private void Refresh()
        {
            if (_listContent == null) return;
            if (UpgradeManager.Instance == null) return;

            foreach (Transform child in _listContent)
                Destroy(child.gameObject);

            var upgrades = UpgradeManager.Instance.Upgrades;
            if (upgrades == null || upgrades.Length == 0)
            {
                ShowEmpty("스킬 데이터 없음");
                return;
            }

            foreach (var upgrade in upgrades)
            {
                if (upgrade == null) continue;
                try { CreateSkillRow(upgrade); }
                catch (Exception e) { Debug.LogError($"[UpgradePanelUI] {upgrade.name}: {e.Message}"); }
            }
        }

        private void ShowEmpty(string msg)
        {
            UIHelper.MakeText(_listContent, msg, 18, TextAnchor.MiddleCenter,
                anchorMin: new Vector2(0, 0.4f), anchorMax: new Vector2(1, 0.6f),
                color: new Color(0.6f, 0.6f, 0.6f));
        }

        private void CreateSkillRow(UpgradeData upgrade)
        {
            int lv = UpgradeManager.Instance.GetLevel(upgrade);
            bool maxed = upgrade.maxLevel > 0 && lv >= upgrade.maxLevel;
            bool canBuy = !maxed && UpgradeManager.Instance.CanBuy(upgrade);
            double cost = UpgradeManager.Instance.GetNextCost(upgrade);

            // Row container
            GameObject row = new GameObject(upgrade.name + "_Row");
            row.transform.SetParent(_listContent, false);
            RectTransform rowRt = row.AddComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0, 120);
            Image rowBg = row.AddComponent<Image>();
            rowBg.color = maxed
                ? new Color(0.22f, 0.18f, 0.05f, 1f)
                : new Color(0.12f, 0.12f, 0.18f, 1f);

            // Left: Name + level
            string levelStr = maxed ? " [MAX]" : $" Lv.{lv}" + (upgrade.maxLevel > 0 ? $"/{upgrade.maxLevel}" : "");
            UIHelper.MakeText(row.transform, upgrade.upgradeName + levelStr, 28, TextAnchor.MiddleLeft,
                offsetMin: new Vector2(14, 16), offsetMax: new Vector2(-155, 0));
            UIHelper.MakeText(row.transform, upgrade.description, 22, TextAnchor.MiddleLeft,
                offsetMin: new Vector2(14, -20), offsetMax: new Vector2(-155, -20),
                color: new Color(0.75f, 0.75f, 0.75f));

            // Right: Buy button
            string btnLabel = maxed ? "MAX" : (canBuy ? $"구매\n{NumberFormatter.Format(cost)}G" : $"{NumberFormatter.Format(cost)}G");
            Color btnColor = maxed
                ? new Color(0.4f, 0.32f, 0.05f)
                : (canBuy ? new Color(0.15f, 0.5f, 0.15f) : new Color(0.28f, 0.18f, 0.18f));
            GameObject btn = UIHelper.MakeButton(row.transform, btnLabel, 22, btnColor);
            RectTransform btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1, 0.5f);
            btnRt.anchorMax = new Vector2(1, 0.5f);
            btnRt.pivot = new Vector2(1, 0.5f);
            btnRt.anchoredPosition = new Vector2(-10, 0);
            btnRt.sizeDelta = new Vector2(140, 88);

            Button button = btn.GetComponent<Button>();
            if (canBuy)
                button.onClick.AddListener(() => UpgradeManager.Instance.Buy(upgrade));
            else
                button.interactable = false;
        }
    }
}
