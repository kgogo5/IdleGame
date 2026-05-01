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
        public event Action<ItemData> OnItemAcquired;

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

            // 장비 아이템 — sell < 0이면 buy의 30% 자동 계산, 명시하면 그 값 사용
            ItemData Equip(string id, string displayName, string desc, double buy, double sell = -1,
                           ItemRarity rarity = ItemRarity.Normal, EquipSlot slot = EquipSlot.Weapon,
                           string setId = "", params (StatType t, float p)[] mods)
            {
                var d = ScriptableObject.CreateInstance<ItemData>();
                d.name = id; d.itemName = displayName; d.description = desc;
                d.buyPrice  = buy;
                // buy > 0 이면 무조건 30%, buy = 0(드랍전용)이면 명시된 sell 값 사용
                d.sellPrice = buy > 0 ? System.Math.Round(buy * 0.3) : (sell < 0 ? 0 : sell);
                d.isStackable = false; d.rarity = rarity; d.slot = slot; d.setId = setId;
                d.modifiers = mods.Select(m => new StatModifier { statType = m.t, percent = m.p }).ToArray();
                return d;
            }

            // 소모품 — sell < 0이면 buy의 30% 자동 계산
            ItemData Consumable(string id, string displayName, double buy, double sell = -1,
                                params (StatType t, float p)[] mods)
            {
                var d = ScriptableObject.CreateInstance<ItemData>();
                d.name = id; d.itemName = displayName;
                d.buyPrice  = buy;
                d.sellPrice = buy > 0 ? System.Math.Round(buy * 0.3) : (sell < 0 ? 0 : sell);
                d.isStackable = true; d.rarity = ItemRarity.Normal;
                d.modifiers = mods.Select(m => new StatModifier { statType = m.t, percent = m.p }).ToArray();
                d.description = string.Join(", ", d.modifiers.Select(m => m.ToDisplayString()));
                return d;
            }

            // ── 노말 — 개별 아이템 (초반 드랍, 약한 스탯) ──
            var 전사검 = Equip("전사_검", "녹슨 검",
                "+3% 클릭 데미지",
                500, -1, ItemRarity.Normal, EquipSlot.Weapon, "",
                (StatType.ClickDamage, 0.03f));
            전사검.particleEffectId = "hit_slash";
            list.Add(전사검);

            var 전사갑옷 = Equip("전사_갑옷", "낡은 갑옷",
                "+3% 자동공격 데미지",
                800, -1, ItemRarity.Normal, EquipSlot.Armor, "",
                (StatType.AutoDamage, 0.03f));
            list.Add(전사갑옷);

            var 전사장갑 = Equip("전사_장갑", "헝겊 장갑",
                "+3% 클릭 데미지",
                400, -1, ItemRarity.Normal, EquipSlot.Gloves, "",
                (StatType.ClickDamage, 0.03f));
            전사장갑.particleEffectId = "hit_punch";
            list.Add(전사장갑);

            var 마법투구 = Equip("마법_투구", "천 투구",
                "+3% 자동공격 데미지",
                600, -1, ItemRarity.Normal, EquipSlot.Helmet, "",
                (StatType.AutoDamage, 0.03f));
            list.Add(마법투구);

            var 마법반지 = Equip("마법_반지", "구리 반지",
                "+4% 골드 배율",
                500, -1, ItemRarity.Normal, EquipSlot.Ring, "",
                (StatType.GoldMultiplier, 0.04f));
            list.Add(마법반지);

            var 마법목걸이 = Equip("마법_목걸이", "나무 목걸이",
                "+4% 골드 배율",
                700, -1, ItemRarity.Normal, EquipSlot.Amulet, "",
                (StatType.GoldMultiplier, 0.04f));
            list.Add(마법목걸이);

            // ── 레어 세트 — 전사 (상점) ──
            // 정예검: 강한 타격 → 공격속도 감소 (무거운 검)
            var 레어검 = Equip("레어_검", "정예검",
                "+12% 클릭 데미지  -6% 공격속도",
                8_000, 3_000, ItemRarity.Rare, EquipSlot.Weapon, "레어_전사세트",
                (StatType.ClickDamage, 0.12f), (StatType.AttackSpeed, -0.06f));
            레어검.particleEffectId = "hit_slash";
            list.Add(레어검);

            // 강화 갑옷: 강한 자동공격 → 자동공속 감소 (묵직한 갑옷)
            var 레어갑옷 = Equip("레어_갑옷", "강화 갑옷",
                "+12% 자동공격 데미지  -6% 자동공격속도",
                10_000, 4_000, ItemRarity.Rare, EquipSlot.Armor, "레어_전사세트",
                (StatType.AutoDamage, 0.12f), (StatType.AutoAttackSpeed, -0.06f));
            list.Add(레어갑옷);

            // 전투 장갑: 공격속도 증가 → 클릭 데미지 소폭 감소 (빠른 주먹)
            var 레어장갑 = Equip("레어_장갑", "전투 장갑",
                "+10% 공격속도  -3% 클릭 데미지",
                6_000, 2_200, ItemRarity.Rare, EquipSlot.Gloves, "레어_전사세트",
                (StatType.AttackSpeed, 0.10f), (StatType.ClickDamage, -0.03f));
            레어장갑.particleEffectId = "hit_punch";
            list.Add(레어장갑);

            // ── 레어 세트 — 마법사 (상점) ──
            // 은빛 반지: 골드 집중 → 자동공속 소폭 감소
            var 레어반지 = Equip("레어_반지", "은빛 반지",
                "+10% 골드 배율  -4% 자동공격속도",
                7_000, 2_500, ItemRarity.Rare, EquipSlot.Ring, "레어_마법세트",
                (StatType.GoldMultiplier, 0.10f), (StatType.AutoAttackSpeed, -0.04f));
            레어반지.particleEffectId = "hit_magic";
            list.Add(레어반지);

            // 마법사의 모자: 자동공격 데미지 + 자동공속 동반 상승 (마법 증폭, 단점 없음)
            var 레어투구 = Equip("레어_투구", "마법사의 모자",
                "+8% 자동공격 데미지  +6% 자동공격속도",
                8_000, 3_000, ItemRarity.Rare, EquipSlot.Helmet, "레어_마법세트",
                (StatType.AutoDamage, 0.08f), (StatType.AutoAttackSpeed, 0.06f));
            레어투구.particleEffectId = "hit_magic";
            list.Add(레어투구);

            // 마나 목걸이: 높은 골드 → 공격속도 소폭 감소
            var 레어목걸이 = Equip("레어_목걸이", "마나 목걸이",
                "+12% 골드 배율  -3% 공격속도",
                9_000, 3_500, ItemRarity.Rare, EquipSlot.Amulet, "레어_마법세트",
                (StatType.GoldMultiplier, 0.12f), (StatType.AttackSpeed, -0.03f));
            레어목걸이.particleEffectId = "hit_magic";
            list.Add(레어목걸이);

            // ── 레어 개별 — 드랍 전용 (스테이지별) ──

            // Stage 3: 뱀의 단검 — 빠른 단검, 공속↑ 대신 클릭 데미지↓
            var 드랍레어_검A = Equip("드랍레어_검A", "뱀의 단검",
                "+15% 공격속도  -5% 클릭 데미지",
                0, 4_000, ItemRarity.Rare, EquipSlot.Weapon, "",
                (StatType.AttackSpeed, 0.15f), (StatType.ClickDamage, -0.05f));
            드랍레어_검A.particleEffectId = "hit_stab";
            드랍레어_검A.minDropStage = 3;
            list.Add(드랍레어_검A);

            // Stage 3: 전사의 고리 — 균형형, 클릭+자동 데미지 소폭 증가 (단점 없음)
            var 드랍레어_반지A = Equip("드랍레어_반지A", "전사의 고리",
                "+8% 클릭 데미지  +8% 자동공격 데미지",
                0, 3_500, ItemRarity.Rare, EquipSlot.Ring, "",
                (StatType.ClickDamage, 0.08f), (StatType.AutoDamage, 0.08f));
            드랍레어_반지A.minDropStage = 3;
            list.Add(드랍레어_반지A);

            // Stage 4: 강철 방어구 — 강한 자동공격, 무거워서 자동공속↓
            var 드랍레어_갑옷A = Equip("드랍레어_갑옷A", "강철 방어구",
                "+14% 자동공격 데미지  -8% 자동공격속도",
                0, 5_000, ItemRarity.Rare, EquipSlot.Armor, "",
                (StatType.AutoDamage, 0.14f), (StatType.AutoAttackSpeed, -0.08f));
            드랍레어_갑옷A.minDropStage = 4;
            list.Add(드랍레어_갑옷A);

            // Stage 4: 행운의 장갑 — 빠른 공속+골드, 단점 없음 (지원형)
            var 드랍레어_장갑A = Equip("드랍레어_장갑A", "행운의 장갑",
                "+8% 골드 배율  +8% 공격속도",
                0, 4_000, ItemRarity.Rare, EquipSlot.Gloves, "",
                (StatType.GoldMultiplier, 0.08f), (StatType.AttackSpeed, 0.08f));
            드랍레어_장갑A.particleEffectId = "hit_punch";
            드랍레어_장갑A.minDropStage = 4;
            list.Add(드랍레어_장갑A);

            // Stage 5: 명예의 투구 — 자동공격 데미지+공속 동반 상승, 클릭 소폭 감소 (자동 특화)
            var 드랍레어_투구A = Equip("드랍레어_투구A", "명예의 투구",
                "+10% 자동공격 데미지  +8% 자동공격속도  -4% 클릭 데미지",
                0, 4_500, ItemRarity.Rare, EquipSlot.Helmet, "",
                (StatType.AutoDamage, 0.10f), (StatType.AutoAttackSpeed, 0.08f), (StatType.ClickDamage, -0.04f));
            드랍레어_투구A.minDropStage = 5;
            list.Add(드랍레어_투구A);

            // ── 유니크 — 영웅 세트 (드랍 전용, 균형형이지만 자동↓) ──
            var 유니크검 = Equip("유니크_검", "영웅의 검",
                "+80% 클릭 데미지  +30% 자동공격 데미지  -20% 골드 배율",
                0, 30_000, ItemRarity.Unique, EquipSlot.Weapon, "유니크_영웅세트",
                (StatType.ClickDamage, 0.80f), (StatType.AutoDamage, 0.30f), (StatType.GoldMultiplier, -0.20f));
            유니크검.particleEffectId = "hit_stab";
            list.Add(유니크검);

            var 유니크목걸이 = Equip("유니크_목걸이", "현자의 돌",
                "+70% 골드 배율  +30% 클릭 데미지  -20% 자동공격 데미지",
                0, 30_000, ItemRarity.Unique, EquipSlot.Amulet, "유니크_영웅세트",
                (StatType.GoldMultiplier, 0.70f), (StatType.ClickDamage, 0.30f), (StatType.AutoDamage, -0.20f));
            유니크목걸이.particleEffectId = "hit_magic";
            list.Add(유니크목걸이);

            // ── 유니크 개별 — Stage 5~6 ──

            // Stage 5: 광대의 투구 (Harlequin Crest/Shako) — 골드+클릭, 패널티 없음 대신 중간
            var 유니크2_투구 = Equip("유니크2_투구", "광대의 투구",
                "+45% 골드 배율  +35% 클릭 데미지",
                0, 35_000, ItemRarity.Unique, EquipSlot.Helmet, "",
                (StatType.GoldMultiplier, 0.45f), (StatType.ClickDamage, 0.35f));
            유니크2_투구.minDropStage = 5;
            list.Add(유니크2_투구);

            // Stage 5: 냉기의 손 (Frostburn) — 자동 전문, 클릭 희생
            var 유니크2_장갑 = Equip("유니크2_장갑", "냉기의 손",
                "+65% 자동공격 데미지  -25% 클릭 데미지",
                0, 32_000, ItemRarity.Unique, EquipSlot.Gloves, "",
                (StatType.AutoDamage, 0.65f), (StatType.ClickDamage, -0.25f));
            유니크2_장갑.particleEffectId = "hit_punch";
            유니크2_장갑.minDropStage = 5;
            list.Add(유니크2_장갑);

            // Stage 5: 마법검 (Wizardspike) — 클릭+골드 균형, 자동 약간 희생
            var 유니크2_검 = Equip("유니크2_검", "마법검",
                "+35% 클릭 데미지  +40% 골드 배율  -15% 자동공격 데미지",
                0, 32_000, ItemRarity.Unique, EquipSlot.Weapon, "",
                (StatType.ClickDamage, 0.35f), (StatType.GoldMultiplier, 0.40f), (StatType.AutoDamage, -0.15f));
            유니크2_검.particleEffectId = "hit_magic";
            유니크2_검.minDropStage = 5;
            list.Add(유니크2_검);

            // Stage 6: 철벽의 갑옷 (Shaftstop) — 자동 강화, 클릭+골드 희생
            var 유니크2_갑옷 = Equip("유니크2_갑옷", "철벽의 갑옷",
                "+55% 자동공격 데미지  +10% 클릭 데미지  -20% 골드 배율",
                0, 40_000, ItemRarity.Unique, EquipSlot.Armor, "",
                (StatType.AutoDamage, 0.55f), (StatType.ClickDamage, 0.10f), (StatType.GoldMultiplier, -0.20f));
            유니크2_갑옷.minDropStage = 6;
            list.Add(유니크2_갑옷);

            // ── 유니크 개별 — Stage 7~8 ──

            // Stage 7: 요르단의 돌 (Stone of Jordan) — 순수 골드, 클릭 희생
            var 유니크3_반지 = Equip("유니크3_반지", "요르단의 돌",
                "+80% 골드 배율  -15% 클릭 데미지",
                0, 45_000, ItemRarity.Unique, EquipSlot.Ring, "",
                (StatType.GoldMultiplier, 0.80f), (StatType.ClickDamage, -0.15f));
            유니크3_반지.particleEffectId = "hit_magic";
            유니크3_반지.minDropStage = 7;
            list.Add(유니크3_반지);

            // Stage 7: 하이로드의 분노 (Highlord's Wrath) — 클릭+자동, 골드 희생
            var 유니크3_목걸이A = Equip("유니크3_목걸이A", "하이로드의 분노",
                "+70% 클릭 데미지  +25% 자동공격 데미지  -15% 골드 배율",
                0, 45_000, ItemRarity.Unique, EquipSlot.Amulet, "",
                (StatType.ClickDamage, 0.70f), (StatType.AutoDamage, 0.25f), (StatType.GoldMultiplier, -0.15f));
            유니크3_목걸이A.particleEffectId = "hit_magic";
            유니크3_목걸이A.minDropStage = 7;
            list.Add(유니크3_목걸이A);

            // Stage 8: 어둠의 갑옷 (Skullder's Ire) — 골드+자동, 클릭 희생
            var 유니크3_갑옷 = Equip("유니크3_갑옷", "어둠의 갑옷",
                "+75% 골드 배율  +30% 자동공격 데미지  -25% 클릭 데미지",
                0, 50_000, ItemRarity.Unique, EquipSlot.Armor, "",
                (StatType.GoldMultiplier, 0.75f), (StatType.AutoDamage, 0.30f), (StatType.ClickDamage, -0.25f));
            유니크3_갑옷.minDropStage = 8;
            list.Add(유니크3_갑옷);

            // ── 유니크 — 정복자 세트 (Stage 9~10) ──

            // Stage 9: 위험한 손 (Laying of Hands) — 자동+클릭, 골드 희생
            var 유니크3_장갑 = Equip("유니크3_장갑", "위험한 손",
                "+70% 자동공격 데미지  +20% 클릭 데미지  -20% 골드 배율",
                0, 45_000, ItemRarity.Unique, EquipSlot.Gloves, "",
                (StatType.AutoDamage, 0.70f), (StatType.ClickDamage, 0.20f), (StatType.GoldMultiplier, -0.20f));
            유니크3_장갑.particleEffectId = "hit_punch";
            유니크3_장갑.minDropStage = 9;
            list.Add(유니크3_장갑);

            // Stage 9: 마라의 만화경 (Mara's Kaleidoscope) — 정복자 세트
            var 유니크4_목걸이 = Equip("유니크4_목걸이", "마라의 만화경",
                "+60% 클릭 데미지  +60% 골드 배율  -30% 자동공격 데미지",
                0, 55_000, ItemRarity.Unique, EquipSlot.Amulet, "유니크_정복자세트",
                (StatType.ClickDamage, 0.60f), (StatType.GoldMultiplier, 0.60f), (StatType.AutoDamage, -0.30f));
            유니크4_목걸이.particleEffectId = "hit_magic";
            유니크4_목걸이.minDropStage = 9;
            list.Add(유니크4_목걸이);

            // Stage 10: 할아버지의 검 (The Grandfather) — 정복자 세트
            var 유니크4_검 = Equip("유니크4_검", "할아버지의 검",
                "+90% 클릭 데미지  +60% 자동공격 데미지  -30% 골드 배율",
                0, 60_000, ItemRarity.Unique, EquipSlot.Weapon, "유니크_정복자세트",
                (StatType.ClickDamage, 0.90f), (StatType.AutoDamage, 0.60f), (StatType.GoldMultiplier, -0.30f));
            유니크4_검.particleEffectId = "hit_slash";
            유니크4_검.minDropStage = 10;
            list.Add(유니크4_검);

            // ── 레전더리 — 신화 세트 (클릭 특화) ──
            var 레전검 = Equip("레전_검", "신화의 검",
                "+150% 클릭 데미지  -10% 공격속도",
                0, 100_000, ItemRarity.Legendary, EquipSlot.Weapon, "레전_신화세트",
                (StatType.ClickDamage, 1.50f), (StatType.AttackSpeed, -0.10f));
            레전검.particleEffectId = "hit_magic";
            list.Add(레전검);

            var 레전반지 = Equip("레전_반지", "황금의 유산",
                "+130% 골드 배율  +80% 클릭 데미지  -8% 자동공격속도",
                0, 120_000, ItemRarity.Legendary, EquipSlot.Ring, "레전_신화세트",
                (StatType.GoldMultiplier, 1.30f), (StatType.ClickDamage, 0.80f), (StatType.AutoAttackSpeed, -0.08f));
            레전반지.particleEffectId = "hit_magic";
            list.Add(레전반지);

            // ── 레전더리 — 폭풍 세트 (자동공격 극한 특화, 클릭 희생) ──

            // Stage 8: 폭풍의 활 (Windforce) — 폭풍 세트, 강한 자동공격 → 자동공속 소폭 감소
            var 레전2_검 = Equip("레전2_검", "폭풍의 활",
                "+100% 자동공격 데미지  +80% 클릭 데미지  -10% 자동공격속도",
                0, 150_000, ItemRarity.Legendary, EquipSlot.Weapon, "레전_폭풍세트",
                (StatType.AutoDamage, 1.00f), (StatType.ClickDamage, 0.80f), (StatType.AutoAttackSpeed, -0.10f));
            레전2_검.particleEffectId = "hit_slash";
            레전2_검.minDropStage = 8;
            list.Add(레전2_검);

            // Stage 12: 하늘의 갑옷 (Tyrael's Might) — 폭풍 세트
            var 레전2_갑옷 = Equip("레전2_갑옷", "하늘의 갑옷",
                "+130% 자동공격 데미지  +80% 골드 배율  -15% 클릭 데미지",
                0, 180_000, ItemRarity.Legendary, EquipSlot.Armor, "레전_폭풍세트",
                (StatType.AutoDamage, 1.30f), (StatType.GoldMultiplier, 0.80f), (StatType.ClickDamage, -0.15f));
            레전2_갑옷.minDropStage = 12;
            list.Add(레전2_갑옷);

            // ── 레전더리 개별 ──

            // Stage 10: 만화경의 목걸이 — 클릭+골드 최강, 자동공속 소폭 감소
            var 레전3_목걸이 = Equip("레전3_목걸이", "만화경의 목걸이",
                "+120% 클릭 데미지  +120% 골드 배율  -10% 자동공격속도",
                0, 200_000, ItemRarity.Legendary, EquipSlot.Amulet, "",
                (StatType.ClickDamage, 1.20f), (StatType.GoldMultiplier, 1.20f), (StatType.AutoAttackSpeed, -0.10f));
            레전3_목걸이.particleEffectId = "hit_magic";
            레전3_목걸이.minDropStage = 10;
            list.Add(레전3_목걸이);

            // Stage 10: 바알의 철권 (Bul-Kathos') — 순수 전투 특화, 공속 소폭 감소
            var 레전3_장갑 = Equip("레전3_장갑", "바알의 철권",
                "+120% 클릭 데미지  +100% 자동공격 데미지  -10% 공격속도",
                0, 170_000, ItemRarity.Legendary, EquipSlot.Gloves, "",
                (StatType.ClickDamage, 1.20f), (StatType.AutoDamage, 1.00f), (StatType.AttackSpeed, -0.10f));
            레전3_장갑.particleEffectId = "hit_punch";
            레전3_장갑.minDropStage = 10;
            list.Add(레전3_장갑);

            // Stage 12: 도깨비불 반지 (Wisp Projector) — 순수 골드 특화, 공속 소폭 감소
            var 레전3_반지 = Equip("레전3_반지", "도깨비불 반지",
                "+150% 골드 배율  +80% 클릭 데미지  -8% 공격속도",
                0, 220_000, ItemRarity.Legendary, EquipSlot.Ring, "",
                (StatType.GoldMultiplier, 1.50f), (StatType.ClickDamage, 0.80f), (StatType.AttackSpeed, -0.08f));
            레전3_반지.particleEffectId = "hit_magic";
            레전3_반지.minDropStage = 12;
            list.Add(레전3_반지);

            // Stage 12: 안다리엘의 투구 (Andariel's Visage) — 공격속도+클릭, 자동공속 소폭 감소
            var 레전3_투구 = Equip("레전3_투구", "안다리엘의 면상",
                "+100% 클릭 데미지  +60% 공격속도  -8% 자동공격속도",
                0, 190_000, ItemRarity.Legendary, EquipSlot.Helmet, "",
                (StatType.ClickDamage, 1.00f), (StatType.AttackSpeed, 0.60f), (StatType.AutoAttackSpeed, -0.08f));
            레전3_투구.minDropStage = 12;
            list.Add(레전3_투구);

            // ── 소모품 (드랍 전용 — 상점 미판매) ──
            list.Add(Consumable("소모_자동소", "자동 장치",
                0, 500, (StatType.AutoDamage, 0.15f)));

            list.Add(Consumable("소모_자동대", "자동 포탑",
                0, 2_000, (StatType.AutoDamage, 0.40f)));

            list.Add(Consumable("소모_골드", "황금 코인",
                0, 300, (StatType.GoldMultiplier, 0.15f)));

            list.Add(Consumable("소모_클릭", "클릭 부적",
                0, 800, (StatType.ClickDamage, 0.20f)));

            list.Add(Consumable("소모_공속", "신속의 룬",
                0, 1_200, (StatType.AttackSpeed, 0.20f)));

            list.Add(Consumable("소모_자동공속", "연사의 태엽",
                0, 1_000, (StatType.AutoAttackSpeed, 0.25f)));

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

            // 전사 세트: 클릭/자동↑, 세트 보너스는 더 강한 클릭이지만 골드 추가 손해
            list.Add(MakeSet("레어_전사세트", "전사 세트",
                new[] { "레어_검", "레어_갑옷", "레어_장갑" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +20% 클릭 데미지", new[] { (StatType.ClickDamage, 0.20f) }),
                    (3, "3세트: +50% 클릭 데미지  +20% 자동공격 데미지  -25% 골드 배율",
                        new[] { (StatType.ClickDamage, 0.50f), (StatType.AutoDamage, 0.20f), (StatType.GoldMultiplier, -0.25f) }),
                }));

            // 마법사 세트: 골드/자동↑, 세트 보너스는 더 많은 골드지만 클릭 추가 손해
            list.Add(MakeSet("레어_마법세트", "마법사 세트",
                new[] { "레어_반지", "레어_투구", "레어_목걸이" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +20% 골드 배율", new[] { (StatType.GoldMultiplier, 0.20f) }),
                    (3, "3세트: +50% 골드 배율  +20% 자동공격 데미지  -25% 클릭 데미지",
                        new[] { (StatType.GoldMultiplier, 0.50f), (StatType.AutoDamage, 0.20f), (StatType.ClickDamage, -0.25f) }),
                }));

            // 영웅 세트: 강하지만 자동공격 희생
            list.Add(MakeSet("유니크_영웅세트", "영웅 세트",
                new[] { "유니크_검", "유니크_목걸이" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +50% 클릭 데미지  +50% 골드 배율  -20% 자동공격 데미지",
                        new[] { (StatType.ClickDamage, 0.50f), (StatType.GoldMultiplier, 0.50f), (StatType.AutoDamage, -0.20f) }),
                }));

            // 정복자 세트: 클릭+자동+골드 전방위, 각 아이템 단점 완화
            list.Add(MakeSet("유니크_정복자세트", "정복자 세트",
                new[] { "유니크4_검", "유니크4_목걸이" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +60% 클릭 데미지  +40% 골드 배율  -30% 자동공격 데미지",
                        new[] { (StatType.ClickDamage, 0.60f), (StatType.GoldMultiplier, 0.40f), (StatType.AutoDamage, -0.30f) }),
                }));

            // 신화 세트: 클릭+골드 최강, 자동공격 극한 희생
            list.Add(MakeSet("레전_신화세트", "신화 세트",
                new[] { "레전_검", "레전_반지" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +100% 클릭 데미지  +100% 골드 배율  -50% 자동공격 데미지",
                        new[] { (StatType.ClickDamage, 1.00f), (StatType.GoldMultiplier, 1.00f), (StatType.AutoDamage, -0.50f) }),
                }));

            // 폭풍 세트: 자동공격 극한 특화, 클릭 대폭 희생
            list.Add(MakeSet("레전_폭풍세트", "폭풍 세트",
                new[] { "레전2_검", "레전2_갑옷" },
                new (int, string, (StatType, float)[])[]
                {
                    (2, "2세트: +80% 자동공격 데미지  +50% 골드 배율  -80% 클릭 데미지",
                        new[] { (StatType.AutoDamage, 0.80f), (StatType.GoldMultiplier, 0.50f), (StatType.ClickDamage, -0.80f) }),
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

        // 장착된 아이템 중 등급이 가장 높은 것의 파티클 이펙트 반환
        // Weapon 슬롯 우선 → 그 외 슬롯 → 없으면 "hit_punch"
        public string GetEquippedParticleEffectId()
        {
            // 1순위: Weapon 슬롯
            if (_equipped.TryGetValue(EquipSlot.Weapon, out var weapon) &&
                !string.IsNullOrEmpty(weapon.particleEffectId))
                return weapon.particleEffectId;

            // 2순위: 나머지 슬롯 중 등급 가장 높은 것
            ItemData best = null;
            foreach (var item in _equipped.Values)
            {
                if (string.IsNullOrEmpty(item.particleEffectId)) continue;
                if (best == null || item.rarity > best.rarity)
                    best = item;
            }

            return best != null ? best.particleEffectId : "hit_punch";
        }

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
            OnItemAcquired?.Invoke(item);
        }

        // 특정 등급의 미보유 장비 중 랜덤 1개 지급
        // 이미 전부 보유 중이면 그 등급 아이템 판매가만큼 골드 지급
        public void GiveRandomItem(ItemRarity rarity)
        {
            if (_shopItems == null) return;

            int stage = MonsterManager.Instance?.Stage ?? 1;
            var pool = _shopItems
                .Where(i => i != null && !i.isStackable && i.rarity == rarity
                         && !IsOwned(i) && i.minDropStage <= stage)
                .ToList();

            if (pool.Count == 0)
            {
                // 전부 보유 → 동급 아이템 판매가로 골드 보상
                var allOfRarity = _shopItems
                    .Where(i => i != null && !i.isStackable && i.rarity == rarity
                             && i.minDropStage <= stage)
                    .ToList();
                if (allOfRarity.Count == 0) return;
                var fallback = allOfRarity[UnityEngine.Random.Range(0, allOfRarity.Count)];
                CurrencyManager.Instance?.AddGold(fallback.sellPrice);
                return;
            }

            var chosen = pool[UnityEngine.Random.Range(0, pool.Count)];
            _owned[chosen.name] = 1;
            Save();
            OnInventoryChanged?.Invoke();
            OnItemAcquired?.Invoke(chosen);
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
