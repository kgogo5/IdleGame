using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;
using IdleGame.Managers;
using IdleGame.Utils;

namespace IdleGame.UI
{
    public class MonsterHealthBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TextMeshProUGUI _healthText;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _stageText;

        private Button _fleeButton;
        private TextMeshProUGUI _fleeLabel;

        private void Start()
        {
            MonsterManager.Instance.OnMonsterSpawned += BindToMonster;
            MonsterManager.Instance.OnStageChanged   += UpdateStage;
            UpdateStage(MonsterManager.Instance.Stage);

            CurrencyManager.Instance.OnGoldChanged += _ => RefreshFleeButton();
            NavigationController.OnTabChanged += OnTabChanged;

            CreateFleeButton();
            RefreshFleeButton();
        }

        private void CreateFleeButton()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            GameObject btn = new GameObject("FleeButton");
            btn.transform.SetParent(canvas.transform, false);

            RectTransform rt = btn.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -145);
            rt.sizeDelta        = new Vector2(150, 65);

            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.6f, 0.15f, 0.15f);

            _fleeButton = btn.AddComponent<Button>();
            _fleeButton.onClick.AddListener(() => {
                MonsterManager.Instance.Flee();
                RefreshFleeButton();
            });

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btn.transform, false);
            RectTransform lrt = labelObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _fleeLabel = labelObj.AddComponent<TextMeshProUGUI>();
            _fleeLabel.alignment  = TextAlignmentOptions.Center;
            _fleeLabel.fontSize   = 24;
            _fleeLabel.color      = Color.white;
            _fleeLabel.raycastTarget = false;
        }

        private void OnTabChanged(int tabIndex)
        {
            if (_fleeButton != null)
                _fleeButton.gameObject.SetActive(tabIndex == 2); // 2 = 전투 탭
        }

        private void RefreshFleeButton()
        {
            if (_fleeButton == null || _fleeLabel == null) return;
            bool canFlee = MonsterManager.Instance.CanFlee();
            double cost  = MonsterManager.Instance.FleeCost;
            _fleeButton.interactable = canFlee;
            _fleeButton.GetComponent<Image>().color = canFlee
                ? new Color(0.6f, 0.15f, 0.15f)
                : new Color(0.3f, 0.1f, 0.1f);
            _fleeLabel.text = $"도망\n{NumberFormatter.Format(cost)}G";
        }

        private void BindToMonster(Monster monster)
        {
            monster.OnHealthChanged += UpdateHealthBar;
            if (_nameText != null)
            {
                _nameText.text  = monster.IsBoss ? $"⚔ {monster.MonsterName} [BOSS]" : monster.MonsterName;
                _nameText.color = monster.IsBoss ? new Color(1f, 0.3f, 0.1f) : Color.white;
            }
            if (_fillImage != null)
                _fillImage.color = monster.IsBoss ? new Color(0.9f, 0.2f, 0.05f) : new Color(0.2f, 0.8f, 0.2f);
            UpdateHealthBar(monster.CurrentHealth, monster.MaxHealth);
            RefreshFleeButton();
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
            RefreshFleeButton();
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnMonsterSpawned -= BindToMonster;
                MonsterManager.Instance.OnStageChanged   -= UpdateStage;
            }
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnGoldChanged -= _ => RefreshFleeButton();
            NavigationController.OnTabChanged -= OnTabChanged;
        }
    }
}
