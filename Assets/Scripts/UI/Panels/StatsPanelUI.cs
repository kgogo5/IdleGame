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
            UIHelper.MakeText(transform, "스탯", 28, TextAnchor.UpperCenter,
                anchorMin: new Vector2(0, 1), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(0, -60), offsetMax: Vector2.zero);

            GameObject textObj = UIHelper.MakeText(transform, "", 18, TextAnchor.UpperLeft,
                anchorMin: new Vector2(0, 0), anchorMax: new Vector2(1, 1),
                offsetMin: new Vector2(20, 20), offsetMax: new Vector2(-20, -70));
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
                $"클릭 데미지:   {NumberFormatter.Format(ps.ClickDamage)}\n\n" +
                $"자동 공격:       {NumberFormatter.Format(ps.AutoDPS)} / 초\n\n" +
                $"골드 배율:       x{ps.GoldMultiplier:F2}";
        }
    }
}
