using UnityEngine;
using IdleGame.Utils;

namespace IdleGame.UI
{
    /// <summary>
    /// 전체 UI에서 사용하는 색상·포맷 규칙을 한 곳에서 정의합니다.
    /// 색이나 포맷을 바꾸고 싶을 때 이 파일만 수정하세요.
    /// </summary>
    public static class UITheme
    {
        // ── 패널 배경 ────────────────────────────────────────
        public static readonly Color BgPanel       = new Color(0.08f, 0.08f, 0.12f, 1f);
        public static readonly Color BgStatBox     = new Color(0.08f, 0.14f, 0.22f, 1f);
        public static readonly Color BgToast       = new Color(0.07f, 0.07f, 0.12f, 0.92f);

        // ── 리스트 행 배경 ───────────────────────────────────
        public static readonly Color BgRowDefault  = new Color(0.13f, 0.13f, 0.18f, 1f);
        public static readonly Color BgRowEquipped = new Color(0.10f, 0.22f, 0.14f, 1f);
        public static readonly Color BgRowSkill    = new Color(0.12f, 0.12f, 0.18f, 1f);
        public static readonly Color BgRowMaxed    = new Color(0.22f, 0.18f, 0.05f, 1f);
        public static readonly Color BgRowSetBonus = new Color(0.18f, 0.14f, 0.06f, 1f);
        public static readonly Color BgRowSellAll  = new Color(0.10f, 0.10f, 0.14f, 1f);

        // ── 탭 ───────────────────────────────────────────────
        public static readonly Color TabInactive   = new Color(0.18f, 0.18f, 0.22f, 1f);
        public static readonly Color TabActiveBuy  = new Color(0.20f, 0.55f, 0.90f, 1f);
        public static readonly Color TabActiveSell = new Color(0.75f, 0.35f, 0.10f, 1f);

        // ── 텍스트 ───────────────────────────────────────────
        public static readonly Color TxtHeading    = new Color(0.67f, 0.80f, 1.00f);
        public static readonly Color TxtStatEquip  = new Color(0.85f, 0.92f, 1.00f);
        public static readonly Color TxtStatSkill  = new Color(0.80f, 0.95f, 0.80f);
        public static readonly Color TxtMod        = new Color(0.70f, 0.85f, 0.70f);
        public static readonly Color TxtSetName    = new Color(1.00f, 0.85f, 0.30f);
        public static readonly Color TxtSetDesc    = new Color(0.95f, 0.80f, 0.40f);
        public static readonly Color TxtSubtle     = new Color(0.50f, 0.60f, 0.70f);
        public static readonly Color TxtEmpty      = new Color(0.40f, 0.40f, 0.40f);
        public static readonly Color TxtDesc       = new Color(0.72f, 0.72f, 0.72f);
        public static readonly Color TxtTabInactive= new Color(0.70f, 0.70f, 0.70f);

        // ── 버튼 ─────────────────────────────────────────────
        public static readonly Color BtnEquip        = new Color(0.15f, 0.45f, 0.25f);
        public static readonly Color BtnUnequip      = new Color(0.55f, 0.15f, 0.15f);
        public static readonly Color BtnBuyable      = new Color(0.15f, 0.50f, 0.15f);
        public static readonly Color BtnTooExpensive = new Color(0.28f, 0.18f, 0.18f);
        public static readonly Color BtnMaxed        = new Color(0.40f, 0.32f, 0.05f);
        public static readonly Color BtnSell         = new Color(0.60f, 0.30f, 0.10f);
        public static readonly Color BtnSellAll      = new Color(0.45f, 0.20f, 0.08f);
        public static readonly Color BtnDisabled     = new Color(0.22f, 0.22f, 0.22f);
        public static readonly Color BtnShopBuyable  = new Color(0.20f, 0.55f, 0.20f);
        public static readonly Color BtnShopCantAfford = new Color(0.28f, 0.28f, 0.28f);

        // ── 리치텍스트 색상 상수 ─────────────────────────────
        public const string HexBuff    = "#7EC8FF";   // 장비 % 버프   (하늘)
        public const string HexDebuff  = "#FF7070";   // 장비 % 패널티 (빨강)
        public const string HexNeutral = "#555555";   // 0값           (회색)
        public const string HexSkill   = "#7EFF7E";   // 스킬 플랫     (초록)
        public const string HexGold    = "#FFD700";   // 골드          (노랑)
        public const string HexWhite   = "#FFFFFF";
        public const string HexEquipped= "#00DD88";   // 장착 중 태그  (민트)

        // ── 스탯 포맷 헬퍼 ───────────────────────────────────

        /// 흰색 값 (클릭 데미지 숫자 등)
        public static string StatVal(string v) => $"<color={HexWhite}>{v}</color>";

        /// 골드 색 값
        public static string StatGold(string v) => $"<color={HexGold}>{v}</color>";

        /// 장비 % 보정 (+파랑 / -빨강 / 0 회색)
        public static string EquipPct(double v) => v == 0
            ? $"<color={HexNeutral}>0%</color>"
            : v > 0 ? $"<color={HexBuff}>+{v * 100:F0}%</color>"
                    : $"<color={HexDebuff}>{v * 100:F0}%</color>";

        /// 스킬 플랫 데미지 보너스 (+초록 / 0 회색)
        public static string SkillBonus(double v) => v == 0
            ? $"<color={HexNeutral}>0</color>"
            : $"<color={HexSkill}>+{NumberFormatter.Format(v)}</color>";

        /// 스킬 속도 보너스 (+초록 / 0 회색)
        public static string SkillSpeed(double v) => v == 0
            ? $"<color={HexNeutral}>0/s</color>"
            : $"<color={HexSkill}>+{v:F2}/s</color>";
    }
}
