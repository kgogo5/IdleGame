using UnityEngine;

namespace IdleGame.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "IdleGame/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("기본 정보")]
        public string itemName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("가격")]
        public double buyPrice;
        public double sellPrice;

        [Header("장비 (isStackable = false)")]
        public bool isStackable;
        public ItemRarity rarity;
        public EquipSlot slot;
        public StatModifier[] modifiers; // % 기반, 음수 가능
        public string setId;             // SetBonusData.name 과 매칭

        [Header("소모품 (isStackable = true)")]
        public StatType statType;
        public double statBonus;

        [Header("파티클 효과")]
        public string particleEffectId; // hit_punch / hit_slash / hit_stab / hit_magic

        [Header("드랍 조건")]
        [Tooltip("이 아이템이 드랍 풀에 포함되는 최소 스테이지")]
        public int minDropStage = 1;
    }
}
