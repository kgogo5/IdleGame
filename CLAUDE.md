# IdleGame - Claude Code Guide

## 게임 개요
클릭형 아이들 게임 (Clicker Idle Game)
- 화면의 몬스터 클릭 → 데미지 → 처치 시 골드/아이템 드랍
- 골드로 스킬 업그레이드, 아이템 장착으로 스탯 강화
- 스테이지 진행 (몬스터 강해짐, 보스 등장)
- 위 과정 반복 (코어 루프)

## 기술 스택
- **Engine**: Unity 2D (URP)
- **Language**: C# (.NET)
- **Target**: PC / Mobile

---

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Monster.cs          # 몬스터 HP, 데미지, 드랍, 파티클, 클릭 처리
│   │   ├── UIBoot.cs           # 게임 시작 시 UI 전체 초기화
│   │   ├── BackgroundManager.cs# 스테이지별 배경 전환
│   │   └── BackgroundLoader.cs
│   ├── Data/                   # ScriptableObject / 데이터 정의
│   │   ├── ItemData.cs
│   │   ├── ItemRarity.cs       # 등급 열거형 + ToColor() 확장 메서드
│   │   ├── MonsterData.cs
│   │   ├── StageConfig.cs
│   │   ├── UpgradeData.cs
│   │   ├── StatType.cs
│   │   ├── StatModifier.cs
│   │   ├── EquipSlot.cs
│   │   ├── SetBonusData.cs
│   │   └── ParticleEffectConfig.cs
│   ├── Managers/
│   │   ├── PlayerStats.cs      # 클릭/자동 데미지, 공속, 골드배율, 드랍률 계산
│   │   ├── CurrencyManager.cs  # 골드·보석 증감, 이벤트 발행
│   │   ├── MonsterManager.cs   # 몬스터 스폰, 스테이지, 보스, 도망 시스템
│   │   ├── UpgradeManager.cs   # 스킬 업그레이드 구매·효과 적용
│   │   ├── InventoryManager.cs # 아이템 보유·장착·판매·드랍
│   │   ├── ParticleManager.cs  # JSON 기반 파티클 이펙트 스폰
│   │   ├── AudioManager.cs     # BGM·SFX 재생
│   │   └── SettingsManager.cs  # 볼륨·진동·알림 설정 저장
│   ├── UI/
│   │   ├── UITheme.cs          # 모든 색상/스타일 상수 중앙 관리 ★
│   │   ├── UIHelper.cs         # MakeText, MakeButton, MakeScrollView 유틸
│   │   ├── NavigationController.cs # 5탭 네비게이션
│   │   ├── Panels/
│   │   │   ├── UpgradePanelUI.cs   # 스킬 업그레이드 + 3열 보너스 요약
│   │   │   ├── EquipmentPanelUI.cs # 장비 목록 + 3열 최종 스탯 요약
│   │   │   ├── ShopPanelUI.cs      # 구매/판매 탭
│   │   │   ├── AchievementPanelUI.cs
│   │   │   ├── SettingsPanel.cs    # 설정 팝업 (볼륨·토글·리셋·개발자)
│   │   │   └── StageSelectPanelUI.cs # 스테이지 선택 팝업 (StageDisplay)
│   │   └── Elements/
│   │       ├── MonsterHealthBar.cs # 체력바 + 도망 버튼
│   │       ├── ItemToastManager.cs # 아이템 획득 토스트 알림
│   │       ├── PanelCurrencyBar.cs # 골드·보석 표시 바
│   │       ├── GoldDisplay.cs
│   │       └── StageDisplay.cs
│   ├── Gameplay/
│   │   └── TapController.cs
│   └── Utils/
│       └── NumberFormatter.cs  # 1K, 1M, 1B 숫자 포맷
├── Scenes/
│   └── Main.unity
├── ScriptableObjects/
│   ├── Upgrades/
│   └── Items/
├── Sprites/
├── Audio/
│   ├── BGM/
│   └── SFX/
└── Resources/
    ├── Backgrounds/            # 스테이지별 배경 이미지
    └── Particles/
        └── hit_particles.json  # 파티클 이펙트 데이터
```

---

## 코딩 컨벤션

### 네이밍
| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스 | PascalCase | `CurrencyManager` |
| 메서드 | PascalCase | `AddCurrency()` |
| 프로퍼티 | PascalCase | `CurrentGold` |
| private 필드 | _camelCase | `_currentGold` |
| 상수 | ALL_CAPS | `MAX_LEVEL` |
| 이벤트 | On + PascalCase | `OnGoldChanged` |

### 파일
- 파일명 = 클래스명 (1파일 1클래스 원칙)
- 네임스페이스: `IdleGame.Core`, `IdleGame.UI`, `IdleGame.Data`, `IdleGame.Managers` 등

### C# 스타일
```csharp
namespace IdleGame.Managers
{
    public class CurrencyManager : MonoBehaviour
    {
        // 1. Serialized Fields
        [SerializeField] private double _startingGold = 0;

        // 2. Private Fields
        private double _currentGold;

        // 3. Properties
        public double CurrentGold => _currentGold;

        // 4. Events
        public event Action<double> OnGoldChanged;

