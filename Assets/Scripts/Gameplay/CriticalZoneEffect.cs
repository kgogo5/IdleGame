using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using IdleGame.Core;
using IdleGame.Managers;
using IdleGame.UI;

namespace IdleGame.Gameplay
{
    public class CriticalZoneEffect : MonoBehaviour, IPointerClickHandler
    {
        public static bool IsActive { get; private set; }

        // 에디터에서 Play 재진입 시 이전 세션 텍스처가 파괴된 상태로 참조될 수 있으므로 null 리셋
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _ringSprite   = null;
            _circleSprite = null;
            IsActive      = false;
        }

        private const float DURATION   = 0.7f;
        private const float START_SIZE = 220f;
        private const float INNER_SIZE = 76f;
        private const float MAX_PULSE  = 1.2f;

        // 링/원 스프라이트 — 런타임 1회 생성 후 캐시
        private static Sprite _ringSprite;
        private static Sprite _circleSprite;

        private float         _elapsed;
        private double        _critDamage;
        private Monster       _target;
        private RectTransform _rootRt;
        private Image         _ringImg;
        private RectTransform _innerRt;
        private bool          _triggered;

        // ── 스프라이트 생성 ────────────────────────────────────────────────────

        private static Sprite BuildRingSprite()
        {
            if (_ringSprite != null) return _ringSprite;
            const int SIZE = 128;
            float outerR = SIZE / 2f - 1f;
            float innerR = outerR * 0.90f;   // 스트로크 = 외반경의 10%
            var tex    = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[SIZE * SIZE];
            var center = new Vector2(SIZE / 2f, SIZE / 2f);
            for (int y = 0; y < SIZE; y++)
                for (int x = 0; x < SIZE; x++)
                {
                    float d     = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float outer = Mathf.Clamp01((outerR - d) * 2f + 1f);  // 외곽 경계 소프트
                    float inner = Mathf.Clamp01((d - innerR) * 2f + 1f);  // 내부 경계 소프트
                    byte  a     = (byte)(Mathf.Min(outer, inner) * 255f);  // 링만 불투명
                    pixels[y * SIZE + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            _ringSprite = Sprite.Create(tex, new Rect(0, 0, SIZE, SIZE), new Vector2(0.5f, 0.5f), 100f);
            return _ringSprite;
        }

        private static Sprite BuildCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            const int SIZE = 128;
            float r    = SIZE / 2f - 1f;
            var tex    = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color32[SIZE * SIZE];
            var center = new Vector2(SIZE / 2f, SIZE / 2f);
            for (int y = 0; y < SIZE; y++)
                for (int x = 0; x < SIZE; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    byte  a = (byte)(Mathf.Clamp01((r - d) * 2f + 1f) * 255f);
                    pixels[y * SIZE + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0, 0, SIZE, SIZE), new Vector2(0.5f, 0.5f), 100f);
            return _circleSprite;
        }

        // ── 생성 ─────────────────────────────────────────────────────────────

        public static void TrySpawn(Canvas canvas, Vector3 worldPos, double critDamage, Monster target)
        {
            if (IsActive || canvas == null) return;

            var go = new GameObject("CriticalZone");
            go.transform.SetParent(canvas.transform, false);
            go.transform.SetAsLastSibling();

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(START_SIZE, START_SIZE);

            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(), screenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPos);
            rt.anchoredPosition = localPos;

            // 투명 히트박스 (클릭 수신)
            var rootImg = go.AddComponent<Image>();
            rootImg.sprite        = BuildCircleSprite();
            rootImg.color         = new Color(0f, 0f, 0f, 0f);
            rootImg.raycastTarget = true;

            // 바깥 링 — 링 스프라이트로 배경 완전 투명
            var ringGo = MakeChild(go.transform, "Ring", START_SIZE);
            var ringImg = ringGo.AddComponent<Image>();
            ringImg.sprite        = BuildRingSprite();
            ringImg.color         = UITheme.CritZoneRing;
            ringImg.raycastTarget = false;

            // 안쪽 흰 원
            var innerGo = MakeChild(go.transform, "Inner", INNER_SIZE);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.sprite        = BuildCircleSprite();
            innerImg.color         = Color.white;
            innerImg.raycastTarget = false;

            var zone          = go.AddComponent<CriticalZoneEffect>();
            zone._critDamage  = critDamage;
            zone._target      = target;
            zone._rootRt      = rt;
            zone._ringImg     = ringImg;
            zone._innerRt     = innerGo.GetComponent<RectTransform>();

            IsActive = true;
        }

        private static GameObject MakeChild(Transform parent, string objName, float size)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(parent, false);
            var crt = go.AddComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot     = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(size, size);
            return go;
        }

        // ── 업데이트 ──────────────────────────────────────────────────────────

        private void Update()
        {
            if (_triggered) return;
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / DURATION);

            // 바깥 링 수축
            float outerSize = Mathf.Lerp(START_SIZE, INNER_SIZE, t);
            if (_rootRt  != null) _rootRt.sizeDelta                 = new Vector2(outerSize, outerSize);
            if (_ringImg != null) _ringImg.rectTransform.sizeDelta   = new Vector2(outerSize, outerSize);

            // 링 색 변화: 금 → 빨강
            float pulse = (Mathf.Sin(_elapsed * 8f) + 1f) * 0.5f;
            if (_ringImg != null)
                _ringImg.color = Color.Lerp(
                    UITheme.CritZoneRing,
                    UITheme.CritZoneRingAlert,
                    Mathf.Clamp01(t + pulse * 0.08f));

            // 안쪽 원 맥동 (수렴할수록 줄어듦)
            float pulseDecay  = Mathf.Clamp01(1f - t * 1.4f);
            float pulseFactor = 1f + (MAX_PULSE - 1f) * pulseDecay * ((Mathf.Sin(_elapsed * 6f) + 1f) * 0.5f);
            if (_innerRt != null)
            {
                float sz = INNER_SIZE * pulseFactor;
                _innerRt.sizeDelta = new Vector2(sz, sz);
            }

            if (t >= 1f) Destroy(gameObject);
        }

        // ── 클릭 ─────────────────────────────────────────────────────────────

        public void OnPointerClick(PointerEventData _)
        {
            if (_triggered) return;
            if (_target == null || _target.CurrentHealth <= 0) { Destroy(gameObject); return; }
            _triggered = true;
            _target.TakeDamage(_critDamage);
            AudioManager.Instance?.PlayHit();
            Destroy(gameObject);
        }

        private void OnDestroy() => IsActive = false;
    }
}
