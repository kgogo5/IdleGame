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
                MonsterManager.Instance.OnStageChanged    += OnStageChanged;
                MonsterManager.Instance.OnMonsterSpawned  += OnMonsterSpawned;
                ApplyForMonster(MonsterManager.Instance.CurrentMonster);
            }
        }

        private void OnDestroy()
        {
            if (MonsterManager.Instance != null)
            {
                MonsterManager.Instance.OnStageChanged   -= OnStageChanged;
                MonsterManager.Instance.OnMonsterSpawned -= OnMonsterSpawned;
            }
        }

        private void OnStageChanged(int stage) => ApplyRandom(stage, isBoss: false);

        private void OnMonsterSpawned(Monster monster)
        {
            ApplyForMonster(monster);
        }

        private void ApplyForMonster(Monster monster)
        {
            if (monster == null) return;
            int stage = MonsterManager.Instance?.Stage ?? 1;
            ApplyRandom(stage, isBoss: monster.IsBoss);
        }

        private void ApplyRandom(int stage, bool isBoss)
        {
            var cfg = MonsterManager.Instance?.GetConfigForStage(stage);
            if (cfg == null) return;

            if (isBoss && !string.IsNullOrEmpty(cfg.bossBgPath))
            {
                SetBackground(cfg.bossBgPath);
                return;
            }

            if (cfg.backgroundPaths != null && cfg.backgroundPaths.Length > 0)
            {
                int idx = Random.Range(0, cfg.backgroundPaths.Length);
                SetBackground(cfg.backgroundPaths[idx]);
            }
        }

        public void SetBackground(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return;

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
