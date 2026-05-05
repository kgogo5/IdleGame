using UnityEngine;

namespace IdleGame.Data
{
    /// <summary>
    /// 게임 전체 밸런스 수치를 한 곳에서 관리하는 중앙 설정 파일.
    /// Unity 메뉴: Assets > Create > IdleGame > Game Config
    /// 생성 후 Assets/Resources/GameConfig.asset 으로 저장하면 자동 로드됨.
    /// 파일이 없으면 아래 기본값으로 동작.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "IdleGame/Game Config")]
    public class GameConfig : ScriptableObject
    {
        private static GameConfig _instance;

        /// <summary>Resources/GameConfig.asset 을 로드. 없으면 기본값 인스턴스 반환.</summary>
        public static GameConfig Get()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameConfig>("GameConfig");
                if (_instance == null)
                    _instance = CreateInstance<GameConfig>(); // 에셋 없을 때 코드 기본값 사용
            }
            return _instance;
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  플레이어 기본 스탯
        //  · 업그레이드/장비 미적용 순수 시작 수치
        //  · 올리면 초반이 쉬워짐, 내리면 어려워짐
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("━━ 플레이어 기본 스탯 ━━")]

        [Tooltip("클릭 1회당 기본 데미지.\n" +
                 "너무 높으면 초반 몬스터가 순삭됨.")]
        public double baseClickDamage = 10;

        [Tooltip("클릭 공격속도 기본값 (초당 클릭 허용 횟수).\n" +
                 "실제 공속은 0.5~20 사이로 클램프됨.")]
        public double baseAttackSpeed = 2;

        [Tooltip("자동공격 기본 데미지.\n" +
                 "0이면 자동공격 없음 — 업그레이드로만 활성화됨.")]
        public double baseAutoDamage = 0;

        [Tooltip("자동공격 속도 기본값 (초당 횟수).\n" +
                 "실제 자동공속은 0.1~10 사이로 클램프됨.")]
        public double baseAutoAttackSpeed = 1;

        [Tooltip("골드 배율 기본값 (1.0 = 100%).\n" +
                 "최종 골드 = 몬스터 기본 골드 × 이 값 × 업그레이드/장비 보너스.")]
        public double baseGoldMultiplier = 1.0;


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  골드 보상 공식
        //  · 잡몹: goldPerStage × 현재 스테이지
        //  · 보스: bossGoldPerStage × 현재 스테이지
        //  · 골드 배율(플레이어 스탯)은 이후에 별도로 곱해짐
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("━━ 골드 보상 ━━")]

        [Tooltip("잡몹 처치 골드 = 이 값 × 스테이지\n" +
                 "예) 25 × 스테이지5 = 125G (배율 미적용 기준)")]
        public double goldPerStage = 25.0;

        [Tooltip("피격당 소량 골드 비율 — 현재 데미지 / 최대체력 × 기본골드 × 이 값으로 계산.\n" +
                 "0이면 피격 골드 없음, 0.05 = 5% 비율.")]
        public double goldOnHitRatio = 0.05;

        [Tooltip("보스 처치 골드 = 이 값 × 스테이지\n" +
                 "예) 500 × 스테이지5 = 2500G (배율 미적용 기준)")]
        public double bossGoldPerStage = 500.0;


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  보스 스폰
        //  · 보스 등장 확률 = bossSpawnChance + 플레이어 장비/업그레이드 보너스
        //  · 첫 스폰 및 도망 직후는 강제 잡몹
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("━━ 보스 스폰 ━━")]

        [Tooltip("보스 기본 등장 확률 (0.03 = 3%).\n" +
                 "보스 소환서 장착 시 이 값에 +2% 추가됨.")]
        [Range(0f, 1f)]
        public float bossSpawnChance = 0.03f;


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  폴백 보스 스탯
        //  · MonsterManager._defaultBossData 에셋이 없을 때만 사용
        //  · 보스 에셋을 만들어 연결하면 이 섹션은 무시됨
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("━━ 폴백 보스 스탯 (보스 에셋 없을 때만 사용) ━━")]

        [Tooltip("폴백 보스 최대 체력.\n" +
                 "너무 낮으면 보스가 쉽게 죽음.")]
        public double fallbackBossMaxHealth = 5000.0;

        [Tooltip("폴백 보스 초당 체력 회복량.\n" +
                 "플레이어 DPS보다 높으면 처치 불가 — 적절히 낮게 설정.")]
        public float fallbackBossRegenPerSecond = 30f;

        [Tooltip("폴백 보스 드랍 확률 (1.0 = 100%). 보스는 통상 고드랍.")]
        [Range(0f, 1f)]
        public float fallbackBossDropChance = 0.8f;

        [Tooltip("폴백 보스 레어 아이템 드랍 가중치 (상대값).")]
        public float fallbackBossRareWeight = 65f;

        [Tooltip("폴백 보스 유니크 아이템 드랍 가중치 (상대값).")]
        public float fallbackBossUniqueWeight = 25f;

        [Tooltip("폴백 보스 레전더리 아이템 드랍 가중치 (상대값).")]
        public float fallbackBossLegendaryWeight = 10f;


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  드랍 시스템
        //  · MonsterData에 별도 가중치가 있으면 그쪽이 우선
        //  · 여기는 코드상 기본값 — MonsterData 에셋에서 개별 조정 가능
        //  · 가중치는 절대값이 아닌 상대값 (합산 후 비율로 계산)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("━━ 잡몹 드랍 기본 가중치 ━━")]

        [Tooltip("노말 등급 드랍 가중치.\n" +
                 "총합 대비 비율: 82/(82+16+1.8+0.2) ≈ 82%")]
        public float defaultNormalWeight = 82f;

        [Tooltip("레어 등급 드랍 가중치.\n" +
                 "총합 대비 비율: 16/(82+16+1.8+0.2) ≈ 16%")]
        public float defaultRareWeight = 16f;

        [Tooltip("유니크 등급 드랍 가중치.\n" +
                 "총합 대비 비율: 1.8/100 ≈ 1.8%")]
        public float defaultUniqueWeight = 1.8f;

        [Tooltip("레전더리 등급 드랍 가중치.\n" +
                 "총합 대비 비율: 0.2/100 ≈ 0.2%")]
        public float defaultLegendaryWeight = 0.2f;


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  업그레이드 비용
        //  · 모든 업그레이드에 일괄 적용
        //  · 개별 조정은 UpgradeManager.cs 의 Make() 호출부에서
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("━━ 업그레이드 비용 배율 ━━")]

        [Tooltip("모든 업그레이드 1레벨 기본 비용에 곱해지는 전역 배율.\n" +
                 "0.5 = 전부 반값 (쉬움), 2.0 = 전부 두 배 (어려움).\n" +
                 "개별 스킬 비용은 UpgradeManager.cs 에서 조정.")]
        public float upgradeCostMultiplier = 1.0f;

        [Tooltip("업그레이드 레벨당 비용 증가 지수(costMultiplier)에 곱해지는 배율.\n" +
                 "1.0 = 그대로, 0.9 = 완만하게 증가, 1.1 = 가파르게 증가.\n" +
                 "예) 기존 지수 1.55에 이 값 0.9 → 실제 지수 1.395.")]
        public float upgradeScalingMultiplier = 1.0f;


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        //  도망 시스템
        //  · 도망 비용 = 현재 보유 골드 × fleeCostRatio
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("━━ 도망 ━━")]

        [Tooltip("도망 비용 = 현재 골드 × 이 비율.\n" +
                 "0.3 = 30%, 0 = 무료, 1.0 = 전 재산.")]
        [Range(0f, 1f)]
        public float fleeCostRatio = 0.3f;
    }
}
