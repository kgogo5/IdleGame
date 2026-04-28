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

            // ── 클릭 데미지 ──────────────────────────────────────
            list.Add(Make("손가락 단련",   "클릭할 때마다 데미지가 조금 더 세진다.\n레벨당 클릭 데미지 +5",            50,  1.15f,  0, StatType.ClickDamage,     5));
            list.Add(Make("강타 훈련",     "더 강하게 내려친다. 레벨당 클릭 데미지 +15",                             300,  1.15f,  0, StatType.ClickDamage,    15));
            list.Add(Make("일격필살",      "한 방 한 방이 치명적으로 강해진다.\n레벨당 클릭 데미지 +50 (최대 20레벨)", 2000, 1.20f, 20, StatType.ClickDamage,    50));

            // ── 클릭 공격속도 ─────────────────────────────────────
            list.Add(Make("손놀림 향상",   "손이 더 빨라진다. 레벨당 클릭 공격속도 +0.5회/초 (최대 10레벨)",          400,  1.20f, 10, StatType.AttackSpeed,   0.5));
            list.Add(Make("초고속 클릭",   "눈에 보이지 않는 속도로 클릭한다.\n레벨당 클릭 공격속도 +1.0회/초 (최대 10레벨)", 3000, 1.25f, 10, StatType.AttackSpeed, 1.0));

            // ── 자동공격 데미지 ───────────────────────────────────
            list.Add(Make("자동 타격기",   "자동으로 공격하는 장치를 설치한다.\n레벨당 자동공격 데미지 +2",            200,  1.15f,  0, StatType.AutoDamage,      2));
            list.Add(Make("강화 타격기",   "자동 타격기를 업그레이드한다. 레벨당 자동공격 데미지 +8",                  800,  1.18f,  0, StatType.AutoDamage,      8));
            list.Add(Make("자동 포탑",     "강력한 포탑이 쉬지 않고 공격한다.\n레벨당 자동공격 데미지 +20",           5000,  1.20f,  0, StatType.AutoDamage,     20));

            // ── 자동공격 속도 ─────────────────────────────────────
            list.Add(Make("연사 장치",     "자동 공격 빈도를 높인다.\n레벨당 자동공격 속도 +0.5회/초 (최대 10레벨)",   600,  1.20f, 10, StatType.AutoAttackSpeed, 0.5));
            list.Add(Make("고속 연사",     "자동 공격이 훨씬 빠르게 발사된다.\n레벨당 자동공격 속도 +1.0회/초 (최대 10레벨)", 4000, 1.25f, 10, StatType.AutoAttackSpeed, 1.0));

            // ── 골드 배율 ─────────────────────────────────────────
            list.Add(Make("금 감지",       "몬스터에게서 더 많은 골드를 얻는다.\n레벨당 골드 배율 +0.1",               500,  1.20f,  0, StatType.GoldMultiplier,  0.1));
            list.Add(Make("행운의 손",     "운이 더 좋아진다. 레벨당 골드 배율 +0.05 (최대 10레벨)",                 1500,  1.25f, 10, StatType.GoldMultiplier,  0.05));

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
