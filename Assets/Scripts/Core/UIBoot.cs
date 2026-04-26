using UnityEngine;
using UnityEngine.UI;
using IdleGame.UI;

namespace IdleGame.Core
{
    public class UIBoot : MonoBehaviour
    {
        private void Start()
        {
            Canvas canvas = FindObjectOfType<Canvas>();
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

            // Create ContentArea between HUD (top 120px) and BottomNavPanel (bottom 100px)
            GameObject contentAreaObj = new GameObject("ContentArea");
            contentAreaObj.transform.SetParent(canvasTransform, false);

            // Insert before BottomNavPanel in hierarchy so it renders behind nav
            contentAreaObj.transform.SetSiblingIndex(navPanel.GetSiblingIndex());

            RectTransform rt = contentAreaObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(0, 100);
            rt.offsetMax = new Vector2(0, -120);

            // Wire NavigationController
            NavigationController nav = navPanel.GetComponent<NavigationController>();
            if (nav == null) nav = navPanel.gameObject.AddComponent<NavigationController>();
            nav.Initialize(contentAreaObj.transform);

            Debug.Log("UIBoot: Navigation initialized");
        }
    }
}
