using System;
using UnityEngine;

namespace IdleGame.Core
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private double _gold;
        public double Gold => _gold;

        public event Action<double> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // 골드 추가 (몬스터 처치, 판매 등)
        public void AddGold(double amount)
        {
            double multiplier = Managers.PlayerStats.Instance?.GoldMultiplier ?? 1;
            _gold += amount * multiplier;
            OnGoldChanged?.Invoke(_gold);
            Save();
        }

        // 골드 보유 여부 확인
        public bool CanAfford(double amount) => _gold >= amount;

        // 골드 차감 (구매 등) — 성공 여부 반환
        public bool SpendGold(double amount)
        {
            if (!CanAfford(amount)) return false;
            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            Save();
            return true;
        }

        private void Save() => PlayerPrefs.SetString("gold", _gold.ToString());

        private void Load()
        {
            string saved = PlayerPrefs.GetString("gold", "0");
            double.TryParse(saved, out _gold);
        }
    }
}
