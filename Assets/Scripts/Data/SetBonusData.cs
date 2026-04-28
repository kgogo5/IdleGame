using System;
using UnityEngine;

namespace IdleGame.Data
{
    [CreateAssetMenu(fileName = "NewSetBonus", menuName = "IdleGame/SetBonus")]
    public class SetBonusData : ScriptableObject
    {
        [Serializable]
        public class SetStep
        {
            public int requiredCount;
            [TextArea] public string description;
            public StatModifier[] bonuses;
        }

        public string setName;
        public string[] itemNames; // ItemData.name 과 매칭
        public SetStep[] steps;   // 2개/3개 등 단계별 보너스

        public SetStep GetActiveStep(int equippedCount)
        {
            SetStep best = null;
            foreach (var step in steps)
                if (equippedCount >= step.requiredCount)
                    best = step;
            return best;
        }
    }
}
