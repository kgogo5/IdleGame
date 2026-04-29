using System;
using UnityEngine;

namespace IdleGame.Data
{
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "IdleGame/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        [Header("기본 정보")]
        public string upgradeName;
        [TextArea] public string description;
        public Sprite icon;

        [Header("비용")]
        public double baseCost;
        [Range(1f, 3f)] public float costMultiplier = 1.15f; // 레벨당 비용 증가율

        [Header("제한")]
        public int maxLevel;     // 0 = 무제한
        public int unlockStage = 1; // 해당 스테이지 도달 시 언락

        [Header("효과")]
        public StatType statType;
        public double effectPerLevel; // 레벨당 스탯 증가량

        // 현재 레벨 기준 다음 구매 비용
        public double GetCost(int currentLevel)
        {
            return Math.Round(baseCost * Math.Pow(costMultiplier, currentLevel));
        }

        // n레벨일 때 누적 효과
        public double GetTotalEffect(int level) => effectPerLevel * level;
    }
}
