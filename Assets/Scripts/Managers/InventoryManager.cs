using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IdleGame.Core;
using IdleGame.Data;

namespace IdleGame.Managers
{
    public class InventoryManager : MonoBehaviour
    {
        public static InventoryManager Instance { get; private set; }

        [SerializeField] private SetBonusData[] _setBonuses;

        // 모든 아이템은 항상 코드로 생성 (Awake마다 재생성)
        private ItemData[] _shopItems;

        private readonly Dictionary<string, int>        _owned      = new();
        private readonly Dictionary<EquipSlot, ItemData> _equipped   = new();
        private readonly List<StatModifier[]>            _activeSets = new();

        public ItemData[]     ShopItems  => _shopItems;
        public SetBonusData[] SetBonuses => _setBonuses;

        public event Action OnInventoryChanged;
        public event Action OnEquipChanged;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 항상 재생성 — 런타임 ScriptableObject이므로 직렬화 불필요
            CreateDefaultItems();
            if (_setBonuses == null || _setBonuses.Length == 0) CreateDefaultSets();
        }

        private void Start() => Load();

        // ── 기본 아이템 생성 ─────────────────────────────────────────────────────

        private void CreateDefaultItems()
        {
            var list = new List<ItemData>();

            // 장비 아이템 (% 스탯 기반)
            ItemData Equip(string id, string displayName, string desc, double buy, double sell,
                           ItemRarity rarity, EquipSlot slot, string setId,
                           params (StatType t, float p)[] mods)
            {
                var d = ScriptableObject.CreateInstance<ItemData>();
                d.name = id; d.itemName = displayName; d.description = desc;
                d.buyPrice = buy; d.sellPrice = sell;
                d.isStackable = false; d.rarity = rarity; d.slot = slot; d.setId = setId;
                d.modifiers = mods.Select(m => new StatModifier { statType = m.t, percent = m.p }).ToArray();
                return d;
            }

            // 소모품 (% 스탯 기반, 중복 구매 가능, 등급 없음)
            ItemData Consumable(string id, string displayName, double buy, double sell,
                                params (StatType t, float p)[] mods)
            {
                var d = ScriptableObject.CreateInstance<ItemData>();
                d.name = id; d.itemName = displayName;
                d.buyPrice = buy; d.sellPrice = sell;
                d.isStackable = true; d.rarity = ItemRarity.Normal;
                d.modifiers = mods.Select(m => new StatModifier { statType = m.t, percent = m.p }).ToArray();
                d.description = string.Join(", ", d.modifiers.Select(m => m.ToDisplayString()));
                return d;
            }

            // ── 노말 — 전사 세트 ──
            list.Add(Equip("전사_검", "전사의 검",
                "+40% 클릭 데미지",
                2_000, 2_000, ItemRarity.Normal, EquipSlot.Weapon, "전사세트",
                (StatType.ClickDamage, 0.40f)));

            list.Add(Equip("전사_갑옷", "전사의 갑옷",
                "+20% 자동공격 데미지  -10% 클릭 데미지",
                3_000, 3_000, ItemRarity.Normal, EquipSlot.Armor, "전사세트",
                (StatType.AutoDamage, 0.20f), (StatType.ClickDamage, -0.10f)));

            list.Add(Equip("전사_장갑", "전사의 장갑",
                "+30% 클릭 데미지",
                1_500, 1_500, ItemRarity.Normal, EquipSlot.Gloves, "전사세트",
                (StatType.ClickDamage, 0.30f)));

            // ── 노말 — 마법사 세트 ──
            list.Add(Equip("마법_투구", "마법사의 투구",
                "+30% 자동공격 데미지",
                2_500, 2_500, ItemRarity.Normal, EquipSlot.Helmet, "마법세트",
                (StatType.AutoDamage, 0.30f)));

            list.Add(Equip("마법_반지", "마법사의 반지",
                "+25% 골드 배율  -10% 클릭 데미지",
                2_000, 2_000, ItemRarity.Normal, EquipSlot.Ring, "마법세트",
                (StatType.GoldMultiplier, 0.25f), (StatType.ClickDamage, -0.10f)));

            list.Add(Equip("마법_목걸이", "마법사의 목걸이",
                "+40% 골드 배율",
                3_500, 3_500, ItemRarity.Normal, EquipSlot.Amulet, "마법세트",
                (StatType.GoldMultiplier, 0.40f)));

            // ── 레어 ──
            list.Add(Equip("레어_검", "정예 전사의 검",
                "+80% 클릭 데미지",
                20_000, 20_000, ItemRarity.Rare, EquipSlot.Weapon, "",
                (StatType.ClickDamage, 0.80f)));

            list.Add(Equip("레어_갑옷", "강철 갑옷",
                "+55% 자동공격 데미지",
                25_000, 25_000, ItemRarity.Rare, EquipSlot.Armor, "",
                (StatType.AutoDamage, 0.55f)));

            list.Add(Equip("레어_반지", "황금 반지",
                "+65% 골드 배율",
                18_000, 18_000, ItemRarity.Rare, EquipSlot.Ring, "",
                (StatType.GoldMultiplier, 0.65f)));

            // ── 유니크 (상점 비매품) ──
            list.Add(Equip("유니크_검", "영웅의 검",
                "+150% 클릭 데미지  +50% 자동공격 데미지",
                0, 150_000, ItemRarity.Unique, EquipSlot.Weapon, "",
                (StatType.ClickDamage, 1.50f), (StatType.AutoDamage, 0.50f)));

            list.Add(Equip("유니크_목걸이", "현자의 돌",
                "+120% 골드 배율  +60% 클릭 데미지",
                0, 180_000, ItemRarity.Unique, EquipSlot.Amulet, "",
                (StatType.GoldMultiplier, 1.20f), (StatType.ClickDamage, 0.60f)));

            // ── 레전더리 (상점 비매품) ──
            list.Add(Equip("레전_검", "신화의 검",
                "+300% 클릭 데미지  +150% 자동공격 데미지",
                0, 600_000, ItemRarity.Legendary, EquipSlot.Weapon, "",
                (StatType.ClickDamage, 3.00f), (StatType.AutoDamage, 1.50f)));

            list.Add(Equip("레전_반지", "황금의 유산",
                "+250% 골드 배율  +150% 클릭 데미지",
                0, 750_000, ItemRarity.Legendary, EquipSlot.Ring, "",
                (StatType.GoldMultiplier, 2.50f), (StatType.ClickDamage, 1.50f)));

            // ── 소모품 (% 기반, 중복 구매 가능) ──
            list.Add(Consumable("소모_자동소", "자동 장치",
                2_000, 700, (StatType.AutoDamage, 0.15f)));

            list.Add(Consumable("소모_자동대", "자동 포탑",
                10_000, 3_500, (StatType.AutoDamage, 0.40f)));

            list.Add(Consumable("소모_골드", "황금 코인",
                1_000, 350, (StatType.GoldMultiplier, 0.15f)));

            list.Add(Consumable("소모_클릭", "클릭 부적",
                3_000, 1_000, (StatType.ClickDamage, 0.20f)));

            list.Add(Consumable("소모_공속", "신속의 룬",
                5_000, 1_800, (StatType.AttackSpeed, 0.20f)));

            list.Add(Consumable("소모_자동공속", "연사의 태엽",
                4_000, 1_400, (StatType.AutoAttackSpeed, 0.25f)));

            _shopItems = list.ToArray();
        }

