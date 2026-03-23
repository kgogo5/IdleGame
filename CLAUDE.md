# IdleGame - Claude Code Guide

## 게임 개요
클릭형 아이들 게임 (Clicker Idle Game)
- 화면 클릭 → 돈(골드) 획득
- 골드로 업그레이드/콘텐츠 언락
- 위 과정 반복 (코어 루프)

## 기술 스택
- **Engine**: Unity 2D (URP)
- **Language**: C# (.NET)
- **Target**: PC / Mobile (추후 결정)

---

## 프로젝트 구조

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager, SaveManager, EventBus 등 핵심 시스템
│   ├── Data/           # ScriptableObject 데이터 정의
│   ├── Managers/       # 각 도메인 매니저 (CurrencyManager, UpgradeManager 등)
│   ├── UI/             # UI 전용 스크립트
│   │   ├── Panels/
│   │   └── Elements/
│   ├── Gameplay/       # 게임플레이 로직 (클릭 처리 등)
│   └── Utils/          # 유틸리티, 확장 메서드
├── Scenes/
│   ├── Boot.unity      # 초기 로딩 씬
│   └── Main.unity      # 메인 게임 씬
├── Prefabs/
│   ├── UI/
│   └── Gameplay/
├── ScriptableObjects/
│   ├── Upgrades/
│   └── Items/
├── Sprites/
├── Audio/
│   ├── BGM/
│   └── SFX/
└── Resources/          # 런타임 동적 로드 리소스만
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
| public 필드 | PascalCase | (가급적 프로퍼티 사용) |
| 상수 | ALL_CAPS | `MAX_LEVEL` |
| 인터페이스 | I + PascalCase | `IClickable` |
| 이벤트 | On + PascalCase | `OnGoldChanged` |

### 파일
- 파일명 = 클래스명 (1파일 1클래스 원칙)
- 네임스페이스: `IdleGame.Core`, `IdleGame.UI`, `IdleGame.Data` 등

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

        // 5. Unity Lifecycle
        private void Awake() { }
        private void Start() { }
        private void Update() { }

        // 6. Public Methods
        public void AddGold(double amount) { }

        // 7. Private Methods
        private void UpdateUI() { }
    }
}
```

---

## 아키텍처 원칙

### 싱글톤 패턴 (매니저 클래스)
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
```

### 데이터: ScriptableObject
- 게임 데이터(업그레이드 스탯, 아이템 정보 등)는 ScriptableObject로 정의
- 런타임 상태(현재 골드, 레벨 등)는 Manager 클래스에서 관리

### 이벤트 시스템
- 매니저 간 직접 참조 금지
- `Action` / `event` 또는 `EventBus`를 통해 통신

### 저장 시스템
- `PlayerPrefs` (간단한 값) 또는 JSON 직렬화
- `SaveManager`가 일괄 관리

---

## 핵심 시스템 목록

| 시스템 | 클래스 | 역할 |
|--------|--------|------|
| 게임 진입점 | `GameManager` | 초기화, 씬 관리 |
| 재화 | `CurrencyManager` | 골드 증감, 이벤트 발행 |
| 클릭 | `ClickManager` | 클릭 감지, 클릭당 골드 계산 |
| 업그레이드 | `UpgradeManager` | 업그레이드 구매, 효과 적용 |
| 저장/불러오기 | `SaveManager` | 게임 상태 직렬화 |
| UI | `UIManager` | 패널 전환, HUD 갱신 |

---

## 작업 시 주의사항
- `Update()`에서 매 프레임 UI 갱신 금지 → 이벤트 기반으로 처리
- `Find()`, `GetComponent()` 반복 호출 금지 → Awake/Start에서 캐싱
- 큰 숫자(골드 등)는 `double` 사용 (long은 수십조에서 오버플로우)
- 새 기능 추가 시 기존 매니저 수정보다 새 클래스 추가 우선 (OCP 원칙)
