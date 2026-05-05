using System;
using UnityEngine;
using IdleGame.Core;
using IdleGame.Data;
using IdleGame.Managers;

namespace IdleGame.Core
{
    public class MonsterManager : MonoBehaviour
    {
        public static MonsterManager Instance { get; private set; }

        [SerializeField] private GameObject _monsterPrefab;
        [SerializeField] private MonsterData[] _monsterDataList;    // 스테이지 설정 없을 때 기본 풀
        [SerializeField] private Transform _spawnPoint;

        [Header("스테이지별 설정 (배경/BGM/몬스터)")]
        [SerializeField] private StageConfig[] _stageConfigs;

        [Header("기본 보스 (StageConfig.bossMonster가 없을 때 사용)")]
        [SerializeField] private MonsterData _defaultBossData;

        private bool _forceNormal = false;

        private MonsterData _fallbackBossData; // Inspector에 보스가 없을 때 코드 폴백

        public double FleeCost => CurrencyManager.Instance != null
            ? System.Math.Max(1, CurrencyManager.Instance.Gold * 0.3)
            : 0;
        public bool CanFlee() => CurrencyManager.Instance != null;

        public int Stage { get; private set; } = 1;
        public int MaxStageReached { get; private set; } = 1;
        public Monster CurrentMonster { get; private set; }

        public event Action<Monster> OnMonsterSpawned;
        public event Action<int> OnStageChanged;
        public event Action<int> OnMaxStageChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            MaxStageReached = PlayerPrefs.GetInt("maxStageReached", 1);
            Stage = PlayerPrefs.GetInt("currentStage", 1);
            if (Stage > MaxStageReached) Stage = MaxStageReached;

            if (_defaultBossData == null)
                CreateFallbackBoss();
        }

        // Inspector에 보스 에셋이 없을 때 코드로 임시 생성 (하위 호환)
        private void CreateFallbackBoss()
        {
            var cfg = Data.GameConfig.Get();
            _fallbackBossData = ScriptableObject.CreateInstance<MonsterData>();
            _fallbackBossData.name        = "드래곤";
            _fallbackBossData.monsterName = "드래곤";
            _fallbackBossData.maxHealth   = cfg.fallbackBossMaxHealth;
            _fallbackBossData.goldReward  = cfg.bossGoldPerStage;
            _fallbackBossData.isBoss      = true;
            _fallbackBossData.regenPerSecond    = cfg.fallbackBossRegenPerSecond;
            _fallbackBossData.sprite            = Resources.Load<Sprite>("Monsters/icedragon");
            _fallbackBossData.tintColor         = new Color(0.9f, 0.2f, 0.1f);
            _fallbackBossData.damageFlashColor  = new Color(1f, 0.5f, 0f);
            _fallbackBossData.spriteSize        = new Vector2(2f, 2f);
            _fallbackBossData.dropChance        = cfg.fallbackBossDropChance;
            _fallbackBossData.normalWeight      = 0f;
            _fallbackBossData.rareWeight        = cfg.fallbackBossRareWeight;
            _fallbackBossData.uniqueWeight      = cfg.fallbackBossUniqueWeight;
            _fallbackBossData.legendaryWeight   = cfg.fallbackBossLegendaryWeight;
        }

        private MonsterData GetBossData()
        {
            var cfg = GetConfigForStage(Stage);
            if (cfg?.bossMonster != null) return cfg.bossMonster;
            return _defaultBossData != null ? _defaultBossData : _fallbackBossData;
        }

        // 현재 스테이지에 해당하는 StageConfig 반환 (없으면 null)
        public StageConfig GetConfigForStage(int stage)
        {
            if (_stageConfigs == null) return null;
            foreach (var cfg in _stageConfigs)
                if (stage >= cfg.stageFrom && stage <= cfg.stageTo) return cfg;
            return null;
        }

