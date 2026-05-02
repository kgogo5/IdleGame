using System;
using System.Collections;
using UnityEngine;
using IdleGame.Data;
using IdleGame.Core;

namespace IdleGame.Managers
{
    public class PlayerStats : MonoBehaviour
    {
        public static PlayerStats Instance { get; private set; }

        [Header("기본 스탯")]
        [SerializeField] private double _baseClickDamage     = 10;
        [SerializeField] private double _baseAttackSpeed     = 2;   // 클릭 공격 횟수/초
        [SerializeField] private double _baseAutoDamage      = 0;   // 자동공격 1회 데미지
        [SerializeField] private double _baseAutoAttackSpeed = 1;   // 자동공격 횟수/초
        [SerializeField] private double _baseGoldMultiplier  = 1;

        // 업그레이드 플랫 보너스
        private double _clickDamageFlat;
        private double _attackSpeedFlat;
        private double _autoDamageFlat;
        private double _autoAttackSpeedFlat;
        private double _goldMultiplierFlat;

        // 아이템/장비 % 보너스
        private double _clickDamagePct;
        private double _attackSpeedPct;
        private double _autoDamagePct;
        private double _autoAttackSpeedPct;
        private double _goldMultiplierPct;
        private double _dropRatePct;
        private double _bossSpawnRatePct;

        public double ClickDamage     => (_baseClickDamage     + _clickDamageFlat)     * (1 + _clickDamagePct);
        public double AutoDamage      => (_baseAutoDamage      + _autoDamageFlat)      * (1 + _autoDamagePct);
        public double GoldMultiplier  => (_baseGoldMultiplier  + _goldMultiplierFlat)  * (1 + _goldMultiplierPct);

        // 공격속도: 최소/최대 클램프
        public double AttackSpeed     => Math.Clamp(
            (_baseAttackSpeed     + _attackSpeedFlat)     * (1 + _attackSpeedPct),     0.5, 20.0);
        public double AutoAttackSpeed => Math.Clamp(
            (_baseAutoAttackSpeed + _autoAttackSpeedFlat) * (1 + _autoAttackSpeedPct), 0.1, 10.0);

        public float  ClickCooldown      => (float)(1.0 / AttackSpeed);
        public float  AutoAttackInterval => (float)(1.0 / AutoAttackSpeed);

        public double DropRateMultiplier => 1.0 + _dropRatePct;
        public double BossSpawnRateBonus => _bossSpawnRatePct;

        // 전투력: 클릭DPS + 자동DPS
        public double CombatPower => ClickDamage * AttackSpeed + AutoDamage * AutoAttackSpeed;

        // 업그레이드(스킬) 플랫 보너스 — 스킬 패널 상태창용
        public double UpgradeClickDamage     => _clickDamageFlat;
        public double UpgradeAttackSpeed     => _attackSpeedFlat;
        public double UpgradeAutoDamage      => _autoDamageFlat;
        public double UpgradeAutoAttackSpeed => _autoAttackSpeedFlat;
        public double UpgradeGoldMultiplier  => _goldMultiplierFlat;

        // 장비 % 보너스 합계 — 장비 패널 상태창용
        public double EquipClickDamagePct     => _clickDamagePct;
        public double EquipAttackSpeedPct     => _attackSpeedPct;
        public double EquipAutoDamagePct      => _autoDamagePct;
        public double EquipAutoAttackSpeedPct => _autoAttackSpeedPct;
        public double EquipGoldMultiplierPct  => _goldMultiplierPct;
        public double EquipDropRatePct        => _dropRatePct;

        public event Action OnStatsChanged;

        private float _lastClickTime = -999f;

        public bool TryConsumeClick()
        {
            if (Time.time - _lastClickTime < ClickCooldown) return false;
            _lastClickTime = Time.time;
            return true;
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() => StartCoroutine(AutoAttackLoop());

        public void AddBonus(StatType type, double amount)
        {
            switch (type)
            {
                case StatType.ClickDamage:     _clickDamageFlat     += amount; break;
                case StatType.AttackSpeed:     _attackSpeedFlat     += amount; break;
                case StatType.AutoDamage:      _autoDamageFlat      += amount; break;
                case StatType.AutoAttackSpeed: _autoAttackSpeedFlat += amount; break;
                case StatType.GoldMultiplier:  _goldMultiplierFlat  += amount; break;
            }
            OnStatsChanged?.Invoke();
        }

        public void RemoveBonus(StatType type, double amount) => AddBonus(type, -amount);

        public void AddEquipModifier(StatType type, float percent)
        {
            switch (type)
            {
                case StatType.ClickDamage:     _clickDamagePct     += percent; break;
                case StatType.AttackSpeed:     _attackSpeedPct     += percent; break;
                case StatType.AutoDamage:      _autoDamagePct      += percent; break;
                case StatType.AutoAttackSpeed: _autoAttackSpeedPct += percent; break;
                case StatType.GoldMultiplier:  _goldMultiplierPct  += percent; break;
                case StatType.DropRate:        _dropRatePct        += percent; break;
                case StatType.BossSpawnRate:   _bossSpawnRatePct   += percent; break;
            }
            OnStatsChanged?.Invoke();
        }

        public void ResetEquipModifiers()
        {
            _clickDamagePct = _attackSpeedPct = _autoDamagePct = _autoAttackSpeedPct = _goldMultiplierPct = 0;
            _dropRatePct = _bossSpawnRatePct = 0;
            OnStatsChanged?.Invoke();
        }

        public void ResetBonuses()
        {
            _clickDamageFlat = _attackSpeedFlat = _autoDamageFlat = _autoAttackSpeedFlat = _goldMultiplierFlat = 0;
            _clickDamagePct  = _attackSpeedPct  = _autoDamagePct  = _autoAttackSpeedPct  = _goldMultiplierPct  = 0;
            _dropRatePct = _bossSpawnRatePct = 0;
            OnStatsChanged?.Invoke();
        }

        // 프레임 단위 타이머로 공격속도 변화에 즉각 반응
        private IEnumerator AutoAttackLoop()
        {
            float elapsed = 0f;
            while (true)
            {
                yield return null;
                elapsed += Time.deltaTime;
                if (elapsed >= AutoAttackInterval)
                {
                    elapsed -= AutoAttackInterval;
                    if (AutoDamage > 0)
                        MonsterManager.Instance?.CurrentMonster?.TakeDamage(AutoDamage);
                }
            }
        }
    }
}
