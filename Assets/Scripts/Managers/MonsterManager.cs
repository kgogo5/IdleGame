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
        [SerializeField] private MonsterData[] _monsterDataList;
        [SerializeField] private Transform _spawnPoint;

        private const float BOSS_SPAWN_CHANCE = 0.03f; // 3% 확률로 보스 등장
        private bool _forceNormal = false;

        private MonsterData _bossData;

        public double FleeCost => CurrencyManager.Instance != null
            ? System.Math.Max(1, CurrencyManager.Instance.Gold * 0.3)
            : 0;
        public bool CanFlee() => CurrencyManager.Instance != null
                              && CurrencyManager.Instance.Gold > 0;

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
            CreateBossData();
        }

        private void CreateBossData()
        {
            _bossData = ScriptableObject.CreateInstance<MonsterData>();
            _bossData.name        = "드래곤";
            _bossData.monsterName = "드래곤";
            _bossData.maxHealth   = 5000.0;
            _bossData.goldReward  = 500.0;
            _bossData.isBoss      = true;
            _bossData.regenPerSecond = 30f;             // 30HP/s — 못 따라가면 못잡음
            _bossData.sprite      = Resources.Load<Sprite>("Monsters/icedragon");
            _bossData.tintColor   = new Color(0.9f, 0.2f, 0.1f);    // 붉은 틴트 (아이스드래곤 → 파이어드래곤)
            _bossData.damageFlashColor = new Color(1f, 0.5f, 0f);
            _bossData.spriteSize  = new Vector2(2f, 2f);
            _bossData.dropChance  = 0.8f;               // 보스는 80% 드랍
            _bossData.normalWeight    = 0f;
            _bossData.rareWeight      = 30f;
            _bossData.uniqueWeight    = 40f;
            _bossData.legendaryWeight = 15f;
            _bossData.customDrops = new Data.DropEntry[]
            {
                new Data.DropEntry { itemId = "소모_드랍부적",   weight = 10f },
                new Data.DropEntry { itemId = "소모_보스소환서", weight = 5f  },
            };
        }

        private void Start()
        {
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
            if (_monsterDataList == null || _monsterDataList.Length == 0)
            {
                Debug.LogError("No monster data available!");
                return;
            }

            float bossChance = BOSS_SPAWN_CHANCE + (float)(PlayerStats.Instance?.BossSpawnRateBonus ?? 0);
            bool isBossSpawn = !_forceNormal && (UnityEngine.Random.value < bossChance);
            _forceNormal = false;
            MonsterData data = isBossSpawn
                ? _bossData
                : _monsterDataList[UnityEngine.Random.Range(0, _monsterDataList.Length)];

            double hp   = isBossSpawn ? _bossData.maxHealth : data.maxHealth;
            double gold = isBossSpawn ? _bossData.goldReward * Stage : GetGoldReward(Stage);

            Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            GameObject monsterObj = Instantiate(_monsterPrefab, spawnPos, Quaternion.identity);
            Monster monster = monsterObj.GetComponent<Monster>();
            monster.Setup(data, hp, gold);
            CurrentMonster = monster;

            OnMonsterSpawned?.Invoke(monster);
        }

        public static double GetGoldReward(int stage) => 25.0 * stage;
    }
}