        private void CreateDefaultSets()
        {
            var list = new List<SetBonusData>();

            SetBonusData MakeSet(string id, string displayName, string[] items,
                (int req, string desc, (StatType t, float p)[] mods)[] steps)
            {
                var d = ScriptableObject.CreateInstance<SetBonusData>();
                d.name = id; d.setName = displayName; d.itemNames = items;
                d.steps = new SetBonusData.SetStep[steps.Length];
                for (int i = 0; i < steps.Length; i++)
                {
                    var s = new SetBonusData.SetStep
                    {
                        requiredCount = steps[i].req,
                        description   = steps[i].desc,
                        bonuses       = steps[i].mods.Select(m =>
                            new StatModifier { statType = m.t, percent = m.p }).ToArray()
                    };
                    d.steps[i] = s;
                }
                return d;
            }

            list.Add(MakeSet("전사세트", "전사 세트",
                new[] { "전사_검", "전사_갑옷", "전사_장갑" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +15% 클릭 데미지", new[] { (StatType.ClickDamage, 0.15f) }),
                    (3, "3세트: +30% 클릭 데미지  +15% 자동공격 데미지",
                        new[] { (StatType.ClickDamage, 0.30f), (StatType.AutoDamage, 0.15f) }),
                }));

            list.Add(MakeSet("마법세트", "마법사 세트",
                new[] { "마법_투구", "마법_반지", "마법_목걸이" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +20% 골드 배율", new[] { (StatType.GoldMultiplier, 0.20f) }),
                    (3, "3세트: +40% 골드 배율  +20% 자동공격 데미지",
                        new[] { (StatType.GoldMultiplier, 0.40f), (StatType.AutoDamage, 0.20f) }),
                }));

            _setBonuses = list.ToArray();
        }

