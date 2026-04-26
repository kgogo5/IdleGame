using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Managers;

namespace IdleGame.UI
{
    public class SettingsPanel : MonoBehaviour
    {
        [Header("References (필수)")]
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private Button     settingsButton;

        [Header("팝업")]
        [SerializeField] private Color contentBgColor = new Color(0.09f, 0.12f, 0.17f, 0.98f);
        [SerializeField] private Color titleColor     = new Color(0.95f, 0.97f, 1.00f, 1f);
        [SerializeField] private Color labelColor     = new Color(0.78f, 0.83f, 0.90f, 1f);
        [SerializeField] private Color dividerColor   = new Color(1.00f, 1.00f, 1.00f, 0.07f);

        [Header("토글")]
        [SerializeField] private Color onColor  = new Color(0.18f, 0.76f, 0.40f, 1f);
        [SerializeField] private Color offColor = new Color(0.22f, 0.24f, 0.30f, 1f);

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

        private void Start()
        {
            settingsButton.onClick.AddListener(OpenSettings);
            SetupOverlayClose();
            ApplyStyles();
        }

        // 전체화면 오버레이 — 자식 이벤트 버블링 문제를 피하기 위해 Button 대신 EventTrigger 사용
        private void SetupOverlayClose()
        {
            Image overlayImg = settingsPopup.GetComponent<Image>();
            if (overlayImg == null)
                overlayImg = settingsPopup.AddComponent<Image>();

            overlayImg.color         = new Color(0f, 0f, 0f, 0.88f);
            overlayImg.raycastTarget = true;

            // Button을 쓰면 자식(슬라이더 등)의 이벤트가 버블링되어 오버레이에 전달됨 → 제거
            Button oldBtn = settingsPopup.GetComponent<Button>();
            if (oldBtn != null) Destroy(oldBtn);

            // EventTrigger: 실제로 오버레이 자체를 클릭했을 때만 닫기
            var et = settingsPopup.GetComponent<UnityEngine.EventSystems.EventTrigger>()
                  ?? settingsPopup.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            et.triggers.Clear();

            var entry = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick
            };
            entry.callback.AddListener(data =>
            {
                var e = (UnityEngine.EventSystems.PointerEventData)data;
                // raycast 원본 hit이 오버레이 자체일 때만 닫기 (자식 이벤트 버블링 무시)
                if (e.pointerCurrentRaycast.gameObject == settingsPopup)
                    CloseSettings();
            });
            et.triggers.Add(entry);
        }

        private void ApplyStyles()
        {
            if (settingsPopup == null) return;
            Transform content = settingsPopup.transform.Find("SettingsContent");
            if (content == null) return;

            Image bg = content.GetComponent<Image>();
            if (bg != null) bg.color = contentBgColor;

            VerticalLayoutGroup vg = content.GetComponent<VerticalLayoutGroup>()
                ?? content.gameObject.AddComponent<VerticalLayoutGroup>();
            vg.childForceExpandHeight = false;
            vg.childControlHeight     = true;
            vg.childForceExpandWidth  = true;
            vg.childControlWidth      = true;
            vg.childAlignment         = TextAnchor.UpperCenter;
            vg.spacing = 0f;
            vg.padding = new RectOffset(0, 0, 0, 0);

            EnsureTitle(content);

            // 볼륨 슬라이더 행
            SetupVolumeRow(content, "BGMToggle", "배경음악 볼륨",
                () => SettingsManager.Instance.BgmVolume,
                v  => SettingsManager.Instance.SetBgmVolume(v));
            SetupVolumeRow(content, "SoundToggle", "효과음 볼륨",
                () => SettingsManager.Instance.SfxVolume,
                v  => SettingsManager.Instance.SetSfxVolume(v));

            // 토글 행
            SetupToggleRow(content, "VibrationToggle",    "진동",
                () => SettingsManager.Instance.VibrationEnabled,    v => SettingsManager.Instance.SetVibration(v));
            SetupToggleRow(content, "NotificationToggle", "알림",
                () => SettingsManager.Instance.NotificationEnabled, v => SettingsManager.Instance.SetNotification(v));

            SetupCloseButton(content);
        }

