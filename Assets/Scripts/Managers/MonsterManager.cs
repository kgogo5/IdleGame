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
        [SerializeField] private MonsterData[] _monsterDataList; // 여러 몬스터 지원
        [SerializeField] private Transform _spawnPoint;

        private int _currentMonsterIndex = 0;

        public event Action<Monster> OnMonsterSpawned;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SpawnMonster();
        }

        public void SpawnMonster()
        {
            if (_monsterDataList == null || _monsterDataList.Length == 0)
            {
                Debug.LogError("No monster data available!");
                return;
            }

            // 랜덤 몬스터 선택
            int randomIndex = UnityEngine.Random.Range(0, _monsterDataList.Length);
            MonsterData currentData = _monsterDataList[randomIndex];

            Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
            GameObject monsterObj = Instantiate(_monsterPrefab, spawnPos, Quaternion.identity);
            Monster monster = monsterObj.GetComponent<Monster>();
            monster.Setup(currentData);

            OnMonsterSpawned?.Invoke(monster);

            _currentMonsterIndex++;
        }
    }
}
