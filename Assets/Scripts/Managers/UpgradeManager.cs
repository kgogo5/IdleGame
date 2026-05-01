using System;
using System.Collections.Generic;
using UnityEngine;
using IdleGame.Core;
using IdleGame.Data;

namespace IdleGame.Managers
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private UpgradeData[] _upgrades;

        private readonly Dictionary<string, int> _levels = new();

        public UpgradeData[] Upgrades => _upgrades;
        public event Action OnUpgradePurchased;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateDefaultUpgrades();
        }

        private void Start() => Load();

        private void CreateDefaultUpgrades()
        {
            var list = new System.Collections.Generic.List<UpgradeData>();

            UpgradeData Make(string uname, string desc, double cost, float mul, int max, StatType stat, double eff, int unlock = 1)
            {
                var d = ScriptableObject.CreateInstance<UpgradeData>();
                d.name = uname;
                d.upgradeName = uname;
                d.description = desc;
                d.baseCost = cost;
                d.costMultiplier = mul;
                d.maxLevel = max;
                d.statType = stat;
                d.effectPerLevel = eff;
                d.unlockStage = unlock;
                return d;
            }

            // ── 스테이지 1: 클릭 공격 + 클릭 공속 ───────────────────────
            list.Add(Make("손가락 단련",  "클릭 데미지 +3 / 레벨",                    20,    1.40f,  0, StatType.ClickDamage,      3,   unlock: 1));
            list.Add(Make("손놀림 향상",  "클릭 공속 +0.3회/초 / 레벨 (최대 5레벨)",   50,    1.45f,  5, StatType.AttackSpeed,      0.3, unlock: 1));

            // ── 스테이지 2: 자동 공격 + 자동 공속 ───────────────────────
            list.Add(Make("자동 타격기",  "자동공격 데미지 +5 / 레벨",                 80,    1.42f,  0, StatType.AutoDamage,       5,   unlock: 2));
            list.Add(Make("연사 장치",    "자동공격 속도 +0.2회/초 / 레벨 (최대 5레벨)", 120,   1.45f,  5, StatType.AutoAttackSpeed,  0.2, unlock: 2));

            // ── 스테이지 3: 클릭·자동 중급 강화 ─────────────────────────
            list.Add(Make("강타 훈련",    "클릭 데미지 +10 / 레벨",                    300,   1.50f,  0, StatType.ClickDamage,     10,   unlock: 3));
            list.Add(Make("강화 타격기",  "자동공격 데미지 +6 / 레벨",                  400,   1.50f,  0, StatType.AutoDamage,       6,   unlock: 3));

            // ── 스테이지 4: 골드 + 자동공속 강화 ────────────────────────
            list.Add(Make("금 감지",      "골드 배율 +0.1 / 레벨",                     1000,  1.55f,  0, StatType.GoldMultiplier,  0.1,  unlock: 4));
            list.Add(Make("고속 연사",    "자동공격 속도 +0.4회/초 / 레벨 (최대 6레벨)", 1500,  1.55f,  6, StatType.AutoAttackSpeed,  0.4, unlock: 4));

            // ── 스테이지 5: 고급 공격력 ──────────────────────────────────
            list.Add(Make("일격필살",     "클릭 데미지 +20 / 레벨 (최대 12레벨)",       4000,  1.65f, 12, StatType.ClickDamage,     20,   unlock: 5));
            list.Add(Make("자동 포탑",    "자동공격 데미지 +12 / 레벨",                 5000,  1.65f,  0, StatType.AutoDamage,      12,   unlock: 5));

            // ── 스테이지 6: 최고급 공속 + 골드 ──────────────────────────
            list.Add(Make("초고속 클릭",  "클릭 공속 +0.8회/초 / 레벨 (최대 8레벨)",   12000, 1.70f,  8, StatType.AttackSpeed,     0.8,  unlock: 6));
            list.Add(Make("행운의 손",    "골드 배율 +0.05 / 레벨 (최대 8레벨)",       10000, 1.70f,  8, StatType.GoldMultiplier,  0.05, unlock: 6));

            _upgrades = list.ToArray();
        }

        // 현재 레벨 조회
        public int GetLevel(UpgradeData upgrade) =>
            _levels.TryGetValue(upgrade.name, out int l) ? l : 0;

        // 다음 레벨 구매 비용
        public double GetNextCost(UpgradeData upgrade) =>
            upgrade.GetCost(GetLevel(upgrade));

        public bool IsUnlocked(UpgradeData upgrade) =>
            Core.MonsterManager.Instance == null || Core.MonsterManager.Instance.Stage >= upgrade.unlockStage;

        // 구매 가능 여부
        public bool CanBuy(UpgradeData upgrade)
        {
            if (!IsUnlocked(upgrade)) return false;
            if (upgrade.maxLevel > 0 && GetLevel(upgrade) >= upgrade.maxLevel) return false;
            return CurrencyManager.Instance.CanAfford(GetNextCost(upgrade));
        }

        // 업그레이드 구매
        public bool Buy(UpgradeData upgrade)
        {
            if (!CanBuy(upgrade)) return false;
            if (!CurrencyManager.Instance.SpendGold(GetNextCost(upgrade))) return false;

            _levels[upgrade.name] = GetLevel(upgrade) + 1;
            PlayerStats.Instance.AddBonus(upgrade.statType, upgrade.effectPerLevel);

            Save();
            OnUpgradePurchased?.Invoke();
            return true;
        }

        public void ResetData()
        {
            foreach (var kv in _levels)
                PlayerPrefs.DeleteKey($"upg_{kv.Key}");
            _levels.Clear();
            OnUpgradePurchased?.Invoke();
        }

        public void MaxAllUpgrades()
        {
            if (_upgrades == null) return;
            foreach (var upgrade in _upgrades)
            {
                int current = GetLevel(upgrade);
                int target  = upgrade.maxLevel > 0 ? upgrade.maxLevel : 50;
                if (current >= target) continue;
                _levels[upgrade.name] = target;
                PlayerStats.Instance.AddBonus(upgrade.statType, upgrade.effectPerLevel * (target - current));
            }
            Save();
            OnUpgradePurchased?.Invoke();
        }

        private void Save()
        {
            foreach (var kv in _levels)
                PlayerPrefs.SetInt($"upg_{kv.Key}", kv.Value);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (_upgrades == null) return;
            foreach (var upgrade in _upgrades)
            {
                int level = PlayerPrefs.GetInt($"upg_{upgrade.name}", 0);
                if (level <= 0) continue;
                _levels[upgrade.name] = level;
                PlayerStats.Instance.AddBonus(upgrade.statType, upgrade.effectPerLevel * level);
            }
        }
    }
}
