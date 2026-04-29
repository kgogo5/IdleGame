using UnityEngine;

namespace IdleGame.Data
{
    public enum ItemRarity { Normal = 0, Rare = 1, Unique = 2, Legendary = 3 }

    public static class ItemRarityExtensions
    {
        public static string ToKorean(this ItemRarity r) => r switch
        {
            ItemRarity.Normal    => "노말",
            ItemRarity.Rare      => "레어",
            ItemRarity.Unique    => "유니크",
            ItemRarity.Legendary => "레전더리",
            _                    => r.ToString(),
        };

        public static Color ToColor(this ItemRarity r) => r switch
        {
            ItemRarity.Normal    => new Color(0.75f, 0.75f, 0.75f),   // 연회색
            ItemRarity.Rare      => new Color(0.20f, 0.80f, 1.00f),   // 하늘색
            ItemRarity.Unique    => new Color(1.00f, 0.85f, 0.10f),   // 노랑
            ItemRarity.Legendary => new Color(1.00f, 0.50f, 0.05f),   // 주황
            _                    => Color.white,
        };
    }
}
