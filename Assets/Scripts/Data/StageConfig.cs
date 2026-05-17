using UnityEngine;

namespace IdleGame.Data
{
    /// <summary>
    /// 스테이지 범위(stageFrom~stageTo)에 적용되는 배경/BGM/몬스터 풀 설정.
    /// Inspector에서 배열로 등록. 범위가 겹치면 첫 번째 일치 항목 사용.
    /// </summary>
    [CreateAssetMenu(fileName = "StageConfig", menuName = "IdleGame/Stage Config")]
    public class StageConfig : ScriptableObject
    {
        [Header("환경 식별자 (grassland / forest / desert / cave / tundra / skyisland / ruins / demon / void / abyss)")]
        public string environmentKey;
        public string environmentName;  // 한글 표시명 (초원, 숲 등)

        [Header("적용 스테이지 범위 (stageFrom 이상 stageTo 이하)")]
        public int stageFrom = 1;
        public int stageTo   = 1;

        [Header("배경 — 몬스터 죽을 때마다 랜덤 교체 (Resources 상대 경로)")]
        public string[] backgroundPaths;

        [Header("보스 전용 배경 (비워두면 일반 배경 유지)")]
        public string bossBgPath;

        [Header("BGM — Resources 상대 경로 (비워두면 이전 BGM 유지)")]
        [Tooltip("예: Audio/BGM/bgm_stage2")]
        public string bgmPath;

        [Header("몬스터 풀 (비워두면 MonsterManager 기본 풀 사용)")]
        public MonsterData[] monsters;

        [Header("보스 (없으면 MonsterManager 기본 보스 사용)")]
        public MonsterData bossMonster;
    }
}
