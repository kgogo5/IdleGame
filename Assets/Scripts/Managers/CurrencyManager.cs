using System;
using UnityEngine;

namespace IdleGame.Core
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        private double _gold = 0;
        public double Gold => _gold;

        public event Action<double> OnGoldChanged;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddGold(double amount)
        {
            _gold += amount;
            OnGoldChanged?.Invoke(_gold);
            Debug.Log($"Gold: {_gold}");
        }
    }
}
