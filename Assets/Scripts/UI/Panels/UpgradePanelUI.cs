using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI.Panels
{
    public class UpgradePanelUI : MonoBehaviour
    {
        private Transform _listContent;
        private TextMeshProUGUI _skillSummaryText;
        private bool _built = false;

        private void Start()
        {
            BuildLayout();
            _built = true;
            Refresh();
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased += Refresh;
            if (MonsterManager.Instance != null)
                MonsterManager.Instance.OnStageChanged += _ => Refresh();
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnStatsChanged += RefreshSummary;
        }

        private void OnEnable()
        {
            if (_built) { Refresh(); RefreshSummary(); }
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= Refresh;
            if (MonsterManager.Instance != null)
                MonsterManager.Instance.OnStageChanged -= _ => Refresh();
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnStatsChanged -= RefreshSummary;
        }

        private void BuildLayout()
        {
            UIHelper.MakeText(transform, "스킬", 42, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, -100), offsetMax: new Vector2(0, -20));

            // 스킬 보너스 요약 박스 (타이틀 아래, 스크롤 위)
            var summaryBox = new GameObject("SkillSummaryBox");
            summaryBox.transform.SetParent(transform, false);
            var boxRt = summaryBox.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0, 1);
            boxRt.anchorMax = new Vector2(1, 1);
            boxRt.offsetMin = new Vector2(10, -230);
            boxRt.offsetMax = new Vector2(-10, -100);
            var boxImg = summaryBox.AddComponent<UnityEngine.UI.Image>();
            boxImg.color = new Color(0.08f, 0.14f, 0.22f, 1f);

            var summaryTextObj = new GameObject("SummaryText");
            summaryTextObj.transform.SetParent(summaryBox.transform, false);
            var stRt = summaryTextObj.AddComponent<RectTransform>();
            stRt.anchorMin = Vector2.zero; stRt.anchorMax = Vector2.one;
            stRt.offsetMin = new Vector2(14, 8); stRt.offsetMax = new Vector2(-14, -8);
            _skillSummaryText = summaryTextObj.AddComponent<TextMeshProUGUI>();
            _skillSummaryText.fontSize = 24;
            _skillSummaryText.color = new Color(0.8f, 0.95f, 0.8f);
            _skillSummaryText.richText = true;
            _skillSummaryText.raycastTarget = false;

            GameObject scrollObj = UIHelper.MakeScrollView(transform, out _listContent);
            RectTransform scrollRt = scrollObj.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(8, 8);
            scrollRt.offsetMax = new Vector2(-8, -235);   // 요약 박스 높이만큼 위로
        }

        private void RefreshSummary()
        {
            if (_skillSummaryText == null || PlayerStats.Instance == null) return;
            var ps = PlayerStats.Instance;

            string Fmt(double v, string unit = "") =>
                v == 0 ? $"<color=#555>  0{unit}</color>" : $"<color=#7EFF7E>+{NumberFormatter.Format(v)}{unit}</color>";

            _skillSummaryText.text =
                "<color=#AACCFF>[ 스킬 보너스 합계 ]</color>\n" +
                $"클릭 데미지       {Fmt(ps.UpgradeClickDamage)}\n" +
                $"자동공격 데미지  {Fmt(ps.UpgradeAutoDamage)}\n" +
                $"공격속도          {Fmt(ps.UpgradeAttackSpeed, "/s")}\n" +
                $"자동공격속도     {Fmt(ps.UpgradeAutoAttackSpeed, "/s")}\n" +
                $"골드 배율          {Fmt(ps.UpgradeGoldMultiplier)}";
        }

        private void Refresh()
        {
            RefreshSummary();
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

            bool anyVisible = false;
            foreach (var upgrade in upgrades)
            {
                if (upgrade == null) continue;
                if (!UpgradeManager.Instance.IsUnlocked(upgrade)) continue;
                anyVisible = true;
                try { CreateSkillRow(upgrade); }
                catch (Exception e) { Debug.LogError($"[UpgradePanelUI] {upgrade.name}: {e.Message}"); }
            }
            if (!anyVisible) ShowEmpty("스킬 없음");
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
            rowRt.sizeDelta = new Vector2(0, 175);
            Image rowBg = row.AddComponent<Image>();
            rowBg.color = maxed
                ? new Color(0.22f, 0.18f, 0.05f, 1f)
                : new Color(0.12f, 0.12f, 0.18f, 1f);

            string categoryTag = upgrade.statType switch
            {
                StatType.AttackSpeed     => "<color=#88DDFF>[클릭공속] </color>",
                StatType.AutoAttackSpeed => "<color=#AAFFAA>[자동공속] </color>",
                StatType.AutoDamage      => "<color=#FFCC88>[자동공격] </color>",
                StatType.GoldMultiplier  => "<color=#FFEE55>[골드] </color>",
                _                        => "<color=#FFFFFF>[클릭] </color>"
            };

            // 이름 + 레벨 (상단)
            string levelStr = maxed ? " [MAX]" : $" Lv.{lv}" + (upgrade.maxLevel > 0 ? $"/{upgrade.maxLevel}" : "");
            MakeRowLabel(row.transform, categoryTag + upgrade.upgradeName + levelStr,
                fontSize: 32, color: Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -70), offsetMax: new Vector2(-158, -4));

            // 설명 라벨: 이름 아래
            MakeRowLabel(row.transform, upgrade.description,
                fontSize: 24, color: new Color(0.72f, 0.72f, 0.72f),
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-158, -78),
                wrap: true);

            // Right: Buy button
            string btnLabel = maxed ? "MAX" : (canBuy ? $"구매\n{NumberFormatter.Format(cost)}G" : $"{NumberFormatter.Format(cost)}G");
            Color btnColor = maxed
                ? new Color(0.4f, 0.32f, 0.05f)
                : (canBuy ? new Color(0.15f, 0.5f, 0.15f) : new Color(0.28f, 0.18f, 0.18f));
            GameObject btn = UIHelper.MakeButton(row.transform, btnLabel, 22, btnColor);
            RectTransform btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1, 0.5f);
            btnRt.anchorMax = new Vector2(1, 0.5f);
            btnRt.pivot     = new Vector2(1, 0.5f);
            btnRt.anchoredPosition = new Vector2(-10, 0);
            btnRt.sizeDelta = new Vector2(140, 120);

            Button button = btn.GetComponent<Button>();
            if (canBuy)
                button.onClick.AddListener(() => UpgradeManager.Instance.Buy(upgrade));
            else
                button.interactable = false;
        }

        private static void MakeRowLabel(Transform parent, string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool wrap = false)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = wrap
                ? TMPro.TextWrappingModes.Normal
                : TMPro.TextWrappingModes.NoWrap;
            tmp.overflowMode  = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
        }
    }
}
