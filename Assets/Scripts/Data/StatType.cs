namespace IdleGame.Data
{
    public enum StatType
    {
        ClickDamage,      // 클릭당 데미지
        AttackSpeed,      // 클릭 공격속도 (횟수/초)
        AutoDamage,       // 자동공격 1회 데미지
        AutoAttackSpeed,  // 자동공격 속도 (횟수/초)
        GoldMultiplier,   // 골드 획득 배율
        DropRate,         // 아이템 드랍률 배율
        BossSpawnRate,    // 보스 등장 확률 (플랫 추가)
        AutoSellNormal,   // 흰색 아이템 자동판매 해금 (1이면 해금)
        AutoSellRare,     // 파란 아이템 자동판매 해금 (1이면 해금)
        CriticalChance,   // 크리티컬 발동 확률 (0.0 ~ 1.0, percent 단위로 합산)
        CriticalDamage,   // 크리티컬 데미지 배수 추가 (기본 2.0에 percent만큼 가산)
    }
}
