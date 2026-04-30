using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;

namespace IdleGame.UI.Panels
{
    public class StageSelectPanelUI : MonoBehaviour
    {
        private static readonly string[] STAGE_NAMES =
        {
            "",                      // 0 (unused)
            "Stage 1  초원",         // 1
            "Stage 2  석굴",         // 2
            "Stage 3  어둠의 숲",    // 3
            "Stage 4  지하 묘지",    // 4
            "Stage 5  불의 협곡",    // 5
            "Stage 6  잊혀진 폐허",  // 6
            "Stage 7  공포의 수도원",// 7
            "Stage 8  강철 요새",    // 8
            "Stage 9  지옥문",       // 9
            "Stage 10 절망의 탑",    // 10
            "Stage 11 혼돈의 성역",  // 11
            "Stage 12 지옥 심층부",  // 12
        };

        private GameObject _overlay;
        private List<Button> _stageButtons = new();
        private int _maxStage;

        private void Awake()
        {
            BuildUI();
        }

        private void Start()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnStageChanged    += OnStageChanged;
                MonsterManager.Instance.OnMaxStageChanged += OnMaxStageUnlocked;
                _maxStage = MonsterManager.Instance.MaxStageReached;
            }
            RefreshButtons();
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnStageChanged    -= OnStageChanged;
                MonsterManager.Instance.OnMaxStageChanged -= OnMaxStageUnlocked;
            }
        }

        private void OnStageChanged(int _) => RefreshButtons();

        private void OnMaxStageUnlocked(int newMax)
        {
            _maxStage = newMax;
            // 새 스테이지 버튼 추가가 필요하면 재빌드, 간단히 기존 버튼 수 비교
            if (newMax >= _stageButtons.Count + 1)
                RebuildButtons();
            else
                RefreshButtons();
        }

        // ── UI 구성 ───────────────────────────────────────────────────────────

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 타이틀
            var title = new GameObject("Title");
            title.transform.SetParent(transform, false);
            var titleRt = title.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.88f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = new Vector2(16, 0);
            titleRt.offsetMax = new Vector2(-16, -8);
            var titleTmp = title.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "지역 선택";
            titleTmp.fontSize = 22;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // 스크롤 뷰
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 0.88f);
            scrollRt.offsetMin = new Vector2(8, 8);
            scrollRt.offsetMax = new Vector2(-8, -4);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollObj.transform, false);
            var viewportRt = viewportObj.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportObj.AddComponent<Image>().color = new Color(0, 0, 0, 0);
            var mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollRect.viewport = viewportRt;

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            var contentRt = contentObj.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot    = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var layout = contentObj.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 6;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;
            layout.childControlHeight = false;

            contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;

            _overlay = contentObj;
            RebuildButtons();
        }

        private void RebuildButtons()
        {
            if (_overlay == null) return;

            foreach (Transform child in _overlay.transform)
                Destroy(child.gameObject);
            _stageButtons.Clear();

            int maxStage = MonsterManager.Instance != null
                ? MonsterManager.Instance.MaxStageReached
                : 1;

            for (int s = 1; s <= Mathf.Max(maxStage, 12); s++)
                _stageButtons.Add(CreateStageButton(_overlay.transform, s, s <= maxStage));

            RefreshButtons();
        }

        private Button CreateStageButton(Transform parent, int stage, bool unlocked)
        {
            var btn = new GameObject($"Stage{stage}Btn");
            btn.transform.SetParent(parent, false);

            var rt = btn.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 52);

            var img = btn.AddComponent<Image>();
            img.color = unlocked ? new Color(0.18f, 0.22f, 0.30f) : new Color(0.12f, 0.12f, 0.15f);

            var button = btn.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor      = unlocked ? new Color(0.18f, 0.22f, 0.30f) : new Color(0.12f, 0.12f, 0.15f);
            colors.highlightedColor = unlocked ? new Color(0.25f, 0.35f, 0.50f) : new Color(0.12f, 0.12f, 0.15f);
            colors.pressedColor     = unlocked ? new Color(0.20f, 0.45f, 0.70f) : new Color(0.12f, 0.12f, 0.15f);
            colors.disabledColor    = new Color(0.12f, 0.12f, 0.15f);
            button.colors = colors;
            button.interactable = unlocked;

            // 이름 텍스트
            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btn.transform, false);
            var labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.5f);
            labelRt.anchorMax = new Vector2(0.75f, 1f);
            labelRt.offsetMin = new Vector2(12, 0);
            labelRt.offsetMax = new Vector2(0, -4);
            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = stage < STAGE_NAMES.Length ? STAGE_NAMES[stage] : $"Stage {stage}";
            label.fontSize = 15;
            label.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // 자물쇠 또는 "현재" 배지
            var badgeObj = new GameObject("Badge");
            badgeObj.transform.SetParent(btn.transform, false);
            var badgeRt = badgeObj.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.75f, 0.1f);
            badgeRt.anchorMax = new Vector2(1f, 0.9f);
            badgeRt.offsetMin = new Vector2(0, 0);
            badgeRt.offsetMax = new Vector2(-8, 0);
            var badge = badgeObj.AddComponent<TextMeshProUGUI>();
            badge.text = unlocked ? "" : "🔒";
            badge.fontSize = 14;
            badge.color = new Color(0.5f, 0.5f, 0.5f);
            badge.alignment = TextAlignmentOptions.MidlineRight;

            if (unlocked)
            {
                int captured = stage;
                button.onClick.AddListener(() =>
                {
                    MonsterManager.Instance?.SelectStage(captured);
                    RefreshButtons();
                });
            }

            return button;
        }

        private void RefreshButtons()
        {
            if (MonsterManager.Instance == null) return;
            int current = MonsterManager.Instance.Stage;
            int max     = MonsterManager.Instance.MaxStageReached;

            for (int i = 0; i < _stageButtons.Count; i++)
            {
                int stage = i + 1;
                var btn = _stageButtons[i];
                if (btn == null) continue;

                bool unlocked = stage <= max;
                bool isCurrent = stage == current;

                var img = btn.GetComponent<Image>();
                if (img != null)
                    img.color = isCurrent
                        ? new Color(0.15f, 0.45f, 0.70f)
                        : (unlocked ? new Color(0.18f, 0.22f, 0.30f) : new Color(0.12f, 0.12f, 0.15f));

                var badge = btn.GetComponentInChildren<TextMeshProUGUI>(); // 첫 번째 tmp는 label
                var tmps = btn.GetComponentsInChildren<TextMeshProUGUI>();
                if (tmps.Length >= 2)
                {
                    tmps[1].text = isCurrent ? "▶ 현재"
                                 : (unlocked ? "" : "🔒");
                    tmps[1].color = isCurrent ? new Color(0.4f, 1f, 0.6f) : new Color(0.5f, 0.5f, 0.5f);
                }
            }
        }
    }
}
