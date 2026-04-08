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

        private void Start()
        {
            // MonsterManager의 이벤트 구독
            MonsterManager.Instance.OnMonsterSpawned += BindToMonster;
        }

        private void BindToMonster(Monster monster)
        {
            monster.OnHealthChanged += UpdateHealthBar;

            if (_nameText != null)
            {
                _nameText.text = monster.MonsterName;
            }
        }

        private void UpdateHealthBar(double currentHealth, double maxHealth)
        {
            if (_fillImage != null)
            {
                float fillAmount = (float)(currentHealth / maxHealth);
                _fillImage.fillAmount = fillAmount;
            }

            if (_healthText != null)
            {
                _healthText.text = $"{currentHealth:F0} / {maxHealth:F0}";
            }
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnMonsterSpawned -= BindToMonster;
            }
        }
    }
}
