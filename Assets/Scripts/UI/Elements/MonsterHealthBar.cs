using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;

namespace IdleGame.UI
{
    public class MonsterHealthBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _stageText;

        private void Start()
        {
            MonsterManager.Instance.OnMonsterSpawned += BindToMonster;
            MonsterManager.Instance.OnStageChanged += UpdateStage;
            UpdateStage(MonsterManager.Instance.Stage);
        }

        private void BindToMonster(Monster monster)
        {
            monster.OnHealthChanged += UpdateHealthBar;
            if (_nameText != null)
                _nameText.text = monster.MonsterName;
            UpdateHealthBar(monster.CurrentHealth, monster.MaxHealth);
        }

        private void UpdateHealthBar(double currentHealth, double maxHealth)
        {
            if (_fillImage != null)
                _fillImage.fillAmount = maxHealth > 0 ? (float)(currentHealth / maxHealth) : 0f;
            if (_healthText != null)
                _healthText.text = $"{currentHealth:F0} / {maxHealth:F0}";
        }

        private void UpdateStage(int stage)
        {
            if (_stageText != null)
                _stageText.text = $"Stage {stage}";
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnMonsterSpawned -= BindToMonster;
                MonsterManager.Instance.OnStageChanged -= UpdateStage;
            }
        }
    }
}
