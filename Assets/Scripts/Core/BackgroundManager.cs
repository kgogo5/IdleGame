using UnityEngine;
using IdleGame.Core;

namespace IdleGame.Core
{
    /// <summary>
    /// 스테이지 변경 이벤트를 받아 배경 스프라이트를 교체한다.
    /// UIBoot에서 생성되며, GameBackground SpriteRenderer를 찾아 참조한다.
    /// </summary>
    public class BackgroundManager : MonoBehaviour
    {
        public static BackgroundManager Instance { get; private set; }

        private SpriteRenderer _sr;
        private string _loadedPath;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            var bgObj = GameObject.Find("GameBackground");
            if (bgObj != null) _sr = bgObj.GetComponent<SpriteRenderer>();

            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnStageChanged += OnStageChanged;
                ApplyForStage(MonsterManager.Instance.Stage);
            }
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
                MonsterManager.Instance.OnStageChanged -= OnStageChanged;
        }

        private void OnStageChanged(int stage) => ApplyForStage(stage);

        private void ApplyForStage(int stage)
        {
            var cfg = MonsterManager.Instance?.GetConfigForStage(stage);
            if (cfg != null && !string.IsNullOrEmpty(cfg.backgroundPath))
                SetBackground(cfg.backgroundPath);
        }

        public void SetBackground(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath) || resourcePath == _loadedPath) return;

            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(resourcePath);
                if (tex != null)
                    sprite = Sprite.Create(tex,
                        new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f));
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[BackgroundManager] 스프라이트 로드 실패: Resources/{resourcePath}");
                return;
            }

            if (_sr != null)
            {
                _sr.sprite = sprite;
                _loadedPath = resourcePath;
            }
        }
    }
}
