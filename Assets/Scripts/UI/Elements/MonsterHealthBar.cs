using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
        private Canvas _canvas;
        private GameObject _fleeConfirmOverlay;

        private void Start()
        {
            HideSliderHandle();
            ApplyNameTextStyle();

            MonsterManager.Instance.OnMonsterSpawned += BindToMonster;
            MonsterManager.Instance.OnStageChanged   += UpdateStage;
            UpdateStage(MonsterManager.Instance.Stage);

            // MonsterManager.Start()가 먼저 실행돼 이미 몬스터가 있으면 즉시 바인딩
            if (MonsterManager.Instance.CurrentMonster != null)
                BindToMonster(MonsterManager.Instance.CurrentMonster);

            CurrencyManager.Instance.OnGoldChanged += OnGoldChangedHandler;
            NavigationController.OnTabChanged += OnTabChanged;

            CreateFleeButton();
            RefreshFleeButton();
        }

        private void ApplyNameTextStyle()
        {
            if (_nameText == null) return;
            _nameText.fontSize          = 32;
            _nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
            _nameText.margin            = new Vector4(-32f, 0f, -98.8f, 0f);
        }

        private void HideSliderHandle()
        {
            var slider = GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                slider.enabled = false;
                if (slider.handleRect != null)
                    slider.handleRect.gameObject.SetActive(false);
            }

            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "Handle Slide Area" || t.name == "Handle")
                    t.gameObject.SetActive(false);
            }
        }

        private void CreateFleeButton()
        {
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null) _canvas = FindAnyObjectByType<Canvas>();
            if (_canvas == null) return;

            GameObject btn = new GameObject("FleeButton");
            btn.transform.SetParent(_canvas.transform, false);

            RectTransform rt = btn.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1, 1);
            rt.anchorMax        = new Vector2(1, 1);
            rt.pivot            = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -150);
            rt.sizeDelta        = new Vector2(200, 90);

            Image bg = btn.AddComponent<Image>();
            bg.color = new Color(0.6f, 0.15f, 0.15f);

            _fleeButton = btn.AddComponent<Button>();
            _fleeButton.onClick.AddListener(ShowFleeConfirm);

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btn.transform, false);
            RectTransform lrt = labelObj.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            _fleeLabel = labelObj.AddComponent<TextMeshProUGUI>();
            _fleeLabel.alignment  = TextAlignmentOptions.Center;
            _fleeLabel.fontSize   = 28;
            _fleeLabel.color      = Color.white;
            _fleeLabel.raycastTarget = false;
        }

        private void OnTabChanged(int tabIndex)
        {
            bool isBattleTab = tabIndex == 2;
            if (_fleeButton != null)
                _fleeButton.gameObject.SetActive(isBattleTab);
            if (!isBattleTab) CloseFleeConfirm();
        }

        private void ShowFleeConfirm()
        {
            if (_fleeConfirmOverlay != null || _canvas == null) return;

            double cost = MonsterManager.Instance.FleeCost;

            _fleeConfirmOverlay = new GameObject("FleeConfirmOverlay");
            _fleeConfirmOverlay.transform.SetParent(_canvas.transform, false);
            var ort = _fleeConfirmOverlay.AddComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;
            _fleeConfirmOverlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            _fleeConfirmOverlay.AddComponent<Button>().onClick.AddListener(CloseFleeConfirm);

            var card = new GameObject("Card");
            card.transform.SetParent(_fleeConfirmOverlay.transform, false);
            var crt = card.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f); crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(480, 260);
            card.AddComponent<Image>().color = UITheme.BgConfirmCard;
            var et = card.AddComponent<EventTrigger>();
            var blockEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            blockEntry.callback.AddListener(_ => { });
            et.triggers.Add(blockEntry);

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 24, 24);
            vlg.spacing = 14;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            MakeFleeCardLabel(card.transform, "도망가기", 30, Color.white, 46);
            MakeFleeCardLabel(card.transform, $"도망 비용: {NumberFormatter.Format(cost)} 골드", 26,
                new Color(1f, 0.75f, 0.3f), 40);

            var btnRow = new GameObject("BtnRow");
            btnRow.transform.SetParent(card.transform, false);
            btnRow.AddComponent<LayoutElement>().preferredHeight = 72;
            var brhlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            brhlg.spacing = 14;
            brhlg.childControlWidth = true;
            brhlg.childControlHeight = true;
            brhlg.childForceExpandWidth = true;
            brhlg.childForceExpandHeight = true;

            UIHelper.MakeButton(btnRow.transform, "취소", 28, UITheme.BtnConfirmCancel)
                .GetComponent<Button>().onClick.AddListener(CloseFleeConfirm);
            UIHelper.MakeButton(btnRow.transform, "도망가기", 28, new Color(0.6f, 0.15f, 0.15f))
                .GetComponent<Button>().onClick.AddListener(() =>
                {
                    CloseFleeConfirm();
                    MonsterManager.Instance.Flee();
                    RefreshFleeButton();
                });
        }

        private void CloseFleeConfirm()
        {
            if (_fleeConfirmOverlay != null) { Destroy(_fleeConfirmOverlay); _fleeConfirmOverlay = null; }
        }

        private static void MakeFleeCardLabel(Transform parent, string text, int fontSize, Color color, int height)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = height;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
        }

        private void OnGoldChangedHandler(double _) => RefreshFleeButton();

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
            {
                // Inspector의 Image Type 설정과 무관하게 fillAmount가 동작하도록 강제 설정
                _fillImage.type        = Image.Type.Filled;
                _fillImage.fillMethod  = Image.FillMethod.Horizontal;
                _fillImage.fillOrigin  = (int)Image.OriginHorizontal.Left;
                _fillImage.color = monster.IsBoss ? new Color(0.9f, 0.2f, 0.05f) : new Color(0.2f, 0.8f, 0.2f);
            }
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
                CurrencyManager.Instance.OnGoldChanged -= OnGoldChangedHandler;
            NavigationController.OnTabChanged -= OnTabChanged;
            CloseFleeConfirm();
        }
    }
}