        // ── 조회 ─────────────────────────────────────────────────────────────────

        public int  GetCount(ItemData item)   => _owned.TryGetValue(item.name, out int c) ? c : 0;
        public bool IsOwned(ItemData item)    => GetCount(item) > 0;
        public bool IsEquipped(ItemData item) =>
            item != null && !item.isStackable &&
            _equipped.TryGetValue(item.slot, out var eq) && eq.name == item.name;

        public ItemData GetEquippedInSlot(EquipSlot slot) =>
            _equipped.TryGetValue(slot, out var item) ? item : null;

        public IEnumerable<ItemData> GetEquippedItems() => _equipped.Values;

        public IReadOnlyList<SetBonusData> ActiveSetBonuses()
        {
            var result = new List<SetBonusData>();
            if (_setBonuses == null) return result;
            foreach (var s in _setBonuses)
                if (s.GetActiveStep(CountEquippedFromSet(s)) != null) result.Add(s);
            return result;
        }

        // ── 구매 / 판매 ──────────────────────────────────────────────────────────

        public bool CanBuy(ItemData item)
        {
            if (item.buyPrice <= 0) return false;
            if (!item.isStackable && IsOwned(item)) return false;
            return CurrencyManager.Instance.CanAfford(item.buyPrice);
        }

        public bool Buy(ItemData item)
        {
            if (!CanBuy(item)) return false;
            if (!CurrencyManager.Instance.SpendGold(item.buyPrice)) return false;

            _owned[item.name] = GetCount(item) + 1;
            ApplyModifiers(item, +1);

            Save();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool Sell(ItemData item)
        {
            if (item.sellPrice <= 0 || !IsOwned(item)) return false;

            if (!item.isStackable && IsEquipped(item))
                UnequipInternal(item);
            else if (item.isStackable)
                ApplyModifiers(item, -1);

            _owned[item.name] = GetCount(item) - 1;
            if (_owned[item.name] <= 0) _owned.Remove(item.name);
            CurrencyManager.Instance.AddGold(item.sellPrice);

            RecalculateSetBonuses();
            Save();
            OnInventoryChanged?.Invoke();
            OnEquipChanged?.Invoke();
            return true;
        }

        // ── 장착 / 해제 ──────────────────────────────────────────────────────────

        public bool Equip(ItemData item)
        {
            if (!IsOwned(item) || item.isStackable || IsEquipped(item)) return false;

            // 같은 슬롯에 이미 장착된 아이템이 있으면 먼저 해제
            if (_equipped.TryGetValue(item.slot, out var current))
                UnequipInternal(current);

            _equipped[item.slot] = item;
            ApplyModifiers(item, +1);

            RecalculateSetBonuses();
            Save();
            OnEquipChanged?.Invoke();
            return true;
        }

        public bool Unequip(ItemData item)
        {
            if (!IsEquipped(item)) return false;
            UnequipInternal(item);
            RecalculateSetBonuses();
            Save();
            OnEquipChanged?.Invoke();
            return true;
        }

        private void UnequipInternal(ItemData item)
        {
            _equipped.Remove(item.slot);
            ApplyModifiers(item, -1);
        }

        // 모든 stat 적용/해제 (부호: +1 또는 -1)
        private void ApplyModifiers(ItemData item, int sign)
        {
            if (item.modifiers == null) return;
            foreach (var mod in item.modifiers)
                PlayerStats.Instance.AddEquipModifier(mod.statType, mod.percent * sign);
        }

        // ── 세트 보너스 ──────────────────────────────────────────────────────────

        private void RecalculateSetBonuses()
        {
            foreach (var bonuses in _activeSets)
                foreach (var mod in bonuses)
                    PlayerStats.Instance.AddEquipModifier(mod.statType, -mod.percent);
            _activeSets.Clear();

            if (_setBonuses == null) return;
            foreach (var setData in _setBonuses)
            {
                var step = setData.GetActiveStep(CountEquippedFromSet(setData));
                if (step?.bonuses == null) continue;
                _activeSets.Add(step.bonuses);
                foreach (var mod in step.bonuses)
                    PlayerStats.Instance.AddEquipModifier(mod.statType, mod.percent);
            }
        }

        private int CountEquippedFromSet(SetBonusData setData)
        {
            int count = 0;
            if (setData.itemNames == null) return 0;
            foreach (var item in _equipped.Values)
                if (Array.IndexOf(setData.itemNames, item.name) >= 0) count++;
            return count;
        }

        // ── 어드민 ───────────────────────────────────────────────────────────────

        public void ResetData()
        {
            foreach (var kv in _owned) PlayerPrefs.DeleteKey($"inv_{kv.Key}");
            PlayerPrefs.DeleteKey("equipped_slots");
            PlayerPrefs.DeleteKey("equipped_items");

            foreach (var item in _equipped.Values)
                ApplyModifiers(item, -1);

            // 스택 소모품 해제
            foreach (var kv in _owned)
            {
                var item = FindItem(kv.Key);
                if (item != null && item.isStackable)
                    for (int i = 0; i < kv.Value; i++) ApplyModifiers(item, -1);
            }

            foreach (var bonuses in _activeSets)
                foreach (var mod in bonuses)
                    PlayerStats.Instance.AddEquipModifier(mod.statType, -mod.percent);

            _owned.Clear(); _equipped.Clear(); _activeSets.Clear();
            OnInventoryChanged?.Invoke();
            OnEquipChanged?.Invoke();
        }

        public void GiveAllItems()
        {
            if (_shopItems == null) return;
            foreach (var item in _shopItems)
            {
                if (IsOwned(item)) continue;
                _owned[item.name] = 1;
                if (!item.isStackable) Equip(item);
                else ApplyModifiers(item, +1);
            }
            Save();
            OnInventoryChanged?.Invoke();
        }

        // 특정 아이템 ID로 직접 지급
        public void GiveItem(string itemId)
        {
            var item = FindItem(itemId);
            if (item == null) return;
            if (!item.isStackable && IsOwned(item)) return; // 장비 중복 지급 방지
            _owned[item.name] = GetCount(item) + 1;
            if (item.isStackable) ApplyModifiers(item, +1);
            Save();
            OnInventoryChanged?.Invoke();
        }

        // 특정 등급의 미보유 장비 중 랜덤 1개 지급
        public void GiveRandomItem(ItemRarity rarity)
        {
            if (_shopItems == null) return;

            var pool = _shopItems
                .Where(i => i != null && !i.isStackable && i.rarity == rarity && !IsOwned(i))
                .ToList();

            if (pool.Count == 0) return;

            var chosen = pool[UnityEngine.Random.Range(0, pool.Count)];
            _owned[chosen.name] = 1;
            Save();
            OnInventoryChanged?.Invoke();
        }

        // ── 저장 / 불러오기 ───────────────────────────────────────────────────────

        private void Save()
        {
            foreach (var kv in _owned)
                PlayerPrefs.SetInt($"inv_{kv.Key}", kv.Value);
            // "slot:itemId" 쌍으로 직렬화
            var parts = new List<string>();
            foreach (var kv in _equipped) parts.Add($"{(int)kv.Key}:{kv.Value.name}");
            PlayerPrefs.SetString("equipped_slots", string.Join(",", parts));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (_shopItems == null) return;
            var itemMap = _shopItems.ToDictionary(i => i.name, i => i);

            // 보유량 복원
            foreach (var item in _shopItems)
            {
                int count = PlayerPrefs.GetInt($"inv_{item.name}", 0);
                if (count <= 0) continue;
                _owned[item.name] = count;
                if (item.isStackable)
                    for (int i = 0; i < count; i++) ApplyModifiers(item, +1);
            }

            // 장착 복원 (새 포맷 "slot:id", 구 포맷 "id" 모두 지원)
            string savedEquipped = PlayerPrefs.GetString("equipped_slots",
                PlayerPrefs.GetString("equipped_items", ""));
            if (!string.IsNullOrEmpty(savedEquipped))
            {
                foreach (string part in savedEquipped.Split(','))
                {
                    if (string.IsNullOrEmpty(part)) continue;
                    string itemId;
                    if (part.Contains(':'))
                    {
                        var tokens = part.Split(':');
                        itemId = tokens[1];
                    }
                    else itemId = part;

                    if (!itemMap.TryGetValue(itemId, out var item) || !IsOwned(item)) continue;
                    if (!_equipped.ContainsKey(item.slot))
                    {
                        _equipped[item.slot] = item;
                        ApplyModifiers(item, +1);
                    }
                }
            }

            RecalculateSetBonuses();
        }

        private ItemData FindItem(string id) =>
            _shopItems?.FirstOrDefault(i => i.name == id);
    }
}