        private void ApplyStageEnvironment(int stage)
        {
            var cfg = GetConfigForStage(stage);
            if (cfg == null) return;
            if (!string.IsNullOrEmpty(cfg.backgroundPath))
                BackgroundManager.Instance?.SetBackground(cfg.backgroundPath);
            if (!string.IsNullOrEmpty(cfg.bgmPath))
                AudioManager.Instance?.PlayBgmByPath(cfg.bgmPath);
        }

        private void Start()
        {
            _forceNormal = true; // 게임 시작 첫 몬스터는 보스 불가
            ApplyStageEnvironment(Stage);
            SpawnMonster();
        }

        public void OnMonsterKilled()
        {
            // 보스를 처치했을 때만 스테이지 진행
            if (CurrentMonster != null && CurrentMonster.IsBoss)
            {
                Stage++;
                PlayerPrefs.SetInt("currentStage", Stage);

                if (Stage > MaxStageReached)
                {
                    MaxStageReached = Stage;
                    PlayerPrefs.SetInt("maxStageReached", MaxStageReached);
                    OnMaxStageChanged?.Invoke(MaxStageReached);
                }

                ApplyStageEnvironment(Stage);
                OnStageChanged?.Invoke(Stage);
            }
            SpawnMonster();
        }

        public void ResetData()
        {
            Stage = 1;
            MaxStageReached = 1;
            _forceNormal = false;

            if (CurrentMonster != null)
            {
                Destroy(CurrentMonster.gameObject);
                CurrentMonster = null;
            }

            ApplyStageEnvironment(Stage);
            OnStageChanged?.Invoke(Stage);
            OnMaxStageChanged?.Invoke(MaxStageReached);
            SpawnMonster();
        }

        public void SelectStage(int stage)
        {
            if (stage < 1 || stage > MaxStageReached) return;
            Stage = stage;
            _forceNormal = false;
            PlayerPrefs.SetInt("currentStage", Stage);
            ApplyStageEnvironment(Stage);
            OnStageChanged?.Invoke(Stage);
            // 현재 몬스터는 유지 — 다음 스폰부터 새 스테이지 적용
        }

        public void Flee()
        {
            if (!CanFlee()) return;
            CurrencyManager.Instance.SpendGold(FleeCost);

            bool fleedFromBoss = CurrentMonster != null && CurrentMonster.IsBoss;

            if (CurrentMonster != null)
            {
                Destroy(CurrentMonster.gameObject);
                CurrentMonster = null;
            }

            _forceNormal = true; // 도망 직후 한 마리는 반드시 잡몹

            SpawnMonster();
        }

        public void SpawnMonster()
        {
            // StageConfig에 몬스터 풀이 설정되어 있으면 우선 사용, 없으면 기본 풀
            var cfg = GetConfigForStage(Stage);
            MonsterData[] pool = (cfg?.monsters != null && cfg.monsters.Length > 0)
                ? cfg.monsters
                : _monsterDataList;

            if (pool == null || pool.Length == 0)
            {
                Debug.LogError("No monster data available!");
                return;
            }

            float bossChance = Data.GameConfig.Get().bossSpawnChance + (float)(PlayerStats.Instance?.BossSpawnRateBonus ?? 0);
            bool isBossSpawn = !_forceNormal && (UnityEngine.Random.value < bossChance);
            _forceNormal = false;

            MonsterData bossData = GetBossData();
            MonsterData data = isBossSpawn
                ? bossData
                : pool[UnityEngine.Random.Range(0, pool.Length)];

            double hp   = isBossSpawn ? bossData.maxHealth : data.maxHealth;
            double gold = isBossSpawn ? Data.GameConfig.Get().bossGoldPerStage * Stage : GetGoldReward(Stage);

            Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            GameObject monsterObj = Instantiate(_monsterPrefab, spawnPos, Quaternion.identity);
            Monster monster = monsterObj.GetComponent<Monster>();
            monster.Setup(data, hp, gold);
            CurrentMonster = monster;

            OnMonsterSpawned?.Invoke(monster);
        }

        public static double GetGoldReward(int stage) => Data.GameConfig.Get().goldPerStage * stage;
    }
}
