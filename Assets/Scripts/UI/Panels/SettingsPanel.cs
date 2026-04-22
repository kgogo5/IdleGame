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
        private const float KnobOffset = 27f; // (ToggleW/2) - (KnobSize/2) - 6px padding

        private static Sprite _pillSprite;
        private static Sprite _knobSprite;
        private static Sprite PillSprite => _pillSprite != null ? _pillSprite : (_pillSprite = MakePill((int)ToggleW, (int)ToggleH));
        private static Sprite KnobSprite => _knobSprite != null ? _knobSprite : (_knobSprite = MakeCircle(64));

        private void Start()
        {
            settingsButton.onClick.AddListener(OpenSettings);
            SetupDimClose();
            ApplyStyles();
        }

        private void SetupDimClose()
        {
            Image dimImg = settingsPopup.GetComponent<Image>();
            if (dimImg == null)
            {
                // 딤 이미지가 없으면 투명 Image를 추가해 레이캐스트 수신
                dimImg = settingsPopup.AddComponent<Image>();
                dimImg.color = Color.clear;
            }
            dimImg.raycastTarget = true;

            Button dimBtn = settingsPopup.GetComponent<Button>() ?? settingsPopup.AddComponent<Button>();
            dimBtn.transition    = Selectable.Transition.None;
            dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.RemoveAllListeners();
            dimBtn.onClick.AddListener(CloseSettings);
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

            SetupRow(content, "SoundToggle",        "사운드",
                () => SettingsManager.Instance.SoundEnabled,        v => SettingsManager.Instance.SetSound(v));
            SetupRow(content, "BGMToggle",          "배경음악",
                () => SettingsManager.Instance.BgmEnabled,          v => SettingsManager.Instance.SetBgm(v));
            SetupRow(content, "VibrationToggle",    "진동",
                () => SettingsManager.Instance.VibrationEnabled,    v => SettingsManager.Instance.SetVibration(v));
            SetupRow(content, "NotificationToggle", "알림",
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

        private void SetupRow(Transform content, string rowName, string label,
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
                labelTmp.fontStyle = FontStyles.Normal;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.margin    = new Vector4(28f, 0f, ToggleW + 28f, 0f);
                labelTmp.raycastTarget = true;
            }

            BuildToggle(row, out Image pillImg, out RectTransform knobRect);

            // 행 아래 구분선
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
            rowBtn.onClick.AddListener(() =>
            {
                bool next = !getState();
                setState(next);
                Refresh(next);
            });
        }

        private void BuildToggle(Transform row, out Image pillImg, out RectTransform knobRect)
        {
            // Pill 배경
            Transform toggleTf = row.Find("_Toggle");
            if (toggleTf == null)
            {
                GameObject go = new GameObject("_Toggle");
                go.transform.SetParent(row, false);
                pillImg  = go.AddComponent<Image>(); // Image 추가 시 RectTransform 자동 생성
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

            // 노브 (흰 원)
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

        // --- 런타임 스프라이트 생성 (외부 에셋 불필요) ---

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
