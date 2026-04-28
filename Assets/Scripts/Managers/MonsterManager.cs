using System;
using UnityEngine;
using IdleGame.Core;
using IdleGame.Data;

namespace IdleGame.Core
{
    public class MonsterManager : MonoBehaviour
    {
        public static MonsterManager Instance { get; private set; }

        [SerializeField] private GameObject _monsterPrefab;
        [SerializeField] private MonsterData[] _monsterDataList;
        [SerializeField] private Transform _spawnPoint;

        private const int KILLS_PER_STAGE = 10;
        private int _killsInStage = 0;

        public int Stage { get; private set; } = 1;
        public Monster CurrentMonster { get; private set; }

        public event Action<Monster> OnMonsterSpawned;
        public event Action<int> OnStageChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            SpawnMonster();
        }

        public void OnMonsterKilled()
        {
            _killsInStage++;
            if (_killsInStage >= KILLS_PER_STAGE)
            {
                _killsInStage = 0;
                Stage++;
                OnStageChanged?.Invoke(Stage);
            }
            SpawnMonster();
        }

        public void SpawnMonster()
        {
            if (_monsterDataList == null || _monsterDataList.Length == 0)
            {
                Debug.LogError("No monster data available!");
                return;
            }

            int idx = UnityEngine.Random.Range(0, _monsterDataList.Length);
            MonsterData data = _monsterDataList[idx];

            double hp = GetMonsterHp(Stage);
            double gold = GetGoldReward(Stage);

            Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            GameObject monsterObj = Instantiate(_monsterPrefab, spawnPos, Quaternion.identity);
            Monster monster = monsterObj.GetComponent<Monster>();
            monster.Setup(data, hp, gold);
            CurrentMonster = monster;

            OnMonsterSpawned?.Invoke(monster);
        }

        public static double GetMonsterHp(int stage) => 100.0 * Math.Pow(1.5, stage - 1);
        public static double GetGoldReward(int stage) => 10.0 * stage;
    }
}
