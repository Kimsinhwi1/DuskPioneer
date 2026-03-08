---
name: unity-csharp
description: Unity C# 스크립트 작성 규칙과 패턴. MonoBehaviour, ScriptableObject, 싱글톤, 이벤트 패턴, 네이밍 컨벤션을 포함한다. 새로운 C# 스크립트를 작성하거나 기존 스크립트를 수정할 때 이 스킬의 규칙을 따른다.
---

# Unity C# 스크립트 작성 규칙

## 네이밍 컨벤션

| 대상 | 규칙 | 예시 |
|------|------|------|
| 클래스/구조체 | PascalCase | `PlayerController`, `FarmTile` |
| public 변수 | camelCase | `moveSpeed`, `maxHealth` |
| private 변수 | _camelCase | `_currentHp`, `_isMoving` |
| 상수 | UPPER_SNAKE | `MAX_STAMINA`, `TILE_SIZE` |
| ScriptableObject 클래스 | SO_ 접두사 | `SO_ToolData`, `SO_CropData` |
| enum | PascalCase | `ToolType`, `Season` |
| 이벤트 | On 접두사 | `OnDamaged`, `OnToolUsed` |
| 인터페이스 | I 접두사 | `IDamageable`, `IInteractable` |

## 주석 규칙

- **모든 주석은 한국어**로 작성
- XML 문서 주석(`<summary>`)도 한국어
- 영어 주석 사용 금지

```csharp
/// <summary>
/// 플레이어가 바라보는 방향의 1타일 앞 위치를 반환한다.
/// </summary>
private Vector2 GetToolTargetPosition()
{
    // 현재 방향에 따른 오프셋 계산
    Vector2 offset = _direction switch { ... };
    return (Vector2)transform.position + offset;
}
```

## MonoBehaviour 템플릿

```csharp
using UnityEngine;

/// <summary>
/// [클래스 설명을 한국어로 작성]
/// </summary>
public class ClassName : MonoBehaviour
{
    // ── 인스펙터 노출 필드 ──
    [Header("설정")]
    [SerializeField] private float _speed = 5f;
    [SerializeField] private int _maxCount = 10;

    // ── 이벤트 ──
    public event System.Action<float> OnValueChanged;

    // ── 읽기 전용 프로퍼티 ──
    public float Speed => _speed;

    // ── 내부 상태 ──
    private bool _isActive;
    private float _timer;

    // ── 참조 (Awake에서 캐싱) ──
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;

    private void Awake()
    {
        // 컴포넌트 참조 캐싱 (GetComponent는 반드시 여기서)
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        // 이벤트 구독
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제
    }

    private void Start()
    {
        // 초기화 (다른 오브젝트 참조 등)
    }

    private void Update()
    {
        // 프레임별 로직
    }

    private void FixedUpdate()
    {
        // 물리 로직
    }
}
```

## ScriptableObject 템플릿

```csharp
using UnityEngine;

/// <summary>
/// [데이터 설명을 한국어로 작성]
/// </summary>
[CreateAssetMenu(fileName = "SO_Name", menuName = "DuskPioneer/Name")]
public class SO_Name : ScriptableObject
{
    [Header("기본 정보")]
    public string displayName;

    [Header("수치")]
    public float value = 1f;
    public int count = 1;

    [Header("스프라이트")]
    public Sprite icon;
}
```

## 싱글톤 패턴 (허용 대상: GameManager, TimeManager, AudioManager만)

```csharp
using UnityEngine;

/// <summary>
/// [매니저 설명]. 싱글톤.
/// </summary>
[DefaultExecutionOrder(-100)]
public class SomeManager : MonoBehaviour
{
    public static SomeManager Instance { get; private set; }

    [SerializeField] private SO_Settings _settings;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
```

## 이벤트 기반 통신 패턴

```csharp
// 발행자 (Publisher)
public class StaminaManager : MonoBehaviour
{
    // C# event 사용 (UnityEvent 아님)
    public event System.Action<float, float> OnStaminaChanged;

    public bool UseStamina(float amount)
    {
        _current -= amount;
        OnStaminaChanged?.Invoke(_current, _max);
        return true;
    }
}

// 구독자 (Subscriber)
public class StaminaHUD : MonoBehaviour
{
    private StaminaManager _stamina;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        _stamina = player.GetComponent<StaminaManager>();
        _stamina.OnStaminaChanged += OnStaminaChanged;
    }

    private void OnDestroy()
    {
        if (_stamina != null)
            _stamina.OnStaminaChanged -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float current, float max)
    {
        _fillImage.fillAmount = current / max;
    }
}
```

## Input System 패턴 (New Input System)

```csharp
using UnityEngine.InputSystem;

private InputAction _moveAction;

private void OnEnable()
{
    _moveAction = new InputAction("Move", InputActionType.Value);
    _moveAction.AddCompositeBinding("2DVector")
        .With("Up", "<Keyboard>/w")
        .With("Down", "<Keyboard>/s")
        .With("Left", "<Keyboard>/a")
        .With("Right", "<Keyboard>/d");
    _moveAction.Enable();
}

private void OnDisable()
{
    _moveAction.Disable();
    _moveAction.Dispose();
}
```

## 절대 하면 안 되는 것

1. **Update()에서 GetComponent 호출 금지** — Awake()에서 캐싱
2. **Update()에서 Find/FindObjectOfType 호출 금지** — Start()에서 캐싱
3. **string 비교에 == 사용 주의** — 태그 비교는 `CompareTag()` 사용
4. **public 필드 남발 금지** — `[SerializeField] private` 우선
5. **싱글톤 남발 금지** — GameManager, TimeManager, AudioManager만 허용
6. **Rigidbody2D.velocity 사용 금지** — Unity 6에서는 `linearVelocity` 사용
7. **Coroutine 남발 금지** — 간단한 타이머는 Update에서 float 카운트
8. **하드코딩 경로 금지** — 상수 또는 SerializeField로 관리
9. **Animator 사용 금지** (이 프로젝트) — 직접 스프라이트 스왑 방식

## Unity 6 주의사항

- `Rigidbody2D.velocity` → `Rigidbody2D.linearVelocity`
- `Object.FindObjectOfType<T>()` → `Object.FindFirstObjectByType<T>()`
- `Object.FindObjectsOfType<T>()` → `Object.FindObjectsByType<T>(FindObjectsSortMode.None)`
