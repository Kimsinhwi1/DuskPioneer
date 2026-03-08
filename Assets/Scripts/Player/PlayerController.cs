using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동 및 애니메이션을 관리하는 컨트롤러.
/// Animator 없이 직접 스프라이트를 제어하여 확실한 방향 표시.
/// </summary>
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float frameRate = 8f;

    // 방향별 스프라이트 (Phase0Setup에서 할당)
    [HideInInspector] public Sprite[] walkDownSprites;
    [HideInInspector] public Sprite[] walkUpSprites;
    [HideInInspector] public Sprite[] walkLeftSprites;
    [HideInInspector] public Sprite[] walkRightSprites;
    [HideInInspector] public Sprite idleDown;
    [HideInInspector] public Sprite idleUp;
    [HideInInspector] public Sprite idleLeft;
    [HideInInspector] public Sprite idleRight;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Vector2 _moveInput;
    private int _direction; // 0=Down, 1=Up, 2=Left, 3=Right
    private float _animTimer;
    private int _animFrame;
    private InputAction _moveAction;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0;
        _rb.freezeRotation = true;
        _sr = GetComponent<SpriteRenderer>();

        // 스프라이트 할당 확인 로그
        Debug.Log($"[PC] === Direct Sprite Mode (No Animator) ===");
        Debug.Log($"[PC] walkDown: {LogSprites(walkDownSprites)}");
        Debug.Log($"[PC] walkUp: {LogSprites(walkUpSprites)}");
        Debug.Log($"[PC] walkLeft: {LogSprites(walkLeftSprites)}");
        Debug.Log($"[PC] walkRight: {LogSprites(walkRightSprites)}");
        Debug.Log($"[PC] idleDown:{idleDown?.name} idleUp:{idleUp?.name} idleLeft:{idleLeft?.name} idleRight:{idleRight?.name}");
    }

    private string LogSprites(Sprite[] arr)
    {
        if (arr == null || arr.Length == 0) return "NULL/EMPTY";
        return string.Join(", ", System.Array.ConvertAll(arr, s => s != null ? s.name : "null"));
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
        _moveInput = _moveAction.ReadValue<Vector2>();
        bool isMoving = _moveInput.sqrMagnitude > 0.01f;

        // 방향 결정
        if (isMoving)
        {
            if (Mathf.Abs(_moveInput.x) > Mathf.Abs(_moveInput.y))
                _direction = _moveInput.x < 0 ? 2 : 3; // Left : Right
            else
                _direction = _moveInput.y > 0 ? 1 : 0; // Up : Down
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

            Sprite[] walkSprites = _direction switch
            {
                1 => walkUpSprites,
                2 => walkLeftSprites,
                3 => walkRightSprites,
                _ => walkDownSprites
            };

            if (walkSprites != null && walkSprites.Length > 0)
            {
                _animFrame %= walkSprites.Length;
                _sr.sprite = walkSprites[_animFrame];
            }
        }
        else
        {
            _animTimer = 0;
            _animFrame = 0;

            _sr.sprite = _direction switch
            {
                1 => idleUp,
                2 => idleLeft,
                3 => idleRight,
                _ => idleDown
            };
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _moveInput.normalized * moveSpeed;
    }
}
