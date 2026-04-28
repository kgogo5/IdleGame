using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace IdleGame.UI
{
    public class NavigationController : MonoBehaviour
    {
        // 원본 탭 순서: 업그레이드 / 장비 / 전투(가운데) / 상점 / 업적
        private static readonly string[] TAB_NAMES = { "업그레이드", "장비", "전투", "상점", "업적" };
        private const int BATTLE_TAB = 2;

        private GameObject[]  _panels;
        private Button[]      _navButtons;
        private Transform     _contentArea;
        private GameObject    _hudPanel;
        private int           _activeTab = BATTLE_TAB;

        public void SetHudPanel(GameObject hud) => _hudPanel = hud;

        public void Initialize(Transform contentArea)
        {
            _contentArea = contentArea;
            _navButtons  = new Button[TAB_NAMES.Length];
            _panels      = new GameObject[TAB_NAMES.Length];

            for (int i = 0; i < transform.childCount && i < TAB_NAMES.Length; i++)
            {
                Button btn = transform.GetChild(i).GetComponent<Button>();
                if (btn == null) continue;

                _navButtons[i] = btn;

                TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = TAB_NAMES[i];

                int idx = i;
                btn.onClick.AddListener(() => ShowTab(idx));
            }

            // 전투 탭은 패널 없음; 나머지만 생성
            for (int i = 0; i < TAB_NAMES.Length; i++)
            {
                if (i == BATTLE_TAB) { _panels[i] = null; continue; }
                _panels[i] = CreatePanel(contentArea, TAB_NAMES[i]);
            }

            AttachPanelScripts();
            ShowTab(BATTLE_TAB);
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name + "Panel");
            panel.transform.SetParent(parent, false);

            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.97f);
            bg.raycastTarget = true;

            return panel;
        }

        private void AttachPanelScripts()
        {
            if (_panels[0] != null) _panels[0].AddComponent<Panels.UpgradePanelUI>();
            if (_panels[1] != null) _panels[1].AddComponent<Panels.EquipmentPanelUI>();
            if (_panels[3] != null) _panels[3].AddComponent<Panels.ShopPanelUI>();
            if (_panels[4] != null) _panels[4].AddComponent<Panels.AchievementPanelUI>();
        }

        private void ShowTab(int index)
        {
            _activeTab = index;

            bool isBattle = index == BATTLE_TAB;

            // HUD는 전투 탭에서만 표시
            if (_hudPanel != null)
                _hudPanel.SetActive(isBattle);

            // 전투 탭일 때 콘텐츠 영역 완전히 숨김 → 몬스터 클릭 가능
            if (_contentArea != null)
                _contentArea.gameObject.SetActive(!isBattle);

            if (!isBattle)
            {
                for (int i = 0; i < _panels.Length; i++)
                {
                    if (_panels[i] != null)
                        _panels[i].SetActive(i == index);
                }
            }

            UpdateButtonColors();
        }

        private void UpdateButtonColors()
        {
            for (int i = 0; i < _navButtons.Length; i++)
            {
                if (_navButtons[i] == null) continue;
                bool active = i == _activeTab;

                Image img = _navButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = active
                        ? new Color(0.25f, 0.6f, 1f, 1f)
                        : new Color(0.18f, 0.18f, 0.22f, 1f);

                TextMeshProUGUI label = _navButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = active ? Color.white : new Color(0.82f, 0.82f, 0.82f);
            }
        }
    }
}
