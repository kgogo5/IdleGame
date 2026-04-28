using UnityEngine;
using IdleGame.Managers;
using IdleGame.Utils;
using TMPro;

namespace IdleGame.UI.Panels
{
    public class StatsPanelUI : MonoBehaviour
    {
        private TextMeshProUGUI _statsText;

        private void Start()
        {
            UIHelper.MakeText(transform, "스탯", 42, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, -100), offsetMax: new Vector2(0, -20));

            GameObject textObj = UIHelper.MakeText(transform, "", 30, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(30, 30), offsetMax: new Vector2(-30, -90));
            _statsText = textObj.GetComponent<TextMeshProUGUI>();

            if (PlayerStats.Instance != null)
            {
                PlayerStats.Instance.OnStatsChanged += RefreshStats;
                RefreshStats();
            }
        }

        private void OnDestroy()
        {
            if (PlayerStats.Instance != null)
                PlayerStats.Instance.OnStatsChanged -= RefreshStats;
        }

        private void RefreshStats()
        {
            if (_statsText == null || PlayerStats.Instance == null) return;

            var ps = PlayerStats.Instance;
            _statsText.text =
                $"클릭 데미지       {NumberFormatter.Format(ps.ClickDamage)}\n" +
                $"클릭 공격속도    {ps.AttackSpeed:F1} 회/초\n\n" +
                $"자동공격 데미지  {NumberFormatter.Format(ps.AutoDamage)}\n" +
                $"자동공격 속도    {ps.AutoAttackSpeed:F1} 회/초\n\n" +
                $"골드 배율          x{ps.GoldMultiplier:F2}\n\n" +
                $"⚔ 전투력           {NumberFormatter.Format(ps.CombatPower)}";
        }
    }
}
