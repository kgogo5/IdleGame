using System;
using UnityEngine;

namespace IdleGame.Data
{
    [Serializable]
    public struct StatModifier
    {
        public StatType statType;
        [Tooltip("비율: 0.5 = +50%, -0.2 = -20%")]
        public float percent;

        public string ToDisplayString()
        {
            string sign = percent >= 0 ? "+" : "";
            string stat = statType switch
            {
                StatType.ClickDamage     => "클릭 데미지",
                StatType.AttackSpeed     => "클릭 공격속도",
                StatType.AutoDamage      => "자동공격 데미지",
                StatType.AutoAttackSpeed => "자동공격 속도",
                StatType.GoldMultiplier  => "골드 배율",
                _                        => statType.ToString(),
            };
            return $"{sign}{percent * 100:F0}% {stat}";
        }
    }
}
