---
name: unity-troubleshoot
description: Unity 에러 해결과 디버깅 가이드. NullReferenceException, 오브젝트 안 보임, 충돌 안 됨, 애니메이션 빙글빙글, 타일맵 깨짐, Input System 문제, 스프라이트 슬라이싱 문제 등의 해결법을 제공한다. Unity 에러 메시지를 보고하거나 "안 돼", "작동 안 해", "이상해" 등의 문제 상황에 이 스킬을 참조한다.
---

# Unity 에러 해결 & 디버깅 가이드

## 1. NullReferenceException

### 증상
```
NullReferenceException: Object reference not set to an instance of an object
```

### 원인별 해결

| 원인 | 해결법 |
|------|--------|
| SerializeField 미할당 | Inspector에서 드래그 or 에디터 스크립트에서 SerializedObject로 할당 |
| GetComponent 결과 null | Awake()에서 null 체크 + 에러 로그 |
| Find 대상 없음 | Tag 확인, 씬에 오브젝트 존재 여부 확인 |
| 이벤트 구독 전 호출 | Start() 대신 OnEnable() 시점 확인, 실행 순서 조정 |
| Destroy 후 접근 | OnDestroy()에서 이벤트 해제, null 체크 |

### 디버깅 패턴

```csharp
private void Awake()
{
    _sr = GetComponent<SpriteRenderer>();
    if (_sr == null)
        Debug.LogError($"[{gameObject.name}] SpriteRenderer가 없습니다!", this);
}
```

## 2. 오브젝트가 안 보임

### 체크리스트

1. **Position 확인** — Z값이 카메라보다 뒤에 있지 않은가? (2D: z=0 권장)
2. **SpriteRenderer 있는가?** — 컴포넌트가 붙어있는지 확인
3. **Sprite 할당됨?** — SpriteRenderer.sprite가 null이 아닌지
4. **Sorting Layer/Order** — 다른 스프라이트에 가려져 있지 않은가?
5. **Scale** — (0,0,0)이 아닌지 확인
6. **Color alpha** — SpriteRenderer.color.a가 0이 아닌지
7. **Camera 위치** — 카메라가 오브젝트를 바라보고 있는지
8. **Canvas UI의 경우** — Canvas가 있는지, RectTransform 크기가 0이 아닌지

### 픽셀아트 특이사항

- **PPU(Pixels Per Unit)** 불일치 → 너무 작거나 너무 크게 보임
- 16x16 스프라이트는 PPU=16 설정 필수
- Filter Mode가 **Bilinear**이면 뿌옇게 보임 → **Point** 필수

## 3. 충돌이 안 됨

### 체크리스트

1. **양쪽 모두 Collider2D** 있는가?
2. **최소 한쪽에 Rigidbody2D** 있는가?
3. **Trigger vs Collision** — `Is Trigger` 체크 여부와 콜백 메서드 일치?
   - Trigger: `OnTriggerEnter2D` / `OnTriggerStay2D`
   - Collision: `OnCollisionEnter2D` / `OnCollisionStay2D`
4. **Layer 충돌 매트릭스** — `Edit > Project Settings > Physics 2D`에서 레이어 간 충돌 허용?
5. **Collider 크기** — 너무 작거나 오프셋이 잘못됨
6. **Rigidbody2D Body Type** — Kinematic은 다른 Kinematic과 충돌 안 함 (기본)

## 4. 애니메이션 문제: 캐릭터가 빙글빙글 돈다

### 원인: 스프라이트시트의 행/열 방향 혼동

**Ninja Adventure 스프라이트시트는 열(column) 기반이다!**

```
잘못된 이해 (행 기반):
  sprites[0-3] = Down의 4프레임    ← 틀림!

올바른 이해 (열 기반):
  sprites[0-3] = 각 방향의 1번째 프레임 (Down, Up, Left, Right)
  같은 방향 = 같은 열 = [0,4,8,12], [1,5,9,13], ...
```

### 해결

```csharp
// 4x4 그리드, row-major 슬라이싱 후 열 기반 추출
Sprite[] down  = { sprites[0], sprites[4], sprites[8],  sprites[12] };
Sprite[] up    = { sprites[1], sprites[5], sprites[9],  sprites[13] };
Sprite[] left  = { sprites[2], sprites[6], sprites[10], sprites[14] };
Sprite[] right = { sprites[3], sprites[7], sprites[11], sprites[15] };
```

### 방향 순서 (Ninja Adventure)

| 열(Column) | 방향 | 인덱스 (row-major) |
|-----------|------|-------------------|
| 0 | Down | 0, 4, 8, 12 |
| 1 | Up | 1, 5, 9, 13 |
| 2 | Left | 2, 6, 10, 14 |
| 3 | Right | 3, 7, 11, 15 |

### Idle.png (4x1)

```
Idle.png = 4프레임: [Down, Up, Left, Right]
idleSprites[0] = Down, [1] = Up, [2] = Left, [3] = Right
```

## 5. 타일맵 깨짐 / 뿌옇게 보임

### 원인 & 해결

