using System;
using UnityEngine;

namespace IdleGame.Core
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private double _gold;
        private double _jewel;

        public double Gold  => _gold;
        public double Jewel => _jewel;

        public event Action<double> OnGoldChanged;
        public event Action<double> OnJewelChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        public void AddGold(double amount)
        {
            double multiplier = Managers.PlayerStats.Instance?.GoldMultiplier ?? 1;
            _gold += amount * multiplier;
            OnGoldChanged?.Invoke(_gold);
            Save();
        }

        public bool CanAfford(double amount) => _gold >= amount;

        public bool SpendGold(double amount)
        {
            if (!CanAfford(amount)) return false;
            _gold -= amount;
            OnGoldChanged?.Invoke(_gold);
            Save();
            return true;
        }

        public void AddGoldRaw(double amount)
        {
            _gold += amount;
            OnGoldChanged?.Invoke(_gold);
            Save();
        }

        public void AddJewel(double amount)
        {
            _jewel += amount;
            OnJewelChanged?.Invoke(_jewel);
            Save();
        }

        public bool CanAffordJewel(double amount) => _jewel >= amount;

        public bool SpendJewel(double amount)
        {
            if (!CanAffordJewel(amount)) return false;
            _jewel -= amount;
            OnJewelChanged?.Invoke(_jewel);
            Save();
            return true;
        }

        // ── 디버그 (Inspector 우클릭 → 메뉴 선택) ─────────────────────────────
        [SerializeField] private double _debugGoldAmount = 10000;

        [ContextMenu("Debug / Add Gold")]
        private void DebugAddGold() => AddGoldRaw(_debugGoldAmount);

        [ContextMenu("Debug / Set Gold")]
        private void DebugSetGold() { _gold = 0; AddGoldRaw(_debugGoldAmount); }

        [ContextMenu("Debug / Reset Gold")]
        private void DebugResetGold() { _gold = 0; OnGoldChanged?.Invoke(_gold); Save(); }

        public void ResetData()
        {
            _gold  = 0;
            _jewel = 0;
            PlayerPrefs.DeleteKey("gold");
            PlayerPrefs.DeleteKey("jewel");
            OnGoldChanged?.Invoke(_gold);
            OnJewelChanged?.Invoke(_jewel);
        }

        private void Save()
        {
            PlayerPrefs.SetString("gold",  _gold.ToString());
            PlayerPrefs.SetString("jewel", _jewel.ToString());
        }

        private void Load()
        {
            double.TryParse(PlayerPrefs.GetString("gold",  "0"), out _gold);
            double.TryParse(PlayerPrefs.GetString("jewel", "0"), out _jewel);
        }
    }
}
