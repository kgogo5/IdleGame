using UnityEngine;

namespace IdleGame.Data
{
    [System.Serializable]
    public class DropEntry
    {
        public ItemData item;   // Inspector에서 ItemData 에셋 직접 드래그
        [Range(0, 1000)] public float weight = 10f;
    }

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
        public Color tintColor = Color.white;
        public Color damageFlashColor = Color.red;
        public Sprite hitSprite;        // 타격 시 잠깐 표시되는 이미지
        public float hitSpriteDuration = 0.12f; // 타격 이미지 유지 시간(초)

        [Header("드랍")]
        [Range(0f, 1f)] public float dropChance = 0.08f;
        public bool isBoss = false;
        public float regenPerSecond = 0f;

        // 등급별 가중치 (0이면 해당 등급 제외)
        public float normalWeight    = 82f;
        public float rareWeight      = 16f;
        public float uniqueWeight    =  1.8f;
        public float legendaryWeight =  0.2f;

        // 이 몬스터 전용 아이템 (등급 풀과 합산해 추첨)
        public DropEntry[] customDrops;
    }
}
