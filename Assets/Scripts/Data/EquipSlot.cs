namespace IdleGame.Data
{
    public enum EquipSlot
    {
        Weapon  = 0,
        Helmet  = 1,
        Armor   = 2,
        Gloves  = 3,
        Ring    = 4,
        Amulet  = 5,
    }

    public static class EquipSlotExtensions
    {
        public static string ToKorean(this EquipSlot slot) => slot switch
        {
            EquipSlot.Weapon => "무기",
            EquipSlot.Helmet => "투구",
            EquipSlot.Armor  => "갑옷",
            EquipSlot.Gloves => "장갑",
            EquipSlot.Ring   => "반지",
            EquipSlot.Amulet => "목걸이",
            _                => slot.ToString(),
        };
    }
}
