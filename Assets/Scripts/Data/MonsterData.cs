using UnityEngine;

namespace IdleGame.Data
{
    [CreateAssetMenu(fileName = "MonsterData", menuName = "IdleGame/Monster Data")]
    public class MonsterData : ScriptableObject
    {
        [Header("Basic Info")]
        public string monsterName;
        public Sprite sprite;

        [Header("Stats")]
        public double maxHealth = 100.0;
        public double goldReward = 10.0;
        public int experienceReward = 5;

        [Header("Visual")]
        public Vector2 spriteSize = Vector2.one;
        public Color damageFlashColor = Color.red;
    }
}