        private void EnsureTitle(Transform content)
        {
            if (content.Find("_Title") != null) return;

            GameObject titleGo = new GameObject("_Title");
            titleGo.transform.SetParent(content, false);
            titleGo.transform.SetSiblingIndex(0);
            titleGo.AddComponent<LayoutElement>().preferredHeight = titleHeight;

            TextMeshProUGUI tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "설  정";
            tmp.color     = titleColor;
            tmp.fontSize  = 32f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            MakeDivider(content, "_DivTitle", 1);
        }

        private void MakeDivider(Transform parent, string name, int siblingIndex)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(siblingIndex);
            go.AddComponent<LayoutElement>().preferredHeight = 1f;
            Image img = go.AddComponent<Image>();
            img.color = dividerColor;
            img.raycastTarget = false;
        }

        // 볼륨 슬라이더 행
        private void SetupVolumeRow(Transform content, string rowName, string label,
            Func<float> getVolume, Action<float> setVolume)
        {
            Transform row = content.Find(rowName);
            if (row == null) return;

            LayoutElement le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.flexibleHeight  = 0f;

            TextMeshProUGUI labelTmp = row.GetComponent<TextMeshProUGUI>();
            if (labelTmp != null)
            {
                labelTmp.text      = label;
                labelTmp.color     = labelColor;
                labelTmp.fontSize  = 24f;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.margin    = new Vector4(28f, 0f, 200f, 0f);
                labelTmp.raycastTarget = false;
            }

            string divName = $"_Div{rowName}";
            if (content.Find(divName) == null)
                MakeDivider(content, divName, row.GetSiblingIndex() + 1);

            // 슬라이더 위젯
            Slider slider = BuildVolumeSlider(row);
            slider.value = getVolume();
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => setVolume(v));
        }

        private Slider BuildVolumeSlider(Transform row)
        {
            Transform existing = row.Find("_VolumeSlider");
            if (existing != null) return existing.GetComponent<Slider>();

            GameObject sliderGo = new GameObject("_VolumeSlider");
            sliderGo.transform.SetParent(row, false);

            RectTransform rt = sliderGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot     = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-20f, 0f);
            rt.sizeDelta        = new Vector2(170f, 44f);

            // Background track
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.3f);
            bgRt.anchorMax = new Vector2(1, 0.7f);
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.22f, 0.28f, 1f);
            bgImg.raycastTarget = true; // true여야 슬라이더 GO까지 버블링되어 정상 작동

            // Fill area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.3f);
            faRt.anchorMax = new Vector2(1, 0.7f);
            faRt.offsetMin = new Vector2(4, 0);
            faRt.offsetMax = new Vector2(-12, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRt = fill.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = onColor;
            fillImg.raycastTarget = true; // 버블링 경로 확보

            // Handle area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGo.transform, false);
            RectTransform haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(8, 0);
            haRt.offsetMax = new Vector2(-8, 0);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform hRt = handle.AddComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(22f, 22f);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.sprite = KnobSprite;
            handleImg.color  = Color.white;

            Slider slider = sliderGo.AddComponent<Slider>();
            slider.fillRect   = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleImg;
            slider.direction  = Slider.Direction.LeftToRight;
            slider.minValue   = 0f;
            slider.maxValue   = 1f;
            return slider;
        }

        // 토글 행 (진동/알림)
        private void SetupToggleRow(Transform content, string rowName, string label,
            Func<bool> getState, Action<bool> setState)
        {
            Transform row = content.Find(rowName);
            if (row == null) return;

            LayoutElement le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.flexibleHeight  = 0f;

            TextMeshProUGUI labelTmp = row.GetComponent<TextMeshProUGUI>();
            if (labelTmp != null)
            {
                labelTmp.text      = label;
                labelTmp.color     = labelColor;
                labelTmp.fontSize  = 28f;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.margin    = new Vector4(28f, 0f, ToggleW + 28f, 0f);
                labelTmp.raycastTarget = true;
            }

            BuildToggle(row, out Image pillImg, out RectTransform knobRect);

            string divName = $"_Div{rowName}";
            if (content.Find(divName) == null)
                MakeDivider(content, divName, row.GetSiblingIndex() + 1);

            Button rowBtn = row.GetComponent<Button>() ?? row.gameObject.AddComponent<Button>();
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
            Transform toggleTf = row.Find("_Toggle");
            if (toggleTf == null)
            {
                GameObject go = new GameObject("_Toggle");
                go.transform.SetParent(row, false);
                pillImg  = go.AddComponent<Image>();
                toggleTf = go.transform;
            }
            else
            {
                pillImg = toggleTf.GetComponent<Image>() ?? toggleTf.gameObject.AddComponent<Image>();
            }

            pillImg.sprite = PillSprite;
            pillImg.raycastTarget = false;

            RectTransform pillRect = (RectTransform)toggleTf;
            pillRect.anchorMin        = new Vector2(1f, 0.5f);
            pillRect.anchorMax        = new Vector2(1f, 0.5f);
            pillRect.pivot            = new Vector2(1f, 0.5f);
            pillRect.anchoredPosition = new Vector2(-20f, 0f);
            pillRect.sizeDelta        = new Vector2(ToggleW, ToggleH);

            Transform knobTf = toggleTf.Find("_Knob");
            Image knobImg;
            if (knobTf == null)
            {
                GameObject kgo = new GameObject("_Knob");
                kgo.transform.SetParent(toggleTf, false);
                knobImg  = kgo.AddComponent<Image>();
                knobTf   = kgo.transform;
                knobRect = (RectTransform)knobTf;
            }
            else
            {
                knobImg  = knobTf.GetComponent<Image>() ?? knobTf.gameObject.AddComponent<Image>();
                knobRect = (RectTransform)knobTf;
            }

            knobImg.sprite = KnobSprite;
            knobImg.color  = Color.white;
            knobImg.raycastTarget = false;

            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot     = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = new Vector2(KnobSize, KnobSize);
        }

        private void SetupCloseButton(Transform content)
        {
            Transform closeBtn = content.Find("CloseButton");
            if (closeBtn == null) return;

            if (content.Find("_Spacer") == null)
            {
                int idx = closeBtn.GetSiblingIndex();
                GameObject spacer = new GameObject("_Spacer");
                spacer.transform.SetParent(content, false);
                spacer.AddComponent<LayoutElement>().flexibleHeight = 1f;
                spacer.transform.SetSiblingIndex(idx);
            }

            LayoutElement cle = closeBtn.GetComponent<LayoutElement>()
                ?? closeBtn.gameObject.AddComponent<LayoutElement>();
            cle.preferredHeight = closeButtonHeight;
            cle.flexibleHeight  = 0f;

            Image closeBg = closeBtn.GetComponent<Image>();
            if (closeBg != null) closeBg.color = Color.white;

            Button btn = closeBtn.GetComponent<Button>();
            if (btn != null)
            {
                if (closeBg != null) btn.targetGraphic = closeBg;
                ColorBlock cb       = btn.colors;
                cb.normalColor      = closeBtnNormal;
                cb.highlightedColor = closeBtnHighlight;
                cb.pressedColor     = closeBtnPressed;
                cb.colorMultiplier  = 1f;
                btn.colors = cb;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(CloseSettings);
            }

            TextMeshProUGUI closeTmp = closeBtn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (closeTmp != null)
            {
                closeTmp.text      = "닫  기";
                closeTmp.color     = Color.white;
                closeTmp.fontSize  = 28f;
                closeTmp.fontStyle = FontStyles.Bold;
                closeTmp.alignment = TextAlignmentOptions.Center;
                closeTmp.raycastTarget = false;
            }
        }

        public void OpenSettings()
        {
            settingsPopup.SetActive(true);
            Time.timeScale = 0f;
        }

        public void CloseSettings()
        {
            settingsPopup.SetActive(false);
            Time.timeScale = 1f;
        }

        private static Sprite MakePill(int w, int h)
        {
            float r = h / 2f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px  = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01(RoundRectSdf(x + .5f, y + .5f, w, h, r) + .5f));
            tex.SetPixels(px);
            tex.Apply();
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
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
        }

        private static float RoundRectSdf(float x, float y, float w, float h, float r)
        {
            float cx = Mathf.Clamp(x, r, w - r);
            float cy = Mathf.Clamp(y, r, h - r);
            float dx = x - cx, dy = y - cy;
            return r - Mathf.Sqrt(dx * dx + dy * dy);
        }
    }
}
