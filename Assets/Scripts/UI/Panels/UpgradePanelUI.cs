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
        private TextMeshProUGUI _sumC1, _sumC2, _sumC3;
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

        private void OnEnable()  { if (_built) { Refresh(); RefreshSummary(); } }
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

            // 스킬 보너스 합계 박스
            var statBox = new GameObject("SkillSummaryBox");
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
            titleTmp.text = "[ 스킬 보너스 합계 ]";
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

            _sumC1 = MakeColTmp(colsRow.transform);
            _sumC2 = MakeColTmp(colsRow.transform);
            _sumC3 = MakeColTmp(colsRow.transform);

            var scrollGo = UIHelper.MakeScrollView(body.transform, out _listContent);
            var scrollLe = scrollGo.AddComponent<LayoutElement>();
            scrollLe.minHeight = 100;
            scrollLe.flexibleHeight = 1;
        }

        private static TextMeshProUGUI MakeColTmp(Transform parent)
        {
            var go = new GameObject("Col");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.color = UITheme.TxtStatSkill;
            tmp.richText = true;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private void RefreshSummary()
        {
            if (_sumC1 == null || PlayerStats.Instance == null) return;
            var ps = PlayerStats.Instance;

            _sumC1.text =
                $"클릭 데미지\n{UITheme.SkillBonus(ps.UpgradeClickDamage)}\n\n" +
                $"자동공격\n{UITheme.SkillBonus(ps.UpgradeAutoDamage)}";

            _sumC2.text =
                $"공격속도\n{UITheme.SkillSpeed(ps.UpgradeAttackSpeed)}\n\n" +
                $"자동공격속도\n{UITheme.SkillSpeed(ps.UpgradeAutoAttackSpeed)}";

            _sumC3.text = $"골드 배율\n{UITheme.SkillBonus(ps.UpgradeGoldMultiplier)}";
        }

        private void Refresh()
        {
            RefreshSummary();
            if (_listContent == null || UpgradeManager.Instance == null) return;

            foreach (Transform child in _listContent)
                Destroy(child.gameObject);

            var upgrades = UpgradeManager.Instance.Upgrades;
            if (upgrades == null || upgrades.Length == 0) { ShowEmpty("스킬 데이터 없음"); return; }

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
                color: UITheme.TxtEmpty);
        }

        private void CreateSkillRow(UpgradeData upgrade)
        {
            int lv    = UpgradeManager.Instance.GetLevel(upgrade);
            bool maxed  = upgrade.maxLevel > 0 && lv >= upgrade.maxLevel;
            bool canBuy = !maxed && UpgradeManager.Instance.CanBuy(upgrade);
            double cost = UpgradeManager.Instance.GetNextCost(upgrade);

            var row = new GameObject(upgrade.name + "_Row");
            row.transform.SetParent(_listContent, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 175);
            row.AddComponent<Image>().color = maxed ? UITheme.BgRowMaxed : UITheme.BgRowSkill;

            string categoryTag = upgrade.statType switch
            {
                StatType.AttackSpeed     => "<color=#88DDFF>[클릭공속] </color>",
                StatType.AutoAttackSpeed => "<color=#AAFFAA>[자동공속] </color>",
                StatType.AutoDamage      => "<color=#FFCC88>[자동공격] </color>",
                StatType.GoldMultiplier  => $"<color={UITheme.HexGold}>[골드] </color>",
                _                        => "<color=#FFFFFF>[클릭] </color>"
            };
            string levelStr = maxed ? " [MAX]" : $" Lv.{lv}" + (upgrade.maxLevel > 0 ? $"/{upgrade.maxLevel}" : "");

            MakeRowLabel(row.transform, categoryTag + upgrade.upgradeName + levelStr,
                fontSize: 32, color: Color.white,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, -70), offsetMax: new Vector2(-158, -4));

            MakeRowLabel(row.transform, upgrade.description,
                fontSize: 24, color: UITheme.TxtDesc,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(14, 6), offsetMax: new Vector2(-158, -78),
                wrap: true);

            string btnLabel = maxed ? "MAX" : (canBuy ? $"구매\n{NumberFormatter.Format(cost)}G" : $"{NumberFormatter.Format(cost)}G");
            Color  btnColor = maxed ? UITheme.BtnMaxed : (canBuy ? UITheme.BtnBuyable : UITheme.BtnTooExpensive);

            var btn   = UIHelper.MakeButton(row.transform, btnLabel, 22, btnColor);
            var btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(1, 0.5f); btnRt.anchorMax = new Vector2(1, 0.5f);
            btnRt.pivot     = new Vector2(1, 0.5f);
            btnRt.anchoredPosition = new Vector2(-10, 0);
            btnRt.sizeDelta = new Vector2(140, 120);

            var button = btn.GetComponent<Button>();
            if (canBuy) button.onClick.AddListener(() => UpgradeManager.Instance.Buy(upgrade));
            else        button.interactable = false;
        }

        private static void MakeRowLabel(Transform parent, string text, float fontSize, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, bool wrap = false)
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
            tmp.alignment        = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            tmp.overflowMode     = TextOverflowModes.Ellipsis;
            tmp.raycastTarget    = false;
        }
    }
}
