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

            // ── 초원 스프라이트 ───────────────────────────────────────────
            Sprite GS(string name)
            {
                var spr = Resources.Load<Sprite>($"Monsters/grassland/{name}_default_nobg");
                if (spr == null) Debug.LogWarning($"[MonsterManager] 스프라이트 로드 실패: {name}");
                return spr;
            }
            Sprite GSHit(string name) => Resources.Load<Sprite>($"Monsters/grassland/{name}_hit_nobg");

            MonsterData MakeMon(string mname, double hp, double gold, Color tint, Sprite spr,
                                bool isBoss = false, float dropChance = 0.3f,
                                float regen = 0f, Vector2? size = null, Sprite hitSpr = null)
            {
                var d = ScriptableObject.CreateInstance<MonsterData>();
                d.name = mname; d.monsterName = mname;
                d.maxHealth = hp; d.goldReward = gold;
                d.tintColor = tint; d.sprite = spr;
                d.hitSprite = hitSpr;
                d.isBoss = isBoss; d.dropChance = dropChance;
                d.regenPerSecond = regen;
                d.spriteSize = size ?? Vector2.one;
                if (isBoss) { d.normalWeight = 0f;  d.rareWeight = 60f; d.uniqueWeight = 30f; d.legendaryWeight = 10f; }
                else        { d.normalWeight = 70f; d.rareWeight = 25f; d.uniqueWeight =  4f; d.legendaryWeight =  1f; }
                return d;
            }

            StageConfig MakeStage(int from, int to, string envKey, string envName,
                                  MonsterData[] mons, MonsterData boss)
            {
                var s = ScriptableObject.CreateInstance<StageConfig>();
                s.stageFrom = from; s.stageTo = to;
                s.environmentKey  = envKey;
                s.environmentName = envName;
                s.monsters = mons; s.bossMonster = boss;
                s.backgroundPaths = new[]
                {
                    $"Backgrounds/{envKey}/normal_1",
                    $"Backgrounds/{envKey}/normal_2",
                    $"Backgrounds/{envKey}/normal_3",
                };
                s.bossBgPath = $"Backgrounds/{envKey}/boss";
                return s;
            }

            // ── Zone 1: 초원 (grassland) · Stage 1-10 ────────────────────
            var g1  = MakeMon("이슬 개구리",        80,   25, Color.white, GS("dew_frog"),              hitSpr:GSHit("dew_frog"));
            var g2  = MakeMon("새싹 거북",         100,   30, Color.white, GS("sprout_turtle"),         hitSpr:GSHit("sprout_turtle"));
            var g3  = MakeMon("바람개비 요정",      120,   35, Color.white, GS("pinwheel_sprite"),       hitSpr:GSHit("pinwheel_sprite"));
            var g4  = MakeMon("솜털 양치기",        150,   45, Color.white, GS("fluffy_shepherd"),       hitSpr:GSHit("fluffy_shepherd"));
            var g5  = MakeMon("달팽이 기사",        180,   55, Color.white, GS("snail_knight"),          hitSpr:GSHit("snail_knight"));
            var g6  = MakeMon("두더지 용사",        220,   65, Color.white, GS("mole_warrior"),          hitSpr:GSHit("mole_warrior"));
            var g7  = MakeMon("황금 풍뎅이",        260,   80, Color.white, GS("golden_scarab"),         hitSpr:GSHit("golden_scarab"));
            var g8  = MakeMon("바람 스라소니",      310,   95, Color.white, GS("wind_lynx"),             hitSpr:GSHit("wind_lynx"));
            var g9  = MakeMon("초원 아홀로틀",      370,  115, Color.white, GS("axolotl-type_amphibian"),hitSpr:GSHit("axolotl-type_amphibian"));
            var g10 = MakeMon("초원 두더지 골렘",   440,  140, Color.white, GS("mole_golem"),            hitSpr:GSHit("mole_golem"));
            var gb  = MakeMon("초원의 재앙 거대독사왕", 3000, 800, Color.white, GS("giant_serpent_king"),
                              isBoss:true, dropChance:0.7f, regen:6f, size:new Vector2(1.2f,1.2f), hitSpr:GSHit("giant_serpent_king"));

            // ── Zone 2: 숲 (forest) · Stage 11-20 ────────────────────────
            Sprite FS(string name)    => Resources.Load<Sprite>($"Monsters/forest/{name}_default_nobg");
            Sprite FSHit(string name) => Resources.Load<Sprite>($"Monsters/forest/{name}_hit_nobg");

            var f1  = MakeMon("이슬 완보",          500,   100, Color.white, FS("tardigrade"),               hitSpr:FSHit("tardigrade"));
            var f2  = MakeMon("통나무 콩벌레",       650,   130, Color.white, FS("isopod"),                   hitSpr:FSHit("isopod"));
            var f3  = MakeMon("도토리 카피바라",      800,   160, Color.white, FS("capybara-type"),             hitSpr:FSHit("capybara-type"));
            var f4  = MakeMon("팡팡 씨방이",        1000,   200, Color.white, FS("seed_pod_organism"),         hitSpr:FSHit("seed_pod_organism"));
            var f5  = MakeMon("쌍둥이 덩굴버섯",    1250,   250, Color.white, FS("parasitic_twin_organism"),   hitSpr:FSHit("parasitic_twin_organism"));
            var f6  = MakeMon("이끼 덩어리 요정",   1500,   300, Color.white, FS("lichen_composite"),          hitSpr:FSHit("lichen_composite"));
            var f7  = MakeMon("수정 산호바위",      1800,   360, Color.white, FS("mineral-coral_hybrid"),      hitSpr:FSHit("mineral-coral_hybrid"));
            var f8  = MakeMon("날개막 다람쥐별",    2100,   420, Color.white, FS("gliding_membrane_creature"), hitSpr:FSHit("gliding_membrane_creature"));
            var f9  = MakeMon("흑요석 살쾡이",      2500,   500, Color.white, FS("obsidian-skinned_predator"), hitSpr:FSHit("obsidian-skinned_predator"));
            var f10 = MakeMon("꽃갓 해파리나무",    3000,   600, Color.white, FS("plant-jellyfish_hybrid"),    hitSpr:FSHit("plant-jellyfish_hybrid"));
            var f11 = MakeMon("석회껍질 달팽이 요정",3500,  700, Color.white, FS("calcite_shell_creature"),    hitSpr:FSHit("calcite_shell_creature"));
            var f12 = MakeMon("먹물 나무갑오징어",  4000,   800, Color.white, FS("cuttlefish"),                hitSpr:FSHit("cuttlefish"));
            var fb  = MakeMon("천년 철갑 멧돼지왕", 30000, 4000, Color.white, FS("iron_armored_boar_king"),
                              isBoss:true, dropChance:0.75f, regen:24f, size:new Vector2(1.2f,1.2f), hitSpr:FSHit("iron_armored_boar_king"));

            // ── Zone 3: 사막 (desert) · Stage 21-30 ──────────────────────
            var d1 = MakeMon("모래 전갈",      2500,    350, new Color(0.9f, 0.7f, 0.2f), slime);
            var d2 = MakeMon("사막 미라",      3200,    450, new Color(0.8f, 0.7f, 0.5f), slime, regen:5f);
            var d3 = MakeMon("사막 뱀",        4000,    550, new Color(0.9f, 0.5f, 0.1f), slime);
            var db = MakeMon("모래 군주",     30000,   6000, new Color(0.9f, 0.6f, 0.0f), slime,
                             isBoss:true, dropChance:0.8f, regen:72f, size:new Vector2(1.2f,1.2f));

            // ── Zone 4: 동굴 (cave) · Stage 31-40 ────────────────────────
            var c1 = MakeMon("동굴 박쥐",     10000,   1200, new Color(0.4f, 0.3f, 0.5f), slime);
            var c2 = MakeMon("바위 골렘",     13000,   1600, new Color(0.5f, 0.5f, 0.5f), slime, regen:15f);
            var c3 = MakeMon("동굴 거미",     16000,   2000, new Color(0.3f, 0.2f, 0.4f), slime);
            var cb = MakeMon("동굴 군주",    120000,  20000, new Color(0.6f, 0.2f, 0.6f), slime,
                             isBoss:true, dropChance:0.82f, regen:240f, size:new Vector2(1.2f,1.2f));

            // ── Zone 5: 설원 (tundra) · Stage 41-50 ──────────────────────
            var t1 = MakeMon("설원 늑대",      45000,   5000, new Color(0.7f, 0.85f, 1.0f), slime);
            var t2 = MakeMon("설인",           60000,   7000, new Color(0.9f, 0.95f, 1.0f), slime, regen:50f);
            var t3 = MakeMon("빙하 정령",      75000,   9000, new Color(0.4f, 0.7f, 1.0f), slime, regen:30f);
            var tb = MakeMon("빙하 거인",     600000,  80000, new Color(0.2f, 0.5f, 0.9f), dragon ?? slime,
                             isBoss:true, dropChance:0.84f, regen:960f, size:new Vector2(1.2f,1.2f));

            // ── Zone 6: 천공 (skyisland) · Stage 51-60 ───────────────────
            var sk1 = MakeMon("바람 정령",    200000,  30000, new Color(0.8f, 0.9f, 1.0f), dragon ?? slime, regen:80f);
            var sk2 = MakeMon("그리핀",       270000,  40000, new Color(0.9f, 0.8f, 0.4f), dragon ?? slime);
            var sk3 = MakeMon("하르피아",     340000,  50000, new Color(0.6f, 0.7f, 0.9f), dragon ?? slime);
            var skb = MakeMon("천공의 군주", 2500000, 350000, new Color(0.5f, 0.8f, 1.0f), dragon ?? slime,
                              isBoss:true, dropChance:0.86f, regen:3600f, size:new Vector2(1.2f,1.2f));

            // ── Zone 7: 고대 유적 (ruins) · Stage 61-70 ──────────────────
            var r1 = MakeMon("미이라 병사",      900000,   150000, new Color(0.8f, 0.75f, 0.5f), dragon ?? slime, regen:200f);
            var r2 = MakeMon("돌 파수꾼",       1200000,   200000, new Color(0.6f, 0.6f, 0.55f), dragon ?? slime, regen:300f);
            var r3 = MakeMon("유적 악령",       1500000,   250000, new Color(0.5f, 0.3f, 0.7f),  dragon ?? slime);
            var rb = MakeMon("고대 수호신",    12000000,  2000000, new Color(0.9f, 0.8f, 0.3f),  dragon ?? slime,
                             isBoss:true, dropChance:0.88f, regen:18000f, size:new Vector2(1.2f,1.2f));

            // ── Zone 8: 마계 (demon) · Stage 71-80 ───────────────────────
            var dm1 = MakeMon("마계 임프",     4000000,    700000, new Color(0.8f, 0.2f, 0.2f), dragon ?? slime);
            var dm2 = MakeMon("악마 기사",     5500000,    950000, new Color(0.6f, 0.1f, 0.1f), dragon ?? slime, regen:1000f);
            var dm3 = MakeMon("마계 마법사",   7000000,   1200000, new Color(0.7f, 0.1f, 0.5f), dragon ?? slime, regen:800f);
            var dmb = MakeMon("마계 군주",    60000000,  10000000, new Color(0.9f, 0.1f, 0.1f), dragon ?? slime,
                              isBoss:true, dropChance:0.9f, regen:96000f, size:new Vector2(1.2f,1.2f));

            // ── Zone 9: 공허 (void) · Stage 81-90 ────────────────────────
            var v1 = MakeMon("공허 유령",      20000000,   4000000, new Color(0.4f, 0.3f, 0.6f), dragon ?? slime, regen:3000f);
            var v2 = MakeMon("혼돈 정령",      28000000,   5500000, new Color(0.2f, 0.1f, 0.5f), dragon ?? slime, regen:4000f);
            var v3 = MakeMon("공허 사냥꾼",    36000000,   7000000, new Color(0.5f, 0.2f, 0.8f), dragon ?? slime);
            var vb = MakeMon("공허의 지배자", 300000000,  50000000, new Color(0.3f, 0.0f, 0.6f), dragon ?? slime,
                             isBoss:true, dropChance:0.92f, regen:480000f, size:new Vector2(1.2f,1.2f));

            // ── Zone 10: 심연 (abyss) · Stage 91+ ────────────────────────
            var a1 = MakeMon("심연 괴물",      100000000,   20000000, new Color(0.1f, 0.1f, 0.2f),  dragon ?? slime, regen:10000f);
            var a2 = MakeMon("고대의 존재",    150000000,   30000000, new Color(0.0f, 0.05f, 0.15f), dragon ?? slime, regen:15000f);
            var a3 = MakeMon("심연 군주",      200000000,   40000000, new Color(0.05f, 0.0f, 0.1f),  dragon ?? slime, regen:20000f);
            var ab = MakeMon("심연의 신",     1500000000,  250000000, new Color(0.0f, 0.0f, 0.05f),  dragon ?? slime,
                             isBoss:true, dropChance:1.0f, regen:2400000f, size:new Vector2(1.2f,1.2f));

            _stageConfigs = new[]
            {
                MakeStage( 1,  1, "grassland", "초원",      new[]{g1,g2,g3,g4,g5,g6,g7,g8,g9,g10}, gb),
                MakeStage( 2,  2, "forest",    "숲",        new[]{f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12}, fb),
                MakeStage( 3,  3, "desert",    "사막",      new[]{d1,d2,d3},    db),
                MakeStage( 4,  4, "cave",      "동굴",      new[]{c1,c2,c3},    cb),
                MakeStage( 5,  5, "tundra",    "설원",      new[]{t1,t2,t3},    tb),
                MakeStage( 6,  6, "skyisland", "천공",      new[]{sk1,sk2,sk3}, skb),
                MakeStage( 7,  7, "ruins",     "고대 유적", new[]{r1,r2,r3},    rb),
                MakeStage( 8,  8, "demon",     "마계",      new[]{dm1,dm2,dm3}, dmb),
                MakeStage( 9,  9, "void",      "공허",      new[]{v1,v2,v3},    vb),
                MakeStage(10, 99, "abyss",     "심연",      new[]{a1,a2,a3},    ab),
            };

            // Inspector 기본 풀이 비어있으면 1구간 몬스터를 폴백으로 사용
            if (!hasPool)
                _monsterDataList = new[] { g1, g2, g3, g4, g5, g6, g7, g8, g9, g10 };
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
            bool wasBoss    = CurrentMonster != null && CurrentMonster.IsBoss;
            bool isNewClear = wasBoss && Stage >= MaxStageReached;

            if (isNewClear)
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

        /// <summary>개발자 전용: MaxStageReached 제한 없이 임의 스테이지로 이동 + 즉시 스폰</summary>
        public void JumpToStage(int stage)
        {
            if (stage < 1) return;
            Stage = stage;
            if (stage > MaxStageReached)
            {
                MaxStageReached = stage;
                PlayerPrefs.SetInt("maxStageReached", MaxStageReached);
                OnMaxStageChanged?.Invoke(MaxStageReached);
            }
            _forceNormal = true;
            PlayerPrefs.SetInt("currentStage", Stage);
            ApplyStageEnvironment(Stage);
            OnStageChanged?.Invoke(Stage);

            if (CurrentMonster != null)
            {
                Destroy(CurrentMonster.gameObject);
                CurrentMonster = null;
            }
            SpawnMonster();
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
