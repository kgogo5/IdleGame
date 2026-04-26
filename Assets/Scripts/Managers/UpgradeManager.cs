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

        [SerializeField] private UpgradeData[] _upgrades;

        private readonly Dictionary<string, int> _levels = new();

        public UpgradeData[] Upgrades => _upgrades;
        public event Action OnUpgradePurchased;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (_upgrades == null || _upgrades.Length == 0 || _upgrades[0] == null)
                CreateDefaultUpgrades();
        }

        private void Start() => Load();

        private void CreateDefaultUpgrades()
        {
            var list = new System.Collections.Generic.List<UpgradeData>();

            UpgradeData Make(string uname, string desc, double cost, float mul, int max, StatType stat, double eff)
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
                return d;
            }

            list.Add(Make("클릭 강화 I",   "+클릭 데미지",        50,   1.15f,  0,  StatType.ClickDamage,    5));
            list.Add(Make("클릭 강화 II",  "+클릭 데미지(대)",   300,   1.15f,  0,  StatType.ClickDamage,   15));
            list.Add(Make("멀티 히트",     "+클릭 데미지(강)",  2000,   1.20f, 20,  StatType.ClickDamage,   50));
            list.Add(Make("자동 타격",     "+자동 공격",         200,   1.15f,  0,  StatType.AutoDPS,        2));
            list.Add(Make("독 안개",       "+자동 공격(중)",     800,   1.18f,  0,  StatType.AutoDPS,        8));
            list.Add(Make("자동 포탑",     "+자동 공격(강)",   5000,   1.20f,  0,  StatType.AutoDPS,       20));
            list.Add(Make("골드 감각",     "+골드 배율",         500,   1.20f,  0,  StatType.GoldMultiplier, 0.1));
            list.Add(Make("행운",          "+골드 배율(강)",   1500,   1.25f, 10,  StatType.GoldMultiplier, 0.05));

            _upgrades = list.ToArray();
        }

        // 현재 레벨 조회
        public int GetLevel(UpgradeData upgrade) =>
            _levels.TryGetValue(upgrade.name, out int l) ? l : 0;

        // 다음 레벨 구매 비용
        public double GetNextCost(UpgradeData upgrade) =>
            upgrade.GetCost(GetLevel(upgrade));

        // 구매 가능 여부
        public bool CanBuy(UpgradeData upgrade)
        {
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
                // 이전 게임의 효과 복원
                PlayerStats.Instance.AddBonus(upgrade.statType, upgrade.effectPerLevel * level);
            }
        }
    }
}
