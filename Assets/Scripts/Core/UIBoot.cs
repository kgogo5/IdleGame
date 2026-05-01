using UnityEngine;
using UnityEngine.UI;
using IdleGame.UI;

namespace IdleGame.Core
{
    public class UIBoot : MonoBehaviour
    {
        private void Start()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("UIBoot: No Canvas found"); return; }

            SetupBackground();
            SetupContentArea(canvas.transform);
        }

        private void SetupBackground()
        {
            if (GameObject.Find("GameBackground") != null) return;

            Sprite sprite = Resources.Load<Sprite>("Backgrounds/stage1_bg");
            if (sprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>("Backgrounds/stage1_bg");
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            if (sprite == null) return;

            GameObject bgObj = new GameObject("GameBackground");
            Camera cam = Camera.main;
            Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
            bgObj.transform.position = new Vector3(pos.x, pos.y, pos.z + 20f);

            SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -100;

            if (cam != null)
            {
                float camH = cam.orthographicSize * 2f;
                float camW = camH * cam.aspect;
                float sprH = sr.sprite.bounds.size.y;
                float sprW = sr.sprite.bounds.size.x;
                float scale = Mathf.Max(camW / sprW, camH / sprH);
                bgObj.transform.localScale = new Vector3(scale, scale, 1f);
            }
        }

        private void SetupContentArea(Transform canvasTransform)
        {
            Transform navPanel = canvasTransform.Find("BottomNavPanel");
            if (navPanel == null) { Debug.LogError("UIBoot: BottomNavPanel not found"); return; }

            Transform existing = canvasTransform.Find("ContentArea");
            if (existing != null) { Debug.Log("UIBoot: ContentArea already exists"); return; }

            // 통화 바: 최상단 65px (비전투 탭에서만 표시)
            GameObject barObj = new GameObject("PanelCurrencyBar");
            barObj.transform.SetParent(canvasTransform, false);
            RectTransform barRt = barObj.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 1);
            barRt.anchorMax = new Vector2(1, 1);
            barRt.offsetMin = new Vector2(0, -65);
            barRt.offsetMax = new Vector2(0, 0);
            barObj.AddComponent<PanelCurrencyBar>();

            // ContentArea: 통화 바 아래 ~ nav 바 위
            GameObject contentAreaObj = new GameObject("ContentArea");
            contentAreaObj.transform.SetParent(canvasTransform, false);
            contentAreaObj.transform.SetSiblingIndex(navPanel.GetSiblingIndex());

            RectTransform rt = contentAreaObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 100); // nav 바 높이
            rt.offsetMax = new Vector2(0, -65); // 통화 바 높이

            // 패널 뒤 배경색 (게임 씬이 비치지 않도록)
            Image bg = contentAreaObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

            navPanel.SetAsLastSibling();

            // HUD 패널 찾기 (GoldDisplay 포함)
            GameObject hudPanel = null;
            GoldDisplay goldDisplay = Object.FindAnyObjectByType<GoldDisplay>();
            if (goldDisplay != null)
            {
                Transform t = goldDisplay.transform;
                while (t.parent != canvasTransform && t.parent != null)
                    t = t.parent;
                if (t.parent == canvasTransform)
                {
                    hudPanel = t.gameObject;
                    t.SetAsLastSibling();
                }
            }
            navPanel.SetAsLastSibling();

            NavigationController nav = navPanel.GetComponent<NavigationController>();
            if (nav == null) nav = navPanel.gameObject.AddComponent<NavigationController>();
            if (hudPanel != null) nav.SetHudPanel(hudPanel);
            nav.SetCurrencyBar(barObj);
            nav.Initialize(contentAreaObj.transform);

            // 아이템 획득 토스트 매니저
            if (UI.ItemToastManager.Instance == null)
            {
                GameObject toastMgrObj = new GameObject("ItemToastManager");
                toastMgrObj.AddComponent<UI.ItemToastManager>();
            }

            Debug.Log("UIBoot: Navigation initialized");
        }
    }
}
