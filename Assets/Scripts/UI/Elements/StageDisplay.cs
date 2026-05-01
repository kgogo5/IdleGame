using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Core;

namespace IdleGame.UI
{
    public class StageDisplay : MonoBehaviour
    {
        private static readonly string[] STAGE_NAMES =
        {
            "",
            "초원",
            "석굴",
            "어둠의 숲",
            "지하 묘지",
            "불의 협곡",
            "잊혀진 폐허",
            "공포의 수도원",
            "강철 요새",
            "지옥문",
            "절망의 탑",
            "혼돈의 성역",
            "지옥 심층부",
        };

        private TextMeshProUGUI _text;
        private GameObject      _popup;
        private Transform       _listContent;
        private readonly List<(int stage, Image bg, TextMeshProUGUI badge)> _rows = new();

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnStageChanged    += OnStageChanged;
                MonsterManager.Instance.OnMaxStageChanged += OnMaxStageChanged;
                UpdateLabel(MonsterManager.Instance.Stage);
            }

            // 텍스트 오브젝트에 Button 추가해 클릭 가능하게
            var btn = gameObject.GetComponent<Button>() ?? gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            if (_text != null) _text.raycastTarget = true;
            btn.onClick.AddListener(TogglePopup);
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnStageChanged    -= OnStageChanged;
                MonsterManager.Instance.OnMaxStageChanged -= OnMaxStageChanged;
            }
        }

        private void OnStageChanged(int stage)
        {
            UpdateLabel(stage);
            RefreshRows();
        }

        private void OnMaxStageChanged(int _)
        {
            RebuildRows();
        }

        private void UpdateLabel(int stage)
        {
            if (_text == null) return;
            string name = stage < STAGE_NAMES.Length ? STAGE_NAMES[stage] : $"Stage {stage}";
            _text.text = name + " ▼";
        }

        // ── 팝업 토글 ────────────────────────────────────────────────────────────

        private void TogglePopup()
        {
            if (_popup == null) BuildPopup();
            bool next = !_popup.activeSelf;
            _popup.SetActive(next);
            if (next)
            {
                _popup.transform.SetAsLastSibling();
                RefreshRows();
            }
        }

        private void ClosePopup()
        {
            if (_popup != null) _popup.SetActive(false);
        }

        // ── 팝업 UI 구성 (최초 1회) ─────────────────────────────────────────────

        private void BuildPopup()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // ── 전체화면 반투명 오버레이 ──
            _popup = new GameObject("StagePickerPopup");
            _popup.transform.SetParent(canvas.transform, false);
            var overlayRt = _popup.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImg = _popup.AddComponent<Image>();
            overlayImg.color = new Color(0f, 0f, 0f, 0.55f);
            // 오버레이 클릭 → 닫기
            var overlayBtn = _popup.AddComponent<Button>();
            overlayBtn.transition = Selectable.Transition.None;
            overlayBtn.onClick.AddListener(ClosePopup);

            // ── 카드 패널 ──
            var card = new GameObject("Card");
            card.transform.SetParent(_popup.transform, false);
            var cardRt = card.AddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.pivot     = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(340, 500);
            card.AddComponent<Image>().color = new Color(0.09f, 0.11f, 0.17f);
            // 카드 클릭은 오버레이로 전파 방지
            card.AddComponent<Image>(); // 이미 추가됨, raycastTarget=true가 기본
            // 별도 block
            var blocker = card.GetComponent<Image>();
            blocker.raycastTarget = true;

            // ── 타이틀 ──
            var title = new GameObject("Title");
            title.transform.SetParent(card.transform, false);
            var titleRt = title.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0, 1);
            titleRt.anchorMax = new Vector2(1, 1);
            titleRt.pivot     = new Vector2(0.5f, 1f);
            titleRt.offsetMin = new Vector2(16, -72);
            titleRt.offsetMax = new Vector2(-16, 0);
            var titleTmp = title.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "지역 선택";
            titleTmp.fontSize = 28; titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.raycastTarget = false;

            // 구분선
            var div = new GameObject("Div");
            div.transform.SetParent(card.transform, false);
            var divRt = div.AddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0, 1); divRt.anchorMax = new Vector2(1, 1);
            divRt.pivot = new Vector2(0.5f, 1f);
            divRt.offsetMin = new Vector2(0, -74); divRt.offsetMax = new Vector2(0, -72);
            div.AddComponent<Image>().color = new Color(1, 1, 1, 0.08f);

            // ── 스크롤 뷰 ──
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(card.transform, false);
            var scrollRt = scrollObj.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(8, 50);
            scrollRt.offsetMax = new Vector2(-8, -80);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            scrollRect.viewport = vpRt;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5; vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentRt;

            _listContent = content.transform;

            // ── 닫기 버튼 ──
            var closeBtn = new GameObject("CloseBtn");
            closeBtn.transform.SetParent(card.transform, false);
            var closeBtnRt = closeBtn.AddComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(0, 0);
            closeBtnRt.anchorMax = new Vector2(1, 0);
            closeBtnRt.pivot     = new Vector2(0.5f, 0f);
            closeBtnRt.offsetMin = new Vector2(12, 8);
            closeBtnRt.offsetMax = new Vector2(-12, 48);
            closeBtn.AddComponent<Image>().color = new Color(0.4f, 0.12f, 0.12f);
            var closeBtnComp = closeBtn.AddComponent<Button>();
            closeBtnComp.onClick.AddListener(ClosePopup);
            var closeTxt = new GameObject("T");
            closeTxt.transform.SetParent(closeBtn.transform, false);
            var closeTxtRt = closeTxt.AddComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero; closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = closeTxtRt.offsetMax = Vector2.zero;
            var closeTmp = closeTxt.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "닫기"; closeTmp.fontSize = 22;
            closeTmp.color = Color.white; closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.raycastTarget = false;

            _popup.SetActive(false);
            RebuildRows();
        }

        // ── 버튼 목록 재생성 ─────────────────────────────────────────────────────

        private void RebuildRows()
        {
            if (_listContent == null) return;

            foreach (Transform child in _listContent)
                Destroy(child.gameObject);
            _rows.Clear();

            int maxStage = MonsterManager.Instance != null
                ? MonsterManager.Instance.MaxStageReached
                : 1;

            for (int s = 1; s <= Mathf.Max(maxStage, STAGE_NAMES.Length - 1); s++)
            {
                bool unlocked = s <= maxStage;
                var (bg, badge) = CreateRow(_listContent, s, unlocked);
                _rows.Add((s, bg, badge));
            }

            RefreshRows();
        }

        private (Image bg, TextMeshProUGUI badge) CreateRow(Transform parent, int stage, bool unlocked)
        {
            var row = new GameObject($"S{stage}");
            row.transform.SetParent(parent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 56);
            var le = row.AddComponent<UnityEngine.UI.LayoutElement>();
            le.preferredHeight = 56;

            var bg = row.AddComponent<Image>();
            bg.color = unlocked ? new Color(0.18f, 0.22f, 0.30f) : new Color(0.12f, 0.12f, 0.16f);

            var btn = row.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor      = unlocked ? new Color(0.18f, 0.22f, 0.30f) : new Color(0.12f, 0.12f, 0.16f);
            cb.highlightedColor = unlocked ? new Color(0.25f, 0.35f, 0.50f) : cb.normalColor;
            cb.pressedColor     = unlocked ? new Color(0.20f, 0.45f, 0.70f) : cb.normalColor;
            btn.colors = cb;
            btn.interactable = unlocked;
            btn.targetGraphic = bg;

            if (unlocked)
            {
                int captured = stage;
                btn.onClick.AddListener(() =>
                {
                    MonsterManager.Instance?.SelectStage(captured);
                    ClosePopup();
                });
            }

            // 스테이지 이름
            var labelObj = new GameObject("L");
            labelObj.transform.SetParent(row.transform, false);
            var labelRt = labelObj.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(0.72f, 1);
            labelRt.offsetMin = new Vector2(14, 0);
            labelRt.offsetMax = Vector2.zero;
            var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = stage < STAGE_NAMES.Length
                ? $"Stage {stage}  {STAGE_NAMES[stage]}"
                : $"Stage {stage}";
            labelTmp.fontSize = 20;
            labelTmp.color = unlocked ? Color.white : new Color(0.38f, 0.38f, 0.42f);
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.raycastTarget = false;

            // 배지 (현재/자물쇠)
            var badgeObj = new GameObject("B");
            badgeObj.transform.SetParent(row.transform, false);
            var badgeRt = badgeObj.AddComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0.72f, 0.1f);
            badgeRt.anchorMax = new Vector2(1f, 0.9f);
            badgeRt.offsetMin = new Vector2(0, 0);
            badgeRt.offsetMax = new Vector2(-10, 0);
            var badge = badgeObj.AddComponent<TextMeshProUGUI>();
            badge.text = unlocked ? "" : "🔒";
            badge.fontSize = 18;
            badge.color = new Color(0.5f, 0.5f, 0.5f);
            badge.alignment = TextAlignmentOptions.MidlineRight;
            badge.raycastTarget = false;

            return (bg, badge);
        }

        // ── 현재/최대 스테이지에 따른 색상·배지 갱신 ──────────────────────────

        private void RefreshRows()
        {
            if (MonsterManager.Instance == null) return;
            int current = MonsterManager.Instance.Stage;
            int max     = MonsterManager.Instance.MaxStageReached;

            foreach (var (stage, bg, badge) in _rows)
            {
                if (bg == null || badge == null) continue;

                bool unlocked  = stage <= max;
                bool isCurrent = stage == current;

                if (bg != null)
                    bg.color = isCurrent
                        ? new Color(0.15f, 0.45f, 0.70f)
                        : (unlocked ? new Color(0.18f, 0.22f, 0.30f) : new Color(0.12f, 0.12f, 0.16f));

                badge.text  = isCurrent ? "▶ 현재" : (unlocked ? "" : "🔒");
                badge.color = isCurrent ? new Color(0.4f, 1f, 0.6f) : new Color(0.5f, 0.5f, 0.5f);
            }
        }
    }
}
