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
    /// <summary>
    /// 개발자용 스테이지/몬스터 관리 패널.
    /// SettingsPanel 어드민 탭 → "콘텐츠 관리" 버튼으로 열림.
    ///
    /// [스테이지 탭] 스테이지별 잡몹 풀 · 보스 · 배경/BGM 확인
    /// [몬스터 탭]  모든 MonsterData의 수치를 런타임에서 직접 편집
    ///              (변경은 세션 내 유지, 종료하면 ScriptableObject 기본값으로 복원)
    /// </summary>
    public class ContentManagerPanel : MonoBehaviour
    {
        private static ContentManagerPanel _instance;

        private GameObject _root;
        private Transform  _scrollContent;
        private Image      _stageTabImg;
        private Image      _monsterTabImg;

        // ── 진입점 ──────────────────────────────────────────────────────────────

        /// <summary>Canvas 자식으로 패널을 생성(최초 1회)하고 표시.</summary>
        public static void OpenOrCreate(Transform canvasParent)
        {
            if (_instance == null)
            {
                var go = new GameObject("ContentManagerPanel");
                go.transform.SetParent(canvasParent, false);
                _instance = go.AddComponent<ContentManagerPanel>();
                _instance.BuildRoot();
            }
            _instance._root.SetActive(true);
            _instance.ShowTab(true); // 스테이지 탭으로 시작
        }

        private void OnDestroy() { if (_instance == this) _instance = null; }

        // ── UI 뼈대 생성 ─────────────────────────────────────────────────────────

        private void BuildRoot()
        {
            // ── 전체화면 오버레이 ──
            _root = new GameObject("Root");
            _root.transform.SetParent(transform, false);
            FullStretch(_root.AddComponent<RectTransform>());
            _root.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.97f);

            // ── 헤더 (60px, 상단 고정) ──
            var headerRt = MakeAnchoredRect("Header", _root.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -60f), new Vector2(0f, 0f));
            headerRt.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.20f, 1f);

            var titleTmp = MakeText(headerRt, "Title", "콘텐츠 관리 (개발자)", 26f, TextAlignmentOptions.MidlineLeft);
            StretchWithPad(titleTmp.rectTransform, 14f, 0f, 90f, 0f);

            // 닫기 버튼 (오른쪽 고정)
            var closeBtnRt = MakeAnchoredRect("CloseBtn", headerRt,
                new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(-82f, 6f), new Vector2(-6f, -6f));
            closeBtnRt.gameObject.AddComponent<Image>().color = new Color(0.65f, 0.12f, 0.12f);
            var closeBtn = closeBtnRt.gameObject.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => _root.SetActive(false));
            MakeTextChild(closeBtnRt, "✕", 26f);

            // ── 탭 바 (50px, 헤더 아래) ──
            var tabRt = MakeAnchoredRect("TabBar", _root.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -110f), new Vector2(0f, -60f));
            tabRt.gameObject.AddComponent<Image>().color = new Color(0.11f, 0.14f, 0.22f, 1f);

            var stageTabRt = MakeAnchoredRect("StageTab", tabRt,
                new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                Vector2.zero, Vector2.zero);
            _stageTabImg = stageTabRt.gameObject.AddComponent<Image>();
            var stageBtn  = stageTabRt.gameObject.AddComponent<Button>();
            stageBtn.onClick.AddListener(() => ShowTab(true));
            MakeTextChild(stageTabRt, "스테이지", 22f);

            var monsterTabRt = MakeAnchoredRect("MonsterTab", tabRt,
                new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            _monsterTabImg = monsterTabRt.gameObject.AddComponent<Image>();
            var monsterBtn  = monsterTabRt.gameObject.AddComponent<Button>();
            monsterBtn.onClick.AddListener(() => ShowTab(false));
            MakeTextChild(monsterTabRt, "몬스터", 22f);

            // ── 스크롤뷰 (헤더+탭 아래 전체) ──
            var scrollRt = MakeAnchoredRect("Scroll", _root.transform,
                Vector2.zero, Vector2.one,
                new Vector2(0f, 0f), new Vector2(0f, -110f));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false; scroll.scrollSensitivity = 60f;

            var vpRt = MakeAnchoredRect("Viewport", scrollRt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            vpRt.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpRt, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false; vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;  vlg.childControlWidth  = true;
            vlg.spacing = 6f; vlg.padding = new RectOffset(10, 10, 10, 20);
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;
            _scrollContent = contentGo.transform;
        }

        // ── 탭 전환 ─────────────────────────────────────────────────────────────

        private void ShowTab(bool stageTab)
        {
            Color active   = new Color(0.18f, 0.52f, 0.82f);
            Color inactive = new Color(0.13f, 0.17f, 0.26f);
            _stageTabImg.color   = stageTab  ? active : inactive;
            _monsterTabImg.color = !stageTab ? active : inactive;

            ClearContent();
            if (stageTab) BuildStageTab();
            else          BuildMonsterTab();
        }

        private void ClearContent()
        {
            foreach (Transform child in _scrollContent)
                Destroy(child.gameObject);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  스테이지 탭
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildStageTab()
        {
            var mm = MonsterManager.Instance;
            if (mm == null) { AddInfoLabel("MonsterManager를 찾을 수 없습니다.", Color.red); return; }

            // ── 기본 몬스터 풀 ──
            AddSectionHeader("기본 몬스터 풀  (StageConfig 없을 때 사용)");
            var pool = mm.DefaultMonsterPool;
            if (pool == null || pool.Length == 0)
                AddInfoLabel("  비어 있음  —  Inspector에서 MonsterManager._monsterDataList에 추가하세요.", Muted);
            else
                foreach (var m in pool)
                    if (m != null)
                        AddInfoLabel($"  • {m.monsterName}  |  HP {m.maxHealth}  |  Gold {m.goldReward}  |  드랍 {m.dropChance * 100:F0}%", Color.white);

            // 기본 보스
            var boss = mm.DefaultBossData;
            AddInfoLabel(
                boss != null
                    ? $"  ⚔ 기본 보스: {boss.monsterName}  |  HP {boss.maxHealth}  |  재생 {boss.regenPerSecond}/초"
                    : "  ⚔ 기본 보스: Inspector에서 MonsterManager._defaultBossData에 연결하세요.",
                boss != null ? new Color(1f, 0.5f, 0.2f) : Muted);

            // ── StageConfig 목록 ──
            var configs = mm.StageConfigs;
            if (configs == null || configs.Length == 0)
            {
                AddSectionHeader("StageConfig 없음");
                AddInfoLabel("  Inspector → MonsterManager._stageConfigs 에 StageConfig 에셋을 추가하세요.", Muted);
                return;
            }

            foreach (var cfg in configs)
            {
                if (cfg == null) continue;

                AddSectionHeader($"Stage {cfg.stageFrom} ~ {cfg.stageTo}");

                // 배경 / BGM
                AddInfoLabel(
                    $"  배경: {(string.IsNullOrEmpty(cfg.backgroundPath) ? "(없음)" : cfg.backgroundPath)}" +
                    $"    BGM: {(string.IsNullOrEmpty(cfg.bgmPath)        ? "(없음)" : cfg.bgmPath)}",
                    new Color(0.65f, 0.80f, 1f));

                // 잡몹 목록
                if (cfg.monsters != null && cfg.monsters.Length > 0)
                {
                    AddInfoLabel("  ─ 잡몹 ─", new Color(0.7f, 0.95f, 0.7f));
                    foreach (var m in cfg.monsters)
                    {
                        if (m == null) continue;
                        float total = m.normalWeight + m.rareWeight + m.uniqueWeight + m.legendaryWeight;
                        string weights = total > 0
                            ? $"N {m.normalWeight/total*100:F0}%  R {m.rareWeight/total*100:F0}%  U {m.uniqueWeight/total*100:F1}%  L {m.legendaryWeight/total*100:F1}%"
                            : "(가중치 0)";
                        AddInfoLabel(
                            $"  • {m.monsterName}  |  HP {m.maxHealth}  |  Gold {m.goldReward}  |  드랍 {m.dropChance*100:F0}%  |  {weights}",
                            Color.white);
                    }
                }
                else
                    AddInfoLabel("  잡몹: (기본 풀 사용)", Muted);

                // 보스
                var b = cfg.bossMonster;
                AddInfoLabel(
                    b != null
                        ? $"  ⚔ 보스: {b.monsterName}  |  HP {b.maxHealth}  |  재생 {b.regenPerSecond}/초  |  드랍 {b.dropChance*100:F0}%"
                        : "  ⚔ 보스: (기본 보스 사용)",
                    b != null ? new Color(1f, 0.5f, 0.2f) : Muted);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  몬스터 탭
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildMonsterTab()
        {
            var mm = MonsterManager.Instance;
            if (mm == null) { AddInfoLabel("MonsterManager를 찾을 수 없습니다.", Color.red); return; }

            AddInfoLabel(
                "※ 런타임 편집 — 세션 내 적용. 종료하면 에셋 기본값으로 복원됩니다.",
                new Color(1f, 0.9f, 0.3f));

            // 모든 MonsterData 수집 (중복 제거)
            var all  = new List<MonsterData>();
            var seen = new HashSet<string>();

            void TryAdd(MonsterData m)
            {
                if (m == null || seen.Contains(m.name)) return;
                seen.Add(m.name); all.Add(m);
            }

            if (mm.DefaultMonsterPool != null)
                foreach (var m in mm.DefaultMonsterPool) TryAdd(m);

            if (mm.StageConfigs != null)
                foreach (var cfg in mm.StageConfigs)
                {
                    if (cfg == null) continue;
                    if (cfg.monsters != null) foreach (var m in cfg.monsters) TryAdd(m);
                    TryAdd(cfg.bossMonster);
                }

            TryAdd(mm.DefaultBossData);

            if (all.Count == 0)
            {
                AddInfoLabel("MonsterData가 없습니다. Inspector에서 몬스터를 연결하세요.", Muted);
                return;
            }

            // 보스 먼저 → 잡몹 순
            all.Sort((a, b) => b.isBoss.CompareTo(a.isBoss));

            foreach (var m in all)
                BuildMonsterCard(m);
        }

        private void BuildMonsterCard(MonsterData m)
        {
            // ── 카드 외형 ──
            var card = new GameObject($"Card_{m.name}");
            card.transform.SetParent(_scrollContent, false);
            card.AddComponent<Image>().color = m.isBoss
                ? new Color(0.18f, 0.10f, 0.07f, 1f)
                : new Color(0.10f, 0.13f, 0.20f, 1f);
            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 10);
            vlg.spacing = 5f;
            vlg.childForceExpandHeight = false; vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;  vlg.childControlWidth  = true;
            card.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card.AddComponent<LayoutElement>();

            // ── 이름 ──
            Color nameColor = m.isBoss ? new Color(1f, 0.48f, 0.18f) : new Color(0.82f, 0.92f, 1f);
            string prefix   = m.isBoss ? "⚔ [BOSS]  " : "• ";
            AddCardLabel(card.transform, $"{prefix}{m.monsterName}", nameColor, 23f, FontStyles.Bold);

            // ── HP  /  회복(초) ──
            AddCardSep(card.transform, "체력");
            AddInputRow2(card.transform,
                "최대 체력",          m.maxHealth.ToString("F0"),
                v => { if (double.TryParse(v, out var d)) m.maxHealth = d; },
                "초당 체력 회복 (0=없음)", m.regenPerSecond.ToString("F1"),
                v => { if (float.TryParse(v, out var f)) m.regenPerSecond = f; });

            // ── 골드  /  드랍률 ──
            AddCardSep(card.transform, "보상");
            AddInputRow2(card.transform,
                "골드 (배율 전 기본값)", m.goldReward.ToString("F0"),
                v => { if (double.TryParse(v, out var d)) m.goldReward = d; },
                "드랍 확률 (0~1)",      m.dropChance.ToString("F3"),
                v => { if (float.TryParse(v, out var f)) m.dropChance = Mathf.Clamp01(f); });

            // ── 드랍 가중치 ──
            AddCardSep(card.transform, "드랍 등급 가중치  (상대값 — 합산 후 비율 계산)");
            if (m.isBoss)
            {
                // 보스: 노말 없음
                AddInputRow3(card.transform,
                    "Rare",   m.rareWeight.ToString("F1"),       v => { if (float.TryParse(v, out var f)) m.rareWeight = f; },
                    "Unique", m.uniqueWeight.ToString("F1"),     v => { if (float.TryParse(v, out var f)) m.uniqueWeight = f; },
                    "Legend", m.legendaryWeight.ToString("F1"),  v => { if (float.TryParse(v, out var f)) m.legendaryWeight = f; });
            }
            else
            {
                // 잡몹: Normal / Rare / Unique / Legend
                AddInputRow4(card.transform,
                    "Normal", m.normalWeight.ToString("F1"),     v => { if (float.TryParse(v, out var f)) m.normalWeight = f; },
                    "Rare",   m.rareWeight.ToString("F1"),       v => { if (float.TryParse(v, out var f)) m.rareWeight = f; },
                    "Unique", m.uniqueWeight.ToString("F1"),     v => { if (float.TryParse(v, out var f)) m.uniqueWeight = f; },
                    "Legend", m.legendaryWeight.ToString("F1"),  v => { if (float.TryParse(v, out var f)) m.legendaryWeight = f; });
            }

            // ── 커스텀 드랍 목록 (표시 전용) ──
            if (m.customDrops != null && m.customDrops.Length > 0)
            {
                AddCardSep(card.transform, "커스텀 드랍 (MonsterData.customDrops)");
                foreach (var entry in m.customDrops)
                {
                    if (entry == null || entry.item == null) continue;
                    AddCardLabel(card.transform,
                        $"  • {entry.item.itemName}  가중치 {entry.weight}",
                        new Color(0.75f, 0.85f, 0.65f), 17f);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  UI 헬퍼 — 공통 위젯
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private static Color Muted => new Color(0.45f, 0.50f, 0.58f);

        // 섹션 구분 헤더 (스테이지 탭용)
        private void AddSectionHeader(string text)
        {
            var go = new GameObject("SectionHdr");
            go.transform.SetParent(_scrollContent, false);
            go.AddComponent<Image>().color = new Color(0.16f, 0.22f, 0.36f, 1f);
            go.AddComponent<LayoutElement>().preferredHeight = 32f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 19f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.72f, 0.88f, 1f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(10f, 0, 10f, 0);
            tmp.raycastTarget = false;
        }

        // 일반 텍스트 줄 (스테이지 탭용)
        private void AddInfoLabel(string text, Color color)
        {
            var go = new GameObject("Info");
            go.transform.SetParent(_scrollContent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 26f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = 17f;
            tmp.color = color; tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(8f, 0, 8f, 0);
            tmp.raycastTarget = false;
        }

        // 카드 내부 텍스트 줄
        private void AddCardLabel(Transform parent, string text, Color color, float size, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("CLabel");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = size + 4f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
            tmp.color = color; tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        // 카드 내부 소제목 구분선
        private void AddCardSep(Transform parent, string label)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 22f;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = $"─ {label}"; tmp.fontSize = 15f;
            tmp.color = new Color(0.52f, 0.62f, 0.78f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
        }

        // 입력 2열 행
        private void AddInputRow2(Transform p,
            string l1, string v1, Action<string> c1,
            string l2, string v2, Action<string> c2)
        {
            var row = MakeHRow(p, "IR2");
            MakeLabeledInput(row, l1, v1, c1);
            MakeLabeledInput(row, l2, v2, c2);
        }

        // 입력 3열 행
        private void AddInputRow3(Transform p,
            string l1, string v1, Action<string> c1,
            string l2, string v2, Action<string> c2,
            string l3, string v3, Action<string> c3)
        {
            var row = MakeHRow(p, "IR3");
            MakeLabeledInput(row, l1, v1, c1);
            MakeLabeledInput(row, l2, v2, c2);
            MakeLabeledInput(row, l3, v3, c3);
        }

        // 입력 4열 행
        private void AddInputRow4(Transform p,
            string l1, string v1, Action<string> c1,
            string l2, string v2, Action<string> c2,
            string l3, string v3, Action<string> c3,
            string l4, string v4, Action<string> c4)
        {
            var row = MakeHRow(p, "IR4");
            MakeLabeledInput(row, l1, v1, c1);
            MakeLabeledInput(row, l2, v2, c2);
            MakeLabeledInput(row, l3, v3, c3);
            MakeLabeledInput(row, l4, v4, c4);
        }

        // HLG 컨테이너 행
        private Transform MakeHRow(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 52f;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6f;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
            return go.transform;
        }

        // 라벨 + InputField 한 셀
        private void MakeLabeledInput(Transform parent, string label, string value, Action<string> onChange)
        {
            var cell = new GameObject($"Cell");
            cell.transform.SetParent(parent, false);
            var vlg = cell.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandHeight = false; vlg.childControlHeight = true;
            vlg.childForceExpandWidth  = true;  vlg.childControlWidth  = true;
            vlg.spacing = 2f;

            // 소라벨
            var lGo = new GameObject("Lbl");
            lGo.transform.SetParent(cell.transform, false);
            lGo.AddComponent<LayoutElement>().preferredHeight = 17f;
            var lTmp = lGo.AddComponent<TextMeshProUGUI>();
            lTmp.text = label; lTmp.fontSize = 13f;
            lTmp.color = new Color(0.60f, 0.70f, 0.82f);
            lTmp.alignment = TextAlignmentOptions.MidlineLeft;
            lTmp.raycastTarget = false;

            // 입력창
            var inputGo = new GameObject("IF");
            inputGo.transform.SetParent(cell.transform, false);
            inputGo.AddComponent<LayoutElement>().preferredHeight = 33f;
            inputGo.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 1f);

            var field = inputGo.AddComponent<TMP_InputField>();

            var vp = new GameObject("Area");
            vp.transform.SetParent(inputGo.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = new Vector2(4f, 1f); vpRt.offsetMax = new Vector2(-4f, -1f);
            vp.AddComponent<RectMask2D>();
            field.textViewport = vpRt;

            var tGo = new GameObject("Txt");
            tGo.transform.SetParent(vp.transform, false);
            var tRt = tGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;
            var tTmp = tGo.AddComponent<TextMeshProUGUI>();
            tTmp.fontSize = 17f; tTmp.color = Color.white;
            tTmp.alignment = TextAlignmentOptions.MidlineLeft;
            field.textComponent = tTmp;

            var phGo = new GameObject("Ph");
            phGo.transform.SetParent(vp.transform, false);
            var phRt = phGo.AddComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = phRt.offsetMax = Vector2.zero;
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.fontSize = 15f; phTmp.color = new Color(0.4f, 0.4f, 0.4f);
            phTmp.fontStyle = FontStyles.Italic;
            phTmp.alignment = TextAlignmentOptions.MidlineLeft;
            phTmp.text = "0";
            field.placeholder = phTmp;

            field.text = value;
            field.contentType = TMP_InputField.ContentType.DecimalNumber;
            field.onEndEdit.AddListener(s => onChange?.Invoke(s));
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  RectTransform 헬퍼
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private static void FullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static RectTransform MakeAnchoredRect(string name, Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            return rt;
        }

        private static void StretchWithPad(RectTransform rt, float l, float b, float r, float t)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string name,
            string text, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size;
            tmp.color = Color.white; tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static void MakeTextChild(RectTransform parent, string text, float size)
        {
            var go = new GameObject("Lbl");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            FullStretch(rt);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size;
            tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
    }
}
