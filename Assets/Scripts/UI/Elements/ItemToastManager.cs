using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using IdleGame.Data;
using IdleGame.Managers;
using IdleGame.Utils;

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
            CreateContainer();

            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemAcquired  += ShowItemToast;
                InventoryManager.Instance.OnItemAutoSold  += ShowAutoSellToast;
            }
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.OnItemAcquired  -= ShowItemToast;
                InventoryManager.Instance.OnItemAutoSold  -= ShowAutoSellToast;
            }
        }

        private void CreateContainer()
        {
            // 씬의 메인 Canvas CanvasScaler 설정을 먼저 확인 (ToastCanvas 생성 전에 검색해야 자기 자신을 안 찾음)
            Canvas mainCanvas = Object.FindAnyObjectByType<Canvas>();
            CanvasScaler mainScaler = mainCanvas?.GetComponent<CanvasScaler>();

            // 전용 Overlay Canvas 생성 — 씬 Canvas 구조에 무관하게 항상 최상위에 표시
            GameObject canvasObj = new GameObject("ToastCanvas");
            canvasObj.transform.SetParent(transform);

            Canvas c = canvasObj.AddComponent<Canvas>();
            c.renderMode   = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 999;

            // 씬 Canvas 의 CanvasScaler 설정 복사 (없으면 기본 Constant Pixel Size)
            if (mainScaler != null)
            {
                CanvasScaler dst = canvasObj.AddComponent<CanvasScaler>();
                dst.uiScaleMode        = mainScaler.uiScaleMode;
                dst.referenceResolution = mainScaler.referenceResolution;
                dst.screenMatchMode    = mainScaler.screenMatchMode;
                dst.matchWidthOrHeight  = mainScaler.matchWidthOrHeight;
            }

            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject obj = new GameObject("ToastContainer");
            obj.transform.SetParent(canvasObj.transform, false);
            _container = obj.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0, 1);
            _container.anchorMax = new Vector2(0, 1);
            _container.pivot     = new Vector2(0, 1);
            _container.anchoredPosition = new Vector2(10, -170);
            _container.sizeDelta = new Vector2(TOAST_WIDTH, 0);
        }

        public void ShowItemToast(ItemData item)
        {
            if (item == null || _container == null) return;
            StartCoroutine(RunToast(item));
        }

        public void ShowAutoSellToast(ItemData item, double gold)
        {
            if (item == null || _container == null) return;
            StartCoroutine(RunAutoSellToast(item, gold));
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

            GameObject toastObj = new GameObject("Toast");
            toastObj.transform.SetParent(_container, false);

            RectTransform rt = toastObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(TOAST_WIDTH, TOAST_HEIGHT);

            Image bg = toastObj.AddComponent<Image>();
            bg.color = UITheme.BgToast;

            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(toastObj.transform, false);
            RectTransform barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0);
            barRt.anchorMax = new Vector2(0, 1);
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = new Vector2(5, 0);
            Image barImg = bar.AddComponent<Image>();
            barImg.color = rarityColor;

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

            yield return new WaitForSeconds(TOAST_DURATION - FADE_DURATION);

            float elapsed = 0f;
            while (elapsed < FADE_DURATION && toastObj != null)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / FADE_DURATION);
                if (bg     != null) bg.color     = new Color(UITheme.BgToast.r, UITheme.BgToast.g, UITheme.BgToast.b, UITheme.BgToast.a * alpha);
                if (barImg != null) barImg.color  = new Color(rarityColor.r, rarityColor.g, rarityColor.b, alpha);
                if (tmp    != null) tmp.alpha     = alpha;
                yield return null;
            }

            if (toastObj != null)
            {
                _activeToasts.Remove(toastObj);
                Destroy(toastObj);
                LayoutToasts();
            }
        }

        private IEnumerator RunAutoSellToast(ItemData item, double gold)
        {
            Color rarityColor = item.rarity.ToColor();
            Color dimColor    = Color.Lerp(rarityColor, new Color(0.3f, 0.3f, 0.3f), 0.5f);
            Color bgColor     = new Color(0.12f, 0.10f, 0.08f, 0.92f);
            Color goldColor   = new Color(1f, 0.85f, 0.3f);

            GameObject toastObj = new GameObject("AutoSellToast");
            toastObj.transform.SetParent(_container, false);
            RectTransform rt = toastObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(TOAST_WIDTH, TOAST_HEIGHT);

            Image bg = toastObj.AddComponent<Image>();
            bg.color = bgColor;

            GameObject bar = new GameObject("Bar");
            bar.transform.SetParent(toastObj.transform, false);
            RectTransform barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0, 0); barRt.anchorMax = new Vector2(0, 1);
            barRt.offsetMin = Vector2.zero;       barRt.offsetMax = new Vector2(5, 0);
            var barImg = bar.AddComponent<Image>();
            barImg.color = dimColor;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(toastObj.transform, false);
            RectTransform textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12, 4); textRt.offsetMax = new Vector2(-8, -4);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"<color=#{ColorUtility.ToHtmlStringRGB(dimColor)}>[판매]</color>  {item.itemName}";
            tmp.fontSize = 20;
            tmp.color = new Color(0.75f, 0.75f, 0.75f);
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode  = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;

            // 골드 라벨 — 박스 오른쪽 외부에 배치 (Image 클리핑 없으므로 항상 보임)
            GameObject goldObj = new GameObject("GoldLabel");
            goldObj.transform.SetParent(toastObj.transform, false);
            RectTransform goldRt = goldObj.AddComponent<RectTransform>();
            goldRt.anchorMin = new Vector2(1f, 0f); goldRt.anchorMax = new Vector2(1f, 1f);
            goldRt.pivot     = new Vector2(0f, 0.5f);
            goldRt.anchoredPosition = new Vector2(8f, 0f);
            goldRt.sizeDelta        = new Vector2(160f, 0f);
            TextMeshProUGUI goldTmp = goldObj.AddComponent<TextMeshProUGUI>();
            goldTmp.text      = $"+{NumberFormatter.Format(gold)}G";
            goldTmp.fontSize  = 22;
            goldTmp.fontStyle = FontStyles.Bold;
            goldTmp.color     = goldColor;
            goldTmp.alignment = TextAlignmentOptions.MidlineLeft;
            goldTmp.raycastTarget = false;

            _activeToasts.Add(toastObj);
            LayoutToasts();

            yield return new WaitForSeconds(TOAST_DURATION - FADE_DURATION);

            float elapsed = 0f;
            while (elapsed < FADE_DURATION && toastObj != null)
            {
                elapsed += Time.deltaTime;
                float a = 1f - (elapsed / FADE_DURATION);
                if (bg      != null) bg.color     = new Color(bgColor.r,  bgColor.g,  bgColor.b,  bgColor.a * a);
                if (barImg  != null) barImg.color  = new Color(dimColor.r, dimColor.g, dimColor.b, a);
                if (tmp     != null) tmp.alpha     = a;
                if (goldTmp != null) goldTmp.alpha = a;
                yield return null;
            }

            if (toastObj != null) { _activeToasts.Remove(toastObj); Destroy(toastObj); LayoutToasts(); }
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
