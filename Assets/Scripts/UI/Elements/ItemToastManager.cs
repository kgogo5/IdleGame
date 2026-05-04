using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.UI;

namespace IdleGame.UI
{
    public class ItemToastManager : MonoBehaviour
    {
        public static ItemToastManager Instance { get; private set; }

        private RectTransform _container;
        private readonly List<GameObject> _activeToasts = new();

        private const float TOAST_DURATION = 3f;
        private const float FADE_DURATION  = 0.5f;
        private const float TOAST_HEIGHT   = 52f;
        private const float TOAST_GAP      = 5f;
        private const float TOAST_WIDTH    = 290f;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null) CreateContainer(canvas.transform);

            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnItemAcquired += ShowItemToast;
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnItemAcquired -= ShowItemToast;
        }

        private void CreateContainer(Transform canvasTransform)
        {
            GameObject obj = new GameObject("ToastContainer");
            obj.transform.SetParent(canvasTransform, false);
            _container = obj.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0, 1);
            _container.anchorMax = new Vector2(0, 1);
            _container.pivot     = new Vector2(0, 1);
            // 상단에서 170px 아래 (HUD 헤더 아래)
            _container.anchoredPosition = new Vector2(10, -170);
            _container.sizeDelta = new Vector2(TOAST_WIDTH, 0);
        }

        public void ShowItemToast(ItemData item)
        {
            if (item == null || _container == null) return;
            StartCoroutine(RunToast(item));
        }

        private IEnumerator RunToast(ItemData item)
        {
            Color rarityColor = item.rarity.ToColor();

            string rarityLabel = item.rarity switch
            {
                ItemRarity.Rare      => "[레어]",
                ItemRarity.Unique    => "[유니크]",
                ItemRarity.Legendary => "[레전더리]",
                _                    => "[일반]",
            };

            // ── Toast 오브젝트 생성 ────────────────────────────
            GameObject toastObj = new GameObject("Toast");
            toastObj.transform.SetParent(_container, false);

            RectTransform rt = toastObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(TOAST_WIDTH, TOAST_HEIGHT);

            Image bg = toastObj.AddComponent<Image>();
            bg.color = UITheme.BgToast;

            // 좌측 색상 바
            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(toastObj.transform, false);
            RectTransform barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0);
            barRt.anchorMax = new Vector2(0, 1);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = new Vector2(5, 0);
            Image barImg = bar.AddComponent<Image>();
            barImg.color = rarityColor;

            // 텍스트
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(toastObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12, 4);
            textRt.offsetMax = new Vector2(-8, -4);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{rarityLabel}</color>  {item.itemName}";
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode  = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;

            _activeToasts.Add(toastObj);
            LayoutToasts();

            // ── 대기 ──────────────────────────────────────────
            yield return new WaitForSeconds(TOAST_DURATION - FADE_DURATION);

            // ── 페이드 아웃 ───────────────────────────────────
            float elapsed = 0f;
            while (elapsed < FADE_DURATION && toastObj != null)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / FADE_DURATION);
                if (bg  != null) bg.color  = new Color(UITheme.BgToast.r, UITheme.BgToast.g, UITheme.BgToast.b, UITheme.BgToast.a * alpha);
                if (barImg != null) barImg.color = new Color(rarityColor.r, rarityColor.g, rarityColor.b, alpha);
                if (tmp != null) tmp.alpha = alpha;
                yield return null;
            }

            if (toastObj != null)
            {
                _activeToasts.Remove(toastObj);
                Destroy(toastObj);
                LayoutToasts();
            }
        }

        private void LayoutToasts()
        {
            float y = 0f;
            foreach (var toast in _activeToasts)
            {
                if (toast == null) continue;
                RectTransform rt = toast.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = new Vector2(0, -y);
                y += TOAST_HEIGHT + TOAST_GAP;
            }
        }
    }
}
