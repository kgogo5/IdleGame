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

        // 콘텐츠 관리 패널용 공개 접근자
        public StageConfig[]  StageConfigs      => _stageConfigs;
        public MonsterData[]  DefaultMonsterPool => _monsterDataList;
        public MonsterData    DefaultBossData    => _defaultBossData ?? _fallbackBossData;

        public double FleeCost => CurrencyManager.Instance != null
            ? System.Math.Max(1, CurrencyManager.Instance.Gold * Data.GameConfig.Get().fleeCostRatio)
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

            CreateDefaultContent();
            if (_defaultBossData == null && _fallbackBossData == null)
                CreateFallbackBoss();
        }

        // Inspector 배열이 비어있을 때 스테이지별 기본 몬스터/보스 풀 생성
        private void CreateDefaultContent()
        {
            bool hasStages  = _stageConfigs   != null && _stageConfigs.Length   > 0;
            bool hasPool    = _monsterDataList != null && _monsterDataList.Length > 0;
            if (hasStages && hasPool) return;

            var slime  = Resources.Load<Sprite>("Monsters/slime");
            var dragon = Resources.Load<Sprite>("Monsters/icedragon");

            MonsterData MakeMon(string mname, double hp, double gold, Color tint, Sprite spr,
                                bool isBoss = false, float dropChance = 0.3f,
                                float regen = 0f, Vector2? size = null)
            {
                var d = ScriptableObject.CreateInstance<MonsterData>();
                d.name = mname; d.monsterName = mname;
                d.maxHealth = hp; d.goldReward = gold;
                d.tintColor = tint; d.sprite = spr;
                d.isBoss = isBoss; d.dropChance = dropChance;
                d.regenPerSecond = regen;
                d.spriteSize = size ?? Vector2.one;
                if (isBoss) { d.normalWeight = 0f;  d.rareWeight = 60f; d.uniqueWeight = 30f; d.legendaryWeight = 10f; }
                else        { d.normalWeight = 70f; d.rareWeight = 25f; d.uniqueWeight =  4f; d.legendaryWeight =  1f; }
                return d;
            }

            StageConfig MakeStage(int from, int to, MonsterData[] mons, MonsterData boss, string bg = "")
            {
                var s = ScriptableObject.CreateInstance<StageConfig>();
                s.stageFrom = from; s.stageTo = to;
                s.monsters = mons; s.bossMonster = boss;
                s.backgroundPath = bg;
                return s;
            }

            // ── Stage 1-3: 슬라임 계열 ────────────────────────────────────
            var s1_green  = MakeMon("초록 슬라임",    80,   25, new Color(0.3f, 0.9f, 0.3f), slime);
            var s1_blue   = MakeMon("파란 슬라임",   120,   35, new Color(0.3f, 0.5f, 1.0f), slime);
            var s1_purple = MakeMon("보라 슬라임",   160,   45, new Color(0.7f, 0.2f, 0.9f), slime);
            var b1        = MakeMon("슬라임 왕",    1200,  500, new Color(1.0f, 0.7f, 0.0f), slime,
                                    isBoss: true, dropChance: 0.7f, regen: 5f, size: new Vector2(1.8f, 1.8f));

            // ── Stage 4-6: 오크 계열 ─────────────────────────────────────
            var s2_war    = MakeMon("오크 전사",     400,   80, new Color(0.4f, 0.7f, 0.2f), slime);
            var s2_arch   = MakeMon("오크 궁수",     500,  100, new Color(0.3f, 0.5f, 0.1f), slime);
            var s2_sha    = MakeMon("오크 주술사",   600,  120, new Color(0.5f, 0.2f, 0.6f), slime, regen: 3f);
            var b2        = MakeMon("오크 족장",    4000, 1500, new Color(0.6f, 0.3f, 0.0f), slime,
                                    isBoss: true, dropChance: 0.8f, regen: 15f, size: new Vector2(1.8f, 1.8f));

            // ── Stage 7-9: 골렘 계열 ─────────────────────────────────────
            var s3_stone  = MakeMon("돌 골렘",      1500,  200, new Color(0.6f, 0.6f, 0.6f), slime, regen: 5f);
            var s3_iron   = MakeMon("철 골렘",      2000,  260, new Color(0.4f, 0.4f, 0.5f), slime, regen: 8f);
            var s3_lava   = MakeMon("용암 골렘",    2500,  320, new Color(0.9f, 0.4f, 0.1f), slime, regen: 10f);
            var b3        = MakeMon("골렘 군주",   18000, 4000, new Color(0.8f, 0.2f, 0.1f), slime,
                                    isBoss: true, dropChance: 0.9f, regen: 40f, size: new Vector2(2.0f, 2.0f));

            // ── Stage 10+: 드래곤 계열 ───────────────────────────────────
            var s4_wyv    = MakeMon("와이번",        6000,  500, new Color(0.2f, 0.6f, 0.9f), dragon ?? slime, regen: 10f);
            var s4_fire   = MakeMon("화염 드래곤",   9000,  700, new Color(1.0f, 0.3f, 0.1f), dragon ?? slime, regen: 15f);
            var s4_ice    = MakeMon("얼음 드래곤",  12000,  900, new Color(0.5f, 0.8f, 1.0f), dragon ?? slime, regen: 20f);
            var b4        = MakeMon("드래곤 군주",  80000,12000, new Color(0.9f, 0.1f, 0.5f), dragon ?? slime,
                                    isBoss: true, dropChance: 1.0f, regen: 120f, size: new Vector2(2.2f, 2.2f));

            _stageConfigs = new[]
            {
                MakeStage( 1,  3, new[] { s1_green, s1_blue, s1_purple }, b1, "Backgrounds/stage1_bg"),
                MakeStage( 4,  6, new[] { s2_war,  s2_arch,  s2_sha   }, b2, "Backgrounds/stage2_bg"),
                MakeStage( 7,  9, new[] { s3_stone, s3_iron, s3_lava  }, b3, "Backgrounds/stage3_bg"),
                MakeStage(10, 99, new[] { s4_wyv,  s4_fire,  s4_ice   }, b4),
            };

            // Inspector 기본 풀이 비어있으면 1구간 몬스터를 폴백으로 사용
            if (!hasPool)
                _monsterDataList = new[] { s1_green, s1_blue, s1_purple };
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
