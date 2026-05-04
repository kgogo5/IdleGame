using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;
using IdleGame.Data;
using IdleGame.Managers;

namespace IdleGame.UI.Panels
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("References (필수)")]
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private Button     settingsButton;

        [Header("색상")]
        [SerializeField] private Color contentBgColor = new Color(0.09f, 0.12f, 0.17f, 0.98f);
        [SerializeField] private Color titleColor     = new Color(0.95f, 0.97f, 1.00f, 1f);
        [SerializeField] private Color labelColor     = new Color(0.78f, 0.83f, 0.90f, 1f);
        [SerializeField] private Color dividerColor   = new Color(1.00f, 1.00f, 1.00f, 0.07f);
        [SerializeField] private Color onColor        = new Color(0.18f, 0.76f, 0.40f, 1f);
        [SerializeField] private Color offColor       = new Color(0.22f, 0.24f, 0.30f, 1f);

        [Header("닫기 버튼")]
        [SerializeField] private float closeButtonHeight = 80f;
        [SerializeField] private Color closeBtnNormal    = new Color(0.72f, 0.16f, 0.16f, 1f);
        [SerializeField] private Color closeBtnHighlight = new Color(0.88f, 0.26f, 0.26f, 1f);
        [SerializeField] private Color closeBtnPressed   = new Color(0.52f, 0.09f, 0.09f, 1f);

        [Header("행")]
        [SerializeField] private float rowHeight   = 88f;
        [SerializeField] private float titleHeight = 80f;

        private const float ToggleW    = 108f;
        private const float ToggleH    = 50f;
        private const float KnobSize   = 42f;
        private const float KnobOffset = 27f;

        private static Sprite _pillSprite;
        private static Sprite _knobSprite;
        private static Sprite PillSprite => _pillSprite != null ? _pillSprite : (_pillSprite = MakePill((int)ToggleW, (int)ToggleH));
        private static Sprite KnobSprite => _knobSprite != null ? _knobSprite : (_knobSprite = MakeCircle(64));

        private bool            _resetPending;
        private Button          _resetBtn;
        private TextMeshProUGUI _resetBtnTmp;

        private GameObject _settingsPage;
        private GameObject _adminPage;
        private Image      _settingsTabImg;
        private Image      _adminTabImg;

        private ItemRarity _selectedRarity = ItemRarity.Normal;
        private readonly Dictionary<ItemRarity, Image> _rarityImgs = new();

        private void Start()
        {
            settingsButton.onClick.AddListener(OpenSettings);
            SetupOverlayClose();
            ApplyStyles();
            settingsPopup.SetActive(false);
        }

        private void SetupOverlayClose()
        {
            Image overlayImg = settingsPopup.GetComponent<Image>()
                            ?? settingsPopup.AddComponent<Image>();
            overlayImg.color         = new Color(0.09f, 0.12f, 0.17f, 1f);
            overlayImg.raycastTarget = true;

            var oldBtn = settingsPopup.GetComponent<Button>();
            if (oldBtn != null) Destroy(oldBtn);

            var et = settingsPopup.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (et != null) et.triggers.Clear();
        }

        private void ApplyStyles()
        {
            if (settingsPopup == null) return;
            Transform content = settingsPopup.transform.Find("SettingsContent");
            if (content == null) return;

            // settingsPopup 전체화면
            var popupRt = settingsPopup.GetComponent<RectTransform>();
            if (popupRt != null)
            {
                popupRt.anchorMin = Vector2.zero;
                popupRt.anchorMax = Vector2.one;
                popupRt.offsetMin = Vector2.zero;
                popupRt.offsetMax = Vector2.zero;
            }

            // SettingsContent 전체화면
            var contentRt = content.GetComponent<RectTransform>();
            if (contentRt != null)
            {
                contentRt.anchorMin = Vector2.zero;
                contentRt.anchorMax = Vector2.one;
                contentRt.offsetMin = Vector2.zero;
                contentRt.offsetMax = Vector2.zero;
            }

            var bg = content.GetComponent<Image>();
            if (bg != null) bg.color = contentBgColor;

            // CanvasGroup alpha 강제 1로 (씬에서 설정된 값 무시)
            var cg = settingsPopup.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;

            // 불투명 배경 패널 — settingsPopup 자식 중 가장 아래(뒤)에 배치
            if (settingsPopup.transform.Find("_BgBlocker") == null)
            {
                var blockerGo = new GameObject("_BgBlocker");
                blockerGo.transform.SetParent(settingsPopup.transform, false);
                blockerGo.transform.SetAsFirstSibling();
                var bRt = blockerGo.AddComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero;
                bRt.anchorMax = Vector2.one;
                bRt.offsetMin = Vector2.zero;
                bRt.offsetMax = Vector2.zero;
                var bImg = blockerGo.AddComponent<Image>();
                bImg.color = new Color(0.09f, 0.12f, 0.17f, 1f);
                bImg.raycastTarget = false;
            }

            var vg = content.GetComponent<VerticalLayoutGroup>()
                  ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            vg.childForceExpandHeight = false;
            vg.childControlHeight     = true;
            vg.childForceExpandWidth  = true;
            vg.childControlWidth      = true;
            vg.childAlignment         = TextAnchor.UpperCenter;
            vg.spacing = 0f;
            vg.padding = new RectOffset(0, 0, 0, 0);

            BuildTitle(content);
            BuildTabBar(content);
            BuildSettingsPage(content);  // 씬 오브젝트 흡수 포함
            BuildAdminPage(content);
            SetupCloseButton(content);

            // 닫기 버튼 항상 마지막
            content.Find("CloseButton")?.SetAsLastSibling();

            ShowTab(isSettingsTab: true);
        }

        // ── 유틸 ────────────────────────────────────────────────────────────────

        private void MakeDivider(Transform parent, string name)
        {
            if (parent.Find(name) != null) return;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 1f;
            var img = go.AddComponent<Image>();
            img.color = dividerColor;
            img.raycastTarget = false;
        }

        // ── 제목 ────────────────────────────────────────────────────────────────

        private void BuildTitle(Transform content)
        {
            if (content.Find("_Title") != null) return;

            var go = new GameObject("_Title");
            go.transform.SetParent(content, false);
            go.transform.SetSiblingIndex(0);
            go.AddComponent<LayoutElement>().preferredHeight = titleHeight;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "설  정"; tmp.color = titleColor;
            tmp.fontSize = 32f; tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center; tmp.raycastTarget = false;

            var div = new GameObject("_DivTitle");
            div.transform.SetParent(content, false);
            div.transform.SetSiblingIndex(1);
            div.AddComponent<LayoutElement>().preferredHeight = 1f;
            var di = div.AddComponent<Image>();
            di.color = dividerColor; di.raycastTarget = false;
        }

        // ── 탭 바 ───────────────────────────────────────────────────────────────

        private void BuildTabBar(Transform content)
        {
            if (content.Find("_TabBar") != null) return;

            var bar = new GameObject("_TabBar");
            bar.transform.SetParent(content, false);
            var le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = 68f; le.flexibleHeight = 0f;
            var hlg = bar.AddComponent<HorizontalLayoutGroup>();
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;     hlg.childControlHeight = true;
            hlg.spacing = 4f; hlg.padding = new RectOffset(8, 8, 6, 6);

            _settingsTabImg = MakeTabBtn(bar.transform, "설  정", () => ShowTab(true));
            _adminTabImg    = MakeTabBtn(bar.transform, "개발자", () => ShowTab(false));

            MakeDivider(content, "_DivTabs");
        }

        private Image MakeTabBtn(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Tab");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.18f, 0.25f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var tgo = new GameObject("L");
            tgo.transform.SetParent(go.transform, false);
            var rt = tgo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 26f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return img;
        }

        private void ShowTab(bool isSettingsTab)
        {
            _settingsPage?.SetActive(isSettingsTab);
            _adminPage?.SetActive(!isSettingsTab);

            var active   = new Color(0.20f, 0.50f, 0.80f);
            var inactive = new Color(0.15f, 0.18f, 0.25f);
            if (_settingsTabImg != null) _settingsTabImg.color = isSettingsTab  ? active : inactive;
            if (_adminTabImg    != null) _adminTabImg.color    = !isSettingsTab ? active : inactive;
        }

        // ── 설정 페이지 ───────────────────────────────────────────────────────────

        private void BuildSettingsPage(Transform content)
        {
            if (content.Find("_SettingsPage") != null)
            {
                _settingsPage = content.Find("_SettingsPage").gameObject;
                return;
            }

            var page = new GameObject("_SettingsPage");
            page.transform.SetParent(content, false);
            _settingsPage = page;

            var vlg = page.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false; vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;  vlg.childControlWidth  = true;
            vlg.childAlignment = TextAnchor.UpperCenter; vlg.spacing = 0f;
            page.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // 씬에 이미 있는 행을 이 페이지 안으로 이동
            MoveToPage(content, page.transform, "BGMToggle");
            MoveToPage(content, page.transform, "SoundToggle");
            MoveToPage(content, page.transform, "VibrationToggle");
            MoveToPage(content, page.transform, "NotificationToggle");

            // 볼륨 슬라이더 행
            SetupVolumeRow(page.transform, "BGMToggle",   "배경음악 볼륨",
                () => SettingsManager.Instance.BgmVolume,
                v  => SettingsManager.Instance.SetBgmVolume(v));
            SetupVolumeRow(page.transform, "SoundToggle", "효과음 볼륨",
                () => SettingsManager.Instance.SfxVolume,
                v  => SettingsManager.Instance.SetSfxVolume(v));

            // 토글 행
            SetupToggleRow(page.transform, "VibrationToggle",    "진동",
                () => SettingsManager.Instance.VibrationEnabled,    v => SettingsManager.Instance.SetVibration(v));
            SetupToggleRow(page.transform, "NotificationToggle", "알림",
                () => SettingsManager.Instance.NotificationEnabled, v => SettingsManager.Instance.SetNotification(v));

            // 초기화 버튼
            BuildResetButton(page.transform);

            var sp = new GameObject("_Sp"); sp.transform.SetParent(page.transform, false);
            sp.AddComponent<LayoutElement>().flexibleHeight = 1f;
        }

        private static void MoveToPage(Transform from, Transform to, string name)
        {
            var t = from.Find(name);
            if (t != null) t.SetParent(to, false);
        }

        // ── 볼륨 슬라이더 행 ─────────────────────────────────────────────────────

        private void SetupVolumeRow(Transform page, string rowName, string label,
            Func<float> getVolume, Action<float> setVolume)
        {
            Transform row = page.Find(rowName);
            if (row == null)
            {
                var rowGo = new GameObject(rowName);
                rowGo.transform.SetParent(page, false);
                rowGo.AddComponent<TextMeshProUGUI>();
                row = rowGo.transform;
            }

            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight; le.flexibleHeight = 0f;

            var labelTmp = row.GetComponent<TextMeshProUGUI>();
            if (labelTmp != null)
            {
                labelTmp.text = label; labelTmp.color = labelColor;
                labelTmp.fontSize = 24f; labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.margin = new Vector4(28f, 0f, 200f, 0f); labelTmp.raycastTarget = false;
            }

            MakeDivider(page, $"_Div{rowName}");

            var slider = BuildVolumeSlider(row);
            slider.value = getVolume();
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => setVolume(v));
        }

        private Slider BuildVolumeSlider(Transform row)
        {
            var existing = row.Find("_VolumeSlider");
            if (existing != null) return existing.GetComponent<Slider>();

            var sliderGo = new GameObject("_VolumeSlider");
            sliderGo.transform.SetParent(row, false);
            var rt = sliderGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f); rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-20f, 0f); rt.sizeDelta = new Vector2(170f, 44f);

            var bg = new GameObject("Background");
            bg.transform.SetParent(sliderGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.3f); bgRt.anchorMax = new Vector2(1, 0.7f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.3f); faRt.anchorMax = new Vector2(1, 0.7f);
            faRt.offsetMin = new Vector2(4, 0); faRt.offsetMax = new Vector2(-12, 0);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            var fillImg = fill.AddComponent<Image>(); fillImg.color = onColor;

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGo.transform, false);
            var haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(8, 0); haRt.offsetMax = new Vector2(-8, 0);
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var hRt = handle.AddComponent<RectTransform>(); hRt.sizeDelta = new Vector2(22f, 22f);
            var handleImg = handle.AddComponent<Image>();
            handleImg.sprite = KnobSprite; handleImg.color = Color.white;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRt; slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f; slider.maxValue = 1f;
            return slider;
        }

        // ── 토글 행 ──────────────────────────────────────────────────────────────

        private void SetupToggleRow(Transform page, string rowName, string label,
            Func<bool> getState, Action<bool> setState)
        {
            Transform row = page.Find(rowName);
            if (row == null)
            {
                var rowGo = new GameObject(rowName);
                rowGo.transform.SetParent(page, false);
                rowGo.AddComponent<TextMeshProUGUI>();
                row = rowGo.transform;
            }

            var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight; le.flexibleHeight = 0f;

            var labelTmp = row.GetComponent<TextMeshProUGUI>();
            if (labelTmp != null)
            {
                labelTmp.text = label; labelTmp.color = labelColor;
                labelTmp.fontSize = 28f; labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.margin = new Vector4(28f, 0f, ToggleW + 28f, 0f);
                labelTmp.raycastTarget = true;
            }

            BuildToggle(row, out Image pillImg, out RectTransform knobRect);
            MakeDivider(page, $"_Div{rowName}");

            var rowBtn = row.GetComponent<Button>() ?? row.gameObject.AddComponent<Button>();
            rowBtn.transition = Selectable.Transition.None;
            if (labelTmp != null) rowBtn.targetGraphic = labelTmp;

            void Refresh(bool state)
            {
                pillImg.color             = state ? onColor  : offColor;
                knobRect.anchoredPosition = new Vector2(state ? KnobOffset : -KnobOffset, 0f);
            }
            Refresh(getState());
            rowBtn.onClick.RemoveAllListeners();
            rowBtn.onClick.AddListener(() => { bool next = !getState(); setState(next); Refresh(next); });
        }

        private void BuildToggle(Transform row, out Image pillImg, out RectTransform knobRect)
        {
            var toggleTf = row.Find("_Toggle");
            if (toggleTf == null)
            {
                var go = new GameObject("_Toggle"); go.transform.SetParent(row, false);
                pillImg = go.AddComponent<Image>(); toggleTf = go.transform;
            }
            else pillImg = toggleTf.GetComponent<Image>() ?? toggleTf.gameObject.AddComponent<Image>();

            pillImg.sprite = PillSprite; pillImg.raycastTarget = false;
            var pillRect = (RectTransform)toggleTf;
            pillRect.anchorMin = new Vector2(1f, 0.5f); pillRect.anchorMax = new Vector2(1f, 0.5f);
            pillRect.pivot = new Vector2(1f, 0.5f);
            pillRect.anchoredPosition = new Vector2(-20f, 0f);
            pillRect.sizeDelta = new Vector2(ToggleW, ToggleH);

            var knobTf = toggleTf.Find("_Knob");
            Image knobImg;
            if (knobTf == null)
            {
                var kgo = new GameObject("_Knob"); kgo.transform.SetParent(toggleTf, false);
                knobImg = kgo.AddComponent<Image>(); knobTf = kgo.transform;
                knobRect = (RectTransform)knobTf;
            }
            else
            {
                knobImg  = knobTf.GetComponent<Image>() ?? knobTf.gameObject.AddComponent<Image>();
                knobRect = (RectTransform)knobTf;
            }
            knobImg.sprite = KnobSprite; knobImg.color = Color.white; knobImg.raycastTarget = false;
            knobRect.anchorMin = new Vector2(0.5f, 0.5f); knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f); knobRect.sizeDelta = new Vector2(KnobSize, KnobSize);
        }

        // ── 초기화 버튼 ───────────────────────────────────────────────────────────

        private void BuildResetButton(Transform parent)
        {
            if (parent.Find("_ResetBtn") != null) return;

            var go = new GameObject("_ResetBtn"); go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight; le.flexibleHeight = 0f;
            var bg = go.AddComponent<Image>(); bg.color = new Color(0.55f, 0.15f, 0.15f);
            _resetBtn = go.AddComponent<Button>(); _resetBtn.targetGraphic = bg;
            ColorBlock cb = _resetBtn.colors;
            cb.normalColor = new Color(0.55f, 0.15f, 0.15f);
            cb.highlightedColor = new Color(0.75f, 0.2f, 0.2f);
            cb.pressedColor = new Color(0.35f, 0.1f, 0.1f); cb.colorMultiplier = 1f;
            _resetBtn.colors = cb; _resetBtn.onClick.AddListener(OnResetClicked);

            var tgo = new GameObject("L"); tgo.transform.SetParent(go.transform, false);
            var rt = tgo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            _resetBtnTmp = tgo.AddComponent<TextMeshProUGUI>();
            _resetBtnTmp.text = "데이터 초기화"; _resetBtnTmp.fontSize = 28f;
            _resetBtnTmp.fontStyle = FontStyles.Bold; _resetBtnTmp.color = Color.white;
            _resetBtnTmp.alignment = TextAlignmentOptions.Center; _resetBtnTmp.raycastTarget = false;
        }

        private void OnResetClicked()
        {
            if (!_resetPending)
            {
                _resetPending = true;
                if (_resetBtnTmp != null) _resetBtnTmp.text = "한번 더 누르면 초기화됩니다!";
                Invoke(nameof(CancelReset), 4f);
            }
            else
            {
                CancelInvoke(nameof(CancelReset));
                ExecuteReset();
            }
        }

        private void CancelReset()
        {
            _resetPending = false;
            if (_resetBtnTmp != null) _resetBtnTmp.text = "데이터 초기화";
        }

        private void ExecuteReset()
        {
            _resetPending = false;
            PlayerStats.Instance?.ResetBonuses();
            CurrencyManager.Instance?.ResetData();
            UpgradeManager.Instance?.ResetData();
            InventoryManager.Instance?.ResetData();
            MonsterManager.Instance?.ResetData();
            PlayerPrefs.DeleteAll(); PlayerPrefs.Save();
            if (_resetBtnTmp != null) _resetBtnTmp.text = "데이터 초기화";
            CloseSettings();
        }

        // ── 어드민 페이지 ────────────────────────────────────────────────────────

        private void BuildAdminPage(Transform content)
        {
            if (content.Find("_AdminPage") != null)
            {
                _adminPage = content.Find("_AdminPage").gameObject;
                return;
            }

            var page = new GameObject("_AdminPage");
            page.transform.SetParent(content, false);
            _adminPage = page;

            var vlg = page.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false; vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;  vlg.childControlWidth  = true;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.spacing = 8f; vlg.padding = new RectOffset(14, 14, 14, 14);
            page.AddComponent<LayoutElement>().flexibleHeight = 1f;

            AddAdminLabel(page.transform, "재화");
            AddTwoButtons(page.transform,
                "골드 ×2",   new Color(0.20f, 0.50f, 0.18f), OnGoldDouble,
                "보석 +100", new Color(0.28f, 0.18f, 0.58f), OnJewelAdd);

            AddAdminDivider(page.transform);

            AddAdminLabel(page.transform, "아이템 지급 (등급 선택)");
            AddRaritySelector(page.transform);
            AddAdminButton(page.transform, "랜덤 아이템 지급", new Color(0.18f, 0.38f, 0.60f),
                OnGiveRandomItem);

            AddAdminDivider(page.transform);

            AddAdminLabel(page.transform, "치트");
            AddAdminButton(page.transform, "업그레이드 모두 최대", new Color(0.18f, 0.48f, 0.18f),
                () => UpgradeManager.Instance?.MaxAllUpgrades());
            AddAdminButton(page.transform, "아이템 모두 지급", new Color(0.18f, 0.38f, 0.55f),
                () => InventoryManager.Instance?.GiveAllItems());

            var sp = new GameObject("_Sp"); sp.transform.SetParent(page.transform, false);
            sp.AddComponent<LayoutElement>().flexibleHeight = 1f;
        }

        private void OnGoldDouble()
            => CurrencyManager.Instance?.AddGoldRaw(CurrencyManager.Instance.Gold);
        private void OnJewelAdd()
            => CurrencyManager.Instance?.AddJewel(100);
        private void OnGiveRandomItem()
            => InventoryManager.Instance?.GiveRandomItem(_selectedRarity);

        private void AddAdminLabel(Transform p, string text)
        {
            var go = new GameObject("Lbl"); go.transform.SetParent(p, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 34f; le.flexibleHeight = 0f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 21f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.8f, 0.8f, 0.4f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft; tmp.raycastTarget = false;
        }

        private void AddAdminDivider(Transform p)
        {
            var go = new GameObject("Div"); go.transform.SetParent(p, false);
            go.AddComponent<LayoutElement>().preferredHeight = 1f;
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.1f); img.raycastTarget = false;
        }

        private void AddAdminButton(Transform p, string label, Color color, Action onClick)
        {
            var go = new GameObject("Btn"); go.transform.SetParent(p, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 68f; le.flexibleHeight = 0f;
            var bg = go.AddComponent<Image>(); bg.color = color;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());

            var tgo = new GameObject("L"); tgo.transform.SetParent(go.transform, false);
            var rt = tgo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 24f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private void AddTwoButtons(Transform p,
            string la, Color ca, Action oa, string lb, Color cb2, Action ob)
        {
            var row = new GameObject("Row2"); row.transform.SetParent(p, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 68f; le.flexibleHeight = 0f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;     hlg.childControlHeight = true;
            AddInlineBtn(row.transform, la, ca, oa);
            AddInlineBtn(row.transform, lb, cb2, ob);
        }

        private void AddInlineBtn(Transform p, string label, Color color, Action onClick)
        {
            var go = new GameObject("IBtn"); go.transform.SetParent(p, false);
            go.AddComponent<RectTransform>();
            var bg = go.AddComponent<Image>(); bg.color = color;
            var btn = go.AddComponent<Button>(); btn.targetGraphic = bg;
            btn.onClick.AddListener(() => onClick());

            var tgo = new GameObject("L"); tgo.transform.SetParent(go.transform, false);
            var rt = tgo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = label; tmp.fontSize = 22f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        private void AddRaritySelector(Transform p)
        {
            var row = new GameObject("RarityRow"); row.transform.SetParent(p, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 60f; le.flexibleHeight = 0f;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;     hlg.childControlHeight = true;

            _rarityImgs.Clear();
            foreach (ItemRarity r in Enum.GetValues(typeof(ItemRarity)))
            {
                var rarity = r;
                Color full = r.ToColor();
                Color dim  = Color.Lerp(full, new Color(0.12f, 0.12f, 0.16f), 0.65f);

                var go = new GameObject("R"); go.transform.SetParent(row.transform, false);
                go.AddComponent<RectTransform>();
                var img = go.AddComponent<Image>(); img.color = dim;
                var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
                btn.onClick.AddListener(() => SelectRarity(rarity));

                var tgo = new GameObject("L"); tgo.transform.SetParent(go.transform, false);
                var rt = tgo.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                var tmp = tgo.AddComponent<TextMeshProUGUI>();
                tmp.text = r.ToKorean(); tmp.fontSize = 19f; tmp.fontStyle = FontStyles.Bold;
                tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;
                _rarityImgs[r] = img;
            }
            SelectRarity(ItemRarity.Normal);
        }

        private void SelectRarity(ItemRarity rarity)
        {
            _selectedRarity = rarity;
            foreach (var kv in _rarityImgs)
            {
                Color full = kv.Key.ToColor();
                Color dim  = Color.Lerp(full, new Color(0.12f, 0.12f, 0.16f), 0.65f);
                kv.Value.color = kv.Key == rarity ? full : dim;
            }
        }

        // ── 닫기 버튼 ────────────────────────────────────────────────────────────

        private void SetupCloseButton(Transform content)
        {
            var closeBtn = content.Find("CloseButton");
            if (closeBtn == null) return;

            var le = closeBtn.GetComponent<LayoutElement>()
                  ?? closeBtn.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = closeButtonHeight; le.flexibleHeight = 0f;

            var btn = closeBtn.GetComponent<Button>();
            var closeBg = closeBtn.GetComponent<Image>();
            if (btn != null)
            {
                if (closeBg != null) btn.targetGraphic = closeBg;
                ColorBlock cb = btn.colors;
                cb.normalColor = closeBtnNormal; cb.highlightedColor = closeBtnHighlight;
                cb.pressedColor = closeBtnPressed; cb.colorMultiplier = 1f;
                btn.colors = cb;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(CloseSettings);
            }

            var closeTmp = closeBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeTmp != null)
            {
                closeTmp.text = "닫  기"; closeTmp.color = Color.white;
                closeTmp.fontSize = 28f; closeTmp.fontStyle = FontStyles.Bold;
                closeTmp.alignment = TextAlignmentOptions.Center; closeTmp.raycastTarget = false;
            }
        }

        // ── 팝업 열기/닫기 ───────────────────────────────────────────────────────

        public void OpenSettings()
        {
            settingsPopup.transform.SetAsLastSibling();
            settingsPopup.SetActive(true);
            Time.timeScale = 0f;
        }

        public void CloseSettings()
        {
            settingsPopup.SetActive(false);
            Time.timeScale = 1f;
        }

        // ── 스프라이트 생성 ──────────────────────────────────────────────────────

        private static Sprite MakePill(int w, int h)
        {
            float r = h / 2f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px  = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01(RoundRectSdf(x + .5f, y + .5f, w, h, r) + .5f));
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f));
        }

        private static Sprite MakeCircle(int size)
        {
            float r = size / 2f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px  = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + .5f - r, dy = y + .5f - r;
                    px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + .5f));
                }
            tex.SetPixels(px); tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }

        private static float RoundRectSdf(float x, float y, float w, float h, float r)
        {
            float cx = Mathf.Clamp(x, r, w - r), cy = Mathf.Clamp(y, r, h - r);
            float dx = x - cx, dy = y - cy;
            return r - Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
