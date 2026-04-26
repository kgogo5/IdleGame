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
        public double sellPrice; // 0 = 판매 불가

        [Header("스탯 보너스")]
        public StatType statType;
        public double statBonus;

        [Header("속성")]
        public bool isStackable; // false = 한 개만 보유 가능 (장비 개념)
    }
}
