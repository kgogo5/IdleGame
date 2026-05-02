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
        [Header("적용 스테이지 범위 (stageFrom 이상 stageTo 이하)")]
        public int stageFrom = 1;
        public int stageTo   = 1;

        [Header("배경 — Resources 상대 경로 (비워두면 이전 배경 유지)")]
        [Tooltip("예: Backgrounds/stage2_bg")]
        public string backgroundPath;

        [Header("BGM — Resources 상대 경로 (비워두면 이전 BGM 유지)")]
        [Tooltip("예: Audio/BGM/bgm_stage2")]
        public string bgmPath;

        [Header("몬스터 풀 (비워두면 MonsterManager 기본 풀 사용)")]
        public MonsterData[] monsters;
    }
}