        // 5. Unity Lifecycle (Awake → Start → Update 순)
        private void Awake() { }
        private void Start() { }

        // 6. Public Methods
        public void AddGold(double amount) { }

        // 7. Private Methods
        private void Notify() { }
    }
}
```

---

## 아키텍처 원칙

### 싱글톤 패턴 (모든 매니저)
```csharp
public static PlayerStats Instance { get; private set; }
private void Awake()
{
    if (Instance != null) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### UITheme - 색상/스타일 중앙 관리 ★
- **모든 UI 색상은 반드시 `UITheme`에서 참조** (하드코딩 금지)
- 버튼 색, 탭 색, 텍스트 색, 토스트 배경 등 모두 포함
- 아이템 등급 색은 `ItemRarity.ToColor()` 확장 메서드 사용

```csharp
// 올바른 방법
bg.color = UITheme.BtnEquip;
tmp.color = UITheme.TxtDesc;
img.color = item.rarity.ToColor();

// 금지
bg.color = new Color(0.15f, 0.45f, 0.25f); // 직접 하드코딩 X
```

### 이벤트 기반 UI 갱신
- `Update()`에서 UI 갱신 절대 금지
- 매니저가 이벤트 발행 → UI가 구독하여 갱신

```csharp
// 구독
CurrencyManager.Instance.OnGoldChanged += UpdateGold;

// 해제 (OnDestroy에서 반드시)
CurrencyManager.Instance.OnGoldChanged -= UpdateGold;
```

### UI 패널 구조 (동적 생성)
- 모든 패널은 코드로 동적 생성 (프리팹 없음)
- 스탯 요약박스: `VerticalLayoutGroup` → `HorizontalLayoutGroup` 3열 구조
- 스크롤뷰: `UIHelper.MakeScrollView()` 사용

### 데이터: ScriptableObject
- 게임 정적 데이터(업그레이드 스탯, 아이템 정보)는 ScriptableObject
- 런타임 상태(현재 골드, 레벨)는 Manager 클래스에서 관리

---

## 핵심 시스템 목록

| 시스템 | 클래스 | 역할 |
|--------|--------|------|
| UI 초기화 | `UIBoot` | 게임 시작 시 전체 UI 구조 생성 |
| 플레이어 스탯 | `PlayerStats` | 클릭/자동 데미지, 공속, 골드배율, 드랍률 |
| 재화 | `CurrencyManager` | 골드·보석 증감, 저장 |
| 몬스터 | `MonsterManager` | 스폰, 스테이지 진행, 보스, 도망 |
| 업그레이드 | `UpgradeManager` | 스킬 구매, PlayerStats에 플랫 보너스 적용 |
| 인벤토리 | `InventoryManager` | 아이템 보유·장착·판매, PlayerStats에 % 보너스 적용 |
| 파티클 | `ParticleManager` | hit_particles.json 기반 이펙트 스폰 |
| 설정 | `SettingsManager` | BGM/SFX 볼륨, 진동, 알림 |
| 오디오 | `AudioManager` | BGM·SFX 재생 |
| 네비게이션 | `NavigationController` | 5탭 전환 (업그레이드/장비/전투/상점/업적) |
| UI 테마 | `UITheme` | 전체 색상·스타일 상수 |

---

## 스탯 시스템

### PlayerStats 계산 구조
```
최종 스탯 = (기본값 + 업그레이드 플랫 보너스) × (1 + 장비 % 보너스)
```
- **업그레이드(스킬)** → `AddBonus(StatType, amount)` → 플랫 보너스
- **장비** → `AddEquipModifier(StatType, percent)` → % 보너스
- 데미지 최솟값은 0으로 클램프 (`Math.Max(0, ...)`)
- 공격속도는 0.5~20.0, 자동공격속도는 0.1~10.0으로 클램프

### 자동공격 루프
- 프레임 단위 타이머 코루틴으로 공속 변화에 즉각 반응
- `AutoDamage <= 0`이면 타이머를 리셋하고 건너뜀 (리소스 절약)

---

## 작업 시 주의사항

### 필수 규칙
- `Update()`에서 UI 갱신 금지 → 이벤트 기반으로
- `Find()`, `GetComponent()` 반복 호출 금지 → Awake/Start에서 캐싱
- 큰 숫자(골드 등)는 `double` 사용
- 새 색상 추가 시 `UITheme.cs`에 먼저 상수 추가 후 참조
- 이벤트 구독은 반드시 `OnDestroy`에서 해제

### 체력바 (MonsterHealthBar)
- Unity Slider 컴포넌트 기반
- 흰 핸들(Handle Rect)은 Inspector에서 **None**으로 설정
- `_fillImage.type = Image.Type.Filled`로 fillAmount 제어
- 체력은 음수 표기 없이 0으로 클램프 (`Math.Max(0, ...)`)

### 파티클 이펙트
- `Resources/Particles/hit_particles.json`에서 데이터 로드
- 새 이펙트 추가 시 JSON에 항목 추가 후 `ParticleManager`가 자동 인식

### 저장 시스템
- `PlayerPrefs` 기반
- 각 매니저에 `ResetData()` 메서드로 초기화 지원
