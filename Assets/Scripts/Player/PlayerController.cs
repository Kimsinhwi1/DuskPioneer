using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동 및 애니메이션을 관리하는 컨트롤러.
/// 8방향 지원, Animator 없이 직접 스프라이트를 제어.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float frameRate = 8f;

    /// <summary>true이면 이동/애니메이션 입력을 무시한다 (도구 사용 중 등).</summary>
    [HideInInspector] public bool isActionLocked;

    /// <summary>현재 바라보는 방향 인덱스. 읽기 전용.</summary>
    public int Direction => _direction;

    // ── 8방향 순서: 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE ──
    public const int DIR_S = 0;
    public const int DIR_SW = 1;
    public const int DIR_W = 2;
    public const int DIR_NW = 3;
    public const int DIR_N = 4;
    public const int DIR_NE = 5;
    public const int DIR_E = 6;
    public const int DIR_SE = 7;
    public const int DIR_COUNT = 8;

    /// <summary>방향별 Idle 스프라이트 (8개, 에디터에서 할당).</summary>
    [HideInInspector] public Sprite[] idleSprites = new Sprite[DIR_COUNT];

    /// <summary>방향별 Walk 프레임 (8방향 × N프레임, 1차원 배열 — [dir*framesPerDir + frame]).</summary>
    [HideInInspector] public Sprite[] walkSprites;

    /// <summary>방향당 walk 프레임 수.</summary>
    [HideInInspector] public int walkFramesPerDir = 6;

    // ── 방향별 오프셋 벡터 (도구 사용 등에서 활용) ──
    public static readonly Vector2[] DIR_OFFSETS =
    {
        new Vector2(0, -1),           // S
        new Vector2(-0.7f, -0.7f),    // SW
        new Vector2(-1, 0),           // W
        new Vector2(-0.7f, 0.7f),     // NW
        new Vector2(0, 1),            // N
        new Vector2(0.7f, 0.7f),      // NE
        new Vector2(1, 0),            // E
        new Vector2(0.7f, -0.7f),     // SE
    };

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Vector2 _moveInput;
    private int _direction;
    private float _animTimer;
    private int _animFrame;
    private InputAction _moveAction;

    // Atan2 각도 섹터 → 방향 인덱스 매핑
    // 섹터: 0=East, 1=NE, 2=N, 3=NW, 4=W, 5=SW, 6=S, 7=SE
    private static readonly int[] SECTOR_TO_DIR = { DIR_E, DIR_NE, DIR_N, DIR_NW, DIR_W, DIR_SW, DIR_S, DIR_SE };

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _moveAction = new InputAction("Move", InputActionType.Value);
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        _moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        _moveAction.AddBinding("<Gamepad>/leftStick");
        _moveAction.Enable();
    }

    private void OnDisable()
    {
        _moveAction?.Disable();
        _moveAction?.Dispose();
    }

    private void Update()
    {
        if (isActionLocked)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = _moveAction.ReadValue<Vector2>();
        bool isMoving = _moveInput.sqrMagnitude > 0.01f;

        // 8방향 결정
        if (isMoving)
        {
            float angle = Mathf.Atan2(_moveInput.y, _moveInput.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            int sector = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;
            _direction = SECTOR_TO_DIR[sector];
        }

        // 스프라이트 직접 설정
        if (isMoving)
        {
            _animTimer += Time.deltaTime;
            if (_animTimer >= 1f / frameRate)
            {
                _animTimer -= 1f / frameRate;
                _animFrame++;
            }

            if (walkSprites != null && walkSprites.Length > 0)
            {
                _animFrame %= walkFramesPerDir;
                int idx = _direction * walkFramesPerDir + _animFrame;
                if (idx < walkSprites.Length)
                    _sr.sprite = walkSprites[idx];
            }
        }
        else
        {
            _animTimer = 0;
            _animFrame = 0;

            if (idleSprites != null && _direction < idleSprites.Length && idleSprites[_direction] != null)
                _sr.sprite = idleSprites[_direction];
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _moveInput.normalized * moveSpeed;
    }
}
