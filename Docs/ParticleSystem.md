# Particle System

## 개요

JSON 설정 파일을 읽어 런타임에 `ParticleSystem`을 동적으로 생성하는 시스템.
프리팹 없이 동작하며, 아이템 장착 상태에 따라 타격 이펙트가 자동으로 변경된다.

---

## 파일 구조

```
Assets/
├── Resources/Particles/
│   └── hit_particles.json        # 이펙트 설정 데이터
└── Scripts/
    ├── Data/ParticleEffectConfig.cs  # JSON 역직렬화 데이터 클래스
    └── Managers/ParticleManager.cs   # 이펙트 생성 및 재생
```

---

## 호출 방법

```csharp
ParticleManager.Instance?.Spawn("hit_slash", position);
```

`Monster.cs`의 `OnMouseDown()`에서 장착 아이템 기준으로 자동 호출된다.

```csharp
string effectId = InventoryManager.Instance?.GetEquippedParticleEffectId() ?? "hit_punch";
ParticleManager.Instance?.Spawn(effectId, pos);
```

---

## 이펙트 목록

| id | effectType | 설명 |
|----|------------|------|
| `hit_punch` | `burst` | 방사형 충격 폭발. 전방향으로 퍼짐 |
| `hit_slash` | `slash_line` | 대각선 순차 스폰 → 수직 양방향 퍼짐 |
| `hit_stab` | `stab_line` | 아래→위 순차 스폰 → 위쪽 집중 발사 |
| `hit_magic` | `magic_circle` | 원형 순차 스폰 → 중심에서 방사형 튕김 |

---

## effectType 별 동작

### `burst` (기본)
단순 Burst 방출. `shapeType`에 따라 방향이 결정된다.

```
Spawn() 호출 → 즉시 burstCount개 방출 → stopAction: Destroy
```

### `slash_line`
```
Phase 1: lineAngle 방향 대각선을 따라 spawnInterval 간격으로 순차 스폰
Phase 2: 슬래시 수직 방향으로 번갈아 양쪽 퍼짐 + 중력 활성화
```

| 튜닝 파라미터 | 설명 |
|---|---|
| `lineLength` | 슬래시 선의 길이 |
| `lineAngle` | 슬래시 각도 (도, 0 = 수평, 45 = 대각선) |
| `spawnInterval` | 파티클 간 스폰 간격 (초) |
| `fallSpeed` | 퍼지는 속도 |
| `gravityModifier` | Phase 2 이후 중력 세기 |

### `stab_line`
```
Phase 1: 아래에서 위쪽으로 stabLength 거리에 걸쳐 순차 스폰
Phase 2: 전체 파티클이 위쪽으로 집중 발사 (±0.3 X 랜덤) + 중력 활성화
```

| 튜닝 파라미터 | 설명 |
|---|---|
| `stabLength` | 스폰 선의 길이 |
| `spawnInterval` | 파티클 간 스폰 간격 (초) |
| `fallSpeed` | 발사 속도 |
| `gravityModifier` | Phase 2 이후 중력 세기 |

### `magic_circle`
```
Phase 1: 360도 원형으로 burstCount개를 spawnInterval 간격으로 순차 스폰
Phase 2: 각 파티클이 중심 기준 바깥 방향으로 튕겨나감 + 중력 활성화
```

| 튜닝 파라미터 | 설명 |
|---|---|
| `circleRadius` | 원의 반지름 |
| `spawnInterval` | 파티클 간 스폰 간격 (초) |
| `fallSpeed` | 튕겨나가는 속도 |
| `gravityModifier` | Phase 2 이후 중력 세기 |

---

## JSON 필드 전체 목록

```json
{
  "id": "이펙트 고유 ID",
  "comment": "설명 (코드에서 무시됨)",
  "effectType": "burst | slash_line | stab_line | magic_circle",

  "burstCount": 14,
  "startLifetime": 1.2,
  "startSpeedMin": 0.0,
  "startSpeedMax": 0.0,
  "startSizeMin": 0.13,
  "startSizeMax": 0.20,
  "gravityModifier": 2.0,

  "color":    { "r": 0.55, "g": 0.2, "b": 1.0, "a": 1.0 },
  "colorEnd": { "r": 0.8,  "g": 0.5, "b": 1.0, "a": 0.0 },

  "shapeType": "sphere | edge | cone",
  "shapeRadius": 0.1,
  "shapeAngle": 9.0,

  "renderMode": "billboard | stretched",
  "velocityScale": 0.0,
  "lengthScale": 1.0,

  "sizeOverLifetime": true,
  "colorFadeOut": true,

  "spawnInterval": 0.045,
  "fallSpeed": 2.5,
  "circleRadius": 0.55,
  "lineLength": 0.9,
  "lineAngle": 45.0,
  "stabLength": 0.6
}
```

---

## 아이템-이펙트 연결

`ItemData.particleEffectId` 필드로 아이템마다 이펙트를 지정한다.
`InventoryManager.GetEquippedParticleEffectId()`가 장착 아이템 중 **Weapon 슬롯 우선 → 등급 높은 순**으로 이펙트 id를 반환한다.

| 아이템 | 이펙트 |
|--------|--------|
| 전사의 검 / 정예 전사의 검 | `hit_slash` |
| 영웅의 검 | `hit_stab` |
| 신화의 검 | `hit_magic` |
| 전사의 장갑 | `hit_punch` |
| 마법사 세트 전체 | `hit_magic` |
| 현자의 돌 / 황금의 유산 | `hit_magic` |
| 장착 아이템 없음 (기본값) | `hit_punch` |

---

## 새 이펙트 추가 방법

1. `hit_particles.json`에 새 항목 추가
2. `effectType`을 기존 타입 중 하나로 지정하거나, 새 타입이 필요하면:
   - `ParticleEffectConfig.cs`에 필드 추가
   - `ParticleManager.cs`의 `Spawn()` switch문에 케이스 추가
   - 새 코루틴 또는 메서드 구현
3. `ItemData.particleEffectId`에 id 할당
