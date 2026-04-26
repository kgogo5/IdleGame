using System;
using System.Collections;
using UnityEngine;
using IdleGame.Data;
using IdleGame.Core;

namespace IdleGame.Managers
{
    // 모든 플레이어 스탯의 단일 진실 원천
    // 업그레이드/아이템은 AddBonus()로 여기에 보너스를 쌓기만 함
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("기본 스탯 (레벨 0 기준)")]
        [SerializeField] private double _baseClickDamage    = 10;
        [SerializeField] private double _baseAutoDPS        = 0;
        [SerializeField] private double _baseGoldMultiplier = 1;

        // 업그레이드 + 아이템으로 쌓인 보너스
        private double _clickDamageBonus;
        private double _autoDPSBonus;
        private double _goldMultiplierBonus;

        // 최종 스탯 (기본 + 보너스)
        public double ClickDamage    => _baseClickDamage    + _clickDamageBonus;
        public double AutoDPS        => _baseAutoDPS        + _autoDPSBonus;
        public double GoldMultiplier => _baseGoldMultiplier + _goldMultiplierBonus;

        public event Action OnStatsChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            StartCoroutine(AutoAttackLoop());
        }

        // 업그레이드/아이템이 보너스를 추가할 때 호출
        public void AddBonus(StatType type, double amount)
        {
            switch (type)
            {
                case StatType.ClickDamage:    _clickDamageBonus    += amount; break;
                case StatType.AutoDPS:        _autoDPSBonus        += amount; break;
                case StatType.GoldMultiplier: _goldMultiplierBonus += amount; break;
            }
            OnStatsChanged?.Invoke();
        }

        public void RemoveBonus(StatType type, double amount) => AddBonus(type, -amount);

        public void ResetBonuses()
        {
            _clickDamageBonus    = 0;
            _autoDPSBonus        = 0;
            _goldMultiplierBonus = 0;
            OnStatsChanged?.Invoke();
        }

        // 자동 공격: AutoDPS만큼 1초마다 현재 몬스터 공격
        private IEnumerator AutoAttackLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (AutoDPS <= 0) continue;
                MonsterManager.Instance?.CurrentMonster?.TakeDamage(AutoDPS);
            }
        }
    }
}
