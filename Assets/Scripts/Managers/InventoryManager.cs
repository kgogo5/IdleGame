using System;
using System.Collections.Generic;
using UnityEngine;
using IdleGame.Core;
using IdleGame.Data;

namespace IdleGame.Managers
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private ItemData[] _shopItems;

        private readonly Dictionary<string, int> _owned = new();
        private readonly HashSet<string> _equipped = new();

        public ItemData[] ShopItems => _shopItems;
        public event Action OnInventoryChanged;
        public event Action OnEquipChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (_shopItems == null || _shopItems.Length == 0 || _shopItems[0] == null)
                CreateDefaultItems();
        }

        private void Start() => Load();

        private void CreateDefaultItems()
        {
            var list = new List<ItemData>();

            ItemData Make(string iname, string desc, double buy, double sell, StatType stat, double bonus, bool stack)
            {
                var d = ScriptableObject.CreateInstance<ItemData>();
                d.name = iname;
                d.itemName = iname;
                d.description = desc;
                d.buyPrice = buy;
                d.sellPrice = sell;
                d.statType = stat;
                d.statBonus = bonus;
                d.isStackable = stack;
                return d;
            }

            list.Add(Make("가죽 장갑",   "클릭 데미지 +20",        1000,  350, StatType.ClickDamage,    20,  false));
            list.Add(Make("마법 검",     "클릭 데미지 +300",      15000, 6000, StatType.ClickDamage,   300,  false));
            list.Add(Make("힘의 반지",   "클릭 데미지 +100",       8000, 2800, StatType.ClickDamage,   100,  false));
            list.Add(Make("자동 장치",   "자동 공격 +10/초",       2000,  700, StatType.AutoDPS,        10,  true));
            list.Add(Make("자동 포탑",   "자동 공격 +50/초",      10000, 3500, StatType.AutoDPS,        50,  true));
            list.Add(Make("황금 부적",   "골드 배율 x1.5",         5000, 1750, StatType.GoldMultiplier, 0.5, false));
            list.Add(Make("행운의 부적", "골드 배율 x2.0",        30000,10000, StatType.GoldMultiplier, 1.0, false));
            list.Add(Make("골드 코인",   "골드 배율 +0.3",         3000, 1050, StatType.GoldMultiplier, 0.3, true));

            _shopItems = list.ToArray();
        }

        public int GetCount(ItemData item) =>
            _owned.TryGetValue(item.name, out int c) ? c : 0;

        public bool IsOwned(ItemData item) => GetCount(item) > 0;
        public bool IsEquipped(ItemData item) => _equipped.Contains(item.name);

        public bool CanBuy(ItemData item)
        {
            if (!item.isStackable && IsOwned(item)) return false;
            return CurrencyManager.Instance.CanAfford(item.buyPrice);
        }

        public bool Buy(ItemData item)
        {
            if (!CanBuy(item)) return false;
            if (!CurrencyManager.Instance.SpendGold(item.buyPrice)) return false;

            _owned[item.name] = GetCount(item) + 1;

            // 소모품은 구매 즉시 스탯 적용, 장비는 장착 시에 적용
            if (item.isStackable)
                PlayerStats.Instance.AddBonus(item.statType, item.statBonus);

            Save();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool Equip(ItemData item)
        {
            if (!IsOwned(item) || IsEquipped(item)) return false;
            _equipped.Add(item.name);
            PlayerStats.Instance.AddBonus(item.statType, item.statBonus);
            Save();
            OnEquipChanged?.Invoke();
            return true;
        }

        public bool Unequip(ItemData item)
        {
            if (!IsEquipped(item)) return false;
            _equipped.Remove(item.name);
            PlayerStats.Instance.RemoveBonus(item.statType, item.statBonus);
            Save();
            OnEquipChanged?.Invoke();
            return true;
        }

        public bool Sell(ItemData item)
        {
            if (item.sellPrice <= 0 || !IsOwned(item)) return false;

            if (item.isStackable)
                PlayerStats.Instance.RemoveBonus(item.statType, item.statBonus);
            else if (IsEquipped(item))
            {
                _equipped.Remove(item.name);
                PlayerStats.Instance.RemoveBonus(item.statType, item.statBonus);
            }

            _owned[item.name] = GetCount(item) - 1;
            if (_owned[item.name] <= 0) _owned.Remove(item.name);
            CurrencyManager.Instance.AddGold(item.sellPrice);

            Save();
            OnInventoryChanged?.Invoke();
            OnEquipChanged?.Invoke();
            return true;
        }

        private void Save()
        {
            foreach (var kv in _owned)
                PlayerPrefs.SetInt($"inv_{kv.Key}", kv.Value);
            if (_shopItems == null) return;
            foreach (var item in _shopItems)
            {
                if (!item.isStackable)
                    PlayerPrefs.SetInt($"equip_{item.name}", _equipped.Contains(item.name) ? 1 : 0);
            }
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (_shopItems == null) return;
            foreach (var item in _shopItems)
            {
                int count = PlayerPrefs.GetInt($"inv_{item.name}", 0);
                if (count <= 0) continue;
                _owned[item.name] = count;

                if (item.isStackable)
                {
                    PlayerStats.Instance.AddBonus(item.statType, item.statBonus * count);
                }
                else
                {
                    // 저장된 장착 상태 없으면 기존 호환용으로 자동 장착
                    bool equipped = PlayerPrefs.GetInt($"equip_{item.name}", 1) == 1;
                    if (equipped)
                    {
                        _equipped.Add(item.name);
                        PlayerStats.Instance.AddBonus(item.statType, item.statBonus);
                    }
                }
            }
        }
    }
}