| 증상 | 원인 | 해결 |
|------|------|------|
| 타일 사이 선이 보임 | Anti-aliasing | `Edit > Project Settings > Quality > Anti Aliasing = Disabled` |
| 타일이 뿌옇다 | Filter Mode | 텍스처 Import → `Filter Mode = Point (no filter)` |
| 타일이 찌그러짐 | Compression | 텍스처 Import → `Compression = None` |
| 타일 크기 불일치 | PPU 불일치 | 모든 타일셋 PPU를 16으로 통일 |
| 타일 사이 검은 선 | 스프라이트 여백 | Sprite Editor에서 padding 확인, extrude edges 설정 |

### 필수 Import 설정 (픽셀아트)

```
Texture Type: Sprite (2D and UI)
Sprite Mode: Multiple (그리드 슬라이싱 시)
Pixels Per Unit: 16
Filter Mode: Point (no filter)       ← 필수!
Compression: None                     ← 필수!
```

## 6. Input System 문제

### "키를 눌러도 반응 없음"

1. **New Input System 패키지 설치 확인**: `com.unity.inputsystem`
2. **Active Input Handling 설정**: `Edit > Project Settings > Player > Active Input Handling = Both` 또는 `Input System Package (New)`
3. **InputAction이 Enable 되었는가?**
```csharp
// 반드시 Enable() 호출
_action.Enable();
```
4. **Dispose 누락**: OnDisable()에서 Disable + Dispose 호출
5. **포커스**: Game 뷰가 포커스되어 있어야 입력이 전달됨

### "여러 키가 동시에 반응"

```csharp
// 어떤 바인딩이 트리거됐는지 확인
private void OnPerformed(InputAction.CallbackContext ctx)
{
    string key = ctx.control.name;  // "1", "2", "space" 등
    Debug.Log($"눌린 키: {key}");
}
```

## 7. 스프라이트 슬라이싱 문제

### "유령 스프라이트가 로드됨"

Unity는 재슬라이싱 후에도 `internalIDToNameTable`에 이전 스프라이트 이름을 유지한다.
`LoadAllAssetsAtPath`가 이전 이름의 스프라이트도 반환할 수 있다.

**해결**: 이름 패턴으로 필터링

```csharp
// {파일명}_{2자리숫자} 패턴만 허용 (예: Walk_00, Walk_15)
string pattern = $@"^{baseName}_\d{{2}}$";
var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
    .OfType<Sprite>()
    .Where(s => Regex.IsMatch(s.name, pattern))
    .OrderBy(s => /* 숫자 추출 정렬 */)
    .ToArray();
```

### "스프라이트 개수가 맞지 않음"

- Phase 0 Setup을 먼저 실행했는지 확인 (슬라이싱은 Phase 0에서 수행)
- `AssetDatabase.Refresh()` 후 다시 로드 시도
- 텍스처 Import Mode가 `Multiple`인지 확인

## 8. 컴파일 에러: Play 모드 진입 불가

### "All compiler errors have to be fixed before you can enter playmode!"

1. **Console 창 확인** — 빨간 에러 메시지 읽기
2. **흔한 원인**:
   - `using` 누락 (예: `using UnityEngine.InputSystem;`)
   - 네임스페이스 변경 (Unity 6: `velocity` → `linearVelocity`)
   - 삭제된 파일의 참조가 남아있음
   - Editor 폴더 밖에 `UnityEditor` 사용
3. **해결 후**: `Assets > Refresh` 또는 Ctrl+R

## 9. SerializedObject 관련 에러

### "SerializedProperty not found: _fieldName"

- 필드 이름이 정확한지 확인 (대소문자 주의)
- 필드에 `[SerializeField]` 어트리뷰트가 있는지 확인
- `public` 필드는 `[SerializeField]` 없이도 직렬화됨
- `[HideInInspector]` 필드도 SerializedObject로 접근 가능

## 10. 성능 문제 디버깅

### 프레임 드롭 원인 탐색

1. **Profiler 사용**: `Window > Analysis > Profiler`
2. **GC Alloc 확인**: 매 프레임 가비지 생성하는 코드 찾기
3. **흔한 원인**:
   - Update()에서 `new` 키워드 (문자열 결합 포함)
   - Update()에서 GetComponent / Find 호출
   - LINQ 쿼리를 매 프레임 실행
   - 불필요한 Debug.Log (릴리즈 시 제거)

## 빠른 참조: 에러 메시지 → 해결법

| 에러 메시지 | 해결법 |
|------------|--------|
| `NullReferenceException` | 참조 체크, Inspector 할당 확인 |
| `MissingReferenceException` | Destroy된 오브젝트 참조, null 체크 추가 |
| `InvalidOperationException: ... velocity` | `linearVelocity`로 변경 (Unity 6) |
| `Can't add script... same name` | 파일명과 클래스명 일치시키기 |
| `The type or namespace '...' could not be found` | using 문 추가 또는 패키지 설치 |
| `Font 'LiberationSans SDF' ... Unicode` | 한글 폰트 생성 + Fallback 등록 |
| `Tag: Player is not defined` | `Edit > Project Settings > Tags and Layers`에 추가 |
