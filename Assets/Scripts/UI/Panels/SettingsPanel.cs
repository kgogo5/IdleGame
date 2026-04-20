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
        [SerializeField] private Button settingsButton;

        [Header("팝업 배경색")]
        [SerializeField] private Color contentBgColor = new Color(0.13f, 0.16f, 0.22f, 0.98f);

        [Header("닫기 버튼")]
        [SerializeField] private float closeButtonHeight  = 90f;
        [SerializeField] private Color closeBtnNormal     = new Color(0.80f, 0.22f, 0.22f, 1f);
        [SerializeField] private Color closeBtnHighlight  = new Color(1.00f, 0.35f, 0.35f, 1f);
        [SerializeField] private Color closeBtnPressed    = new Color(0.55f, 0.10f, 0.10f, 1f);

        [Header("텍스트 색상")]
        [SerializeField] private Color labelColor = new Color(0.90f, 0.92f, 0.95f, 1f);
        [SerializeField] private Color onColor    = new Color(0.30f, 0.80f, 0.45f, 1f);
        [SerializeField] private Color offColor   = new Color(0.35f, 0.35f, 0.40f, 1f);

        [Header("옵션 행 높이")]
        [SerializeField] private float rowHeight = 72f;

        private void Start()
        {
            settingsButton.onClick.AddListener(OpenSettings);
            ApplyStyles();
            SetupDimClose();
        }

        private void SetupDimClose()
        {
            Image dimImage = settingsPopup.GetComponent<Image>();
            if (dimImage == null) return;

            dimImage.raycastTarget = true; // 딤 클릭 수신에 필수

            Button dimBtn = settingsPopup.GetComponent<Button>();
            if (dimBtn == null) dimBtn = settingsPopup.AddComponent<Button>();
            dimBtn.transition = Selectable.Transition.None;
            dimBtn.targetGraphic = dimImage;
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

            VerticalLayoutGroup vg = content.GetComponent<VerticalLayoutGroup>();
            if (vg != null)
            {
                vg.childForceExpandHeight = false; // 각 행은 자신의 preferredHeight만 사용
                vg.childControlHeight = true;
                vg.childAlignment = TextAnchor.UpperCenter;
                vg.spacing = 4f;
                vg.padding = new RectOffset(0, 0, 12, 12);
            }

            SetupRow(content, "SoundToggle",        "사운드",   () => SettingsManager.Instance.SoundEnabled,
                v => SettingsManager.Instance.SetSound(v));
            SetupRow(content, "BGMToggle",          "배경음악", () => SettingsManager.Instance.BgmEnabled,
                v => SettingsManager.Instance.SetBgm(v));
            SetupRow(content, "VibrationToggle",    "진동",     () => SettingsManager.Instance.VibrationEnabled,
                v => SettingsManager.Instance.SetVibration(v));
            SetupRow(content, "NotificationToggle", "알림",     () => SettingsManager.Instance.NotificationEnabled,
                v => SettingsManager.Instance.SetNotification(v));

            Transform closeBtn = content.Find("CloseButton");
            if (closeBtn != null)
            {
                if (content.Find("_Spacer") == null)
                {
                    int idx = closeBtn.GetSiblingIndex();
                    GameObject spacer = new GameObject("_Spacer");
                    spacer.transform.SetParent(content, false);
                    LayoutElement sle = spacer.AddComponent<LayoutElement>();
                    sle.flexibleHeight = 1f;
                    spacer.transform.SetSiblingIndex(idx);
                }

                LayoutElement cle = closeBtn.GetComponent<LayoutElement>();
                if (cle == null) cle = closeBtn.gameObject.AddComponent<LayoutElement>();
                cle.preferredHeight = closeButtonHeight;
                cle.flexibleHeight  = 0f;

                Button btn = closeBtn.GetComponent<Button>();
                if (btn != null)
                {
                    ColorBlock cb       = btn.colors;
                    cb.normalColor      = closeBtnNormal;
                    cb.highlightedColor = closeBtnHighlight;
                    cb.pressedColor     = closeBtnPressed;
                    btn.colors          = cb;
                }

                TextMeshProUGUI closeTmp = closeBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (closeTmp != null) closeTmp.color = Color.white;
            }
        }

        private void SetupRow(Transform content, string rowName, string label,
            Func<bool> getState, Action<bool> setState)
        {
            Transform row = content.Find(rowName);
            if (row == null) return;

            LayoutElement le = row.GetComponent<LayoutElement>();
            if (le == null) le = row.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = rowHeight;
            le.flexibleHeight  = 0f;

            // 라벨 TMP (row 오브젝트에 직접 붙어 있음)
            TextMeshProUGUI labelTmp = row.GetComponent<TextMeshProUGUI>();
            if (labelTmp != null)
            {
                labelTmp.text      = label;
                labelTmp.color     = labelColor;
                labelTmp.fontSize  = 30f;
                labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
                labelTmp.margin    = new Vector4(24f, 0f, 116f, 0f); // 오른쪽 여백: 토글 공간
                labelTmp.raycastTarget = true;
            }

            // 토글 스위치 (오른쪽에 절대 위치)
            Image toggleImg;
            RectTransform toggleRect;

            Transform toggleTf = row.Find("_Toggle");
            if (toggleTf == null)
            {
                GameObject toggleGo = new GameObject("_Toggle");
                toggleGo.transform.SetParent(row, false);
                toggleImg  = toggleGo.AddComponent<Image>(); // Image 추가 시 RectTransform 생성됨
                toggleRect = (RectTransform)toggleGo.transform;
            }
            else
            {
                toggleImg  = toggleTf.GetComponent<Image>() ?? toggleTf.gameObject.AddComponent<Image>();
                toggleRect = (RectTransform)toggleTf;
            }

            toggleImg.raycastTarget   = false;
            toggleRect.anchorMin      = new Vector2(1f, 0.5f);
            toggleRect.anchorMax      = new Vector2(1f, 0.5f);
            toggleRect.pivot          = new Vector2(1f, 0.5f);
            toggleRect.anchoredPosition = new Vector2(-20f, 0f);
            toggleRect.sizeDelta      = new Vector2(92f, 44f);

            // 토글 텍스트 (ON / OFF)
            TextMeshProUGUI toggleTmp = toggleRect.GetComponentInChildren<TextMeshProUGUI>(true);
            if (toggleTmp == null)
            {
                GameObject textGo = new GameObject("_Text");
                textGo.transform.SetParent(toggleRect, false);
                toggleTmp = textGo.AddComponent<TextMeshProUGUI>();
                RectTransform textRect = (RectTransform)textGo.transform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }
            toggleTmp.alignment    = TextAlignmentOptions.Center;
            toggleTmp.fontSize     = 22f;
            toggleTmp.fontStyle    = FontStyles.Bold;
            toggleTmp.raycastTarget = false;

            // 행 전체 클릭 → 토글
            Button rowBtn = row.GetComponent<Button>();
            if (rowBtn == null) rowBtn = row.gameObject.AddComponent<Button>();
            rowBtn.transition = Selectable.Transition.None;
            if (labelTmp != null) rowBtn.targetGraphic = labelTmp;

            void Refresh(bool state)
            {
                toggleImg.color = state ? onColor : offColor;
                toggleTmp.text  = state ? "ON"    : "OFF";
                toggleTmp.color = state ? Color.white : new Color(0.75f, 0.75f, 0.80f, 1f);
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
    }
}
