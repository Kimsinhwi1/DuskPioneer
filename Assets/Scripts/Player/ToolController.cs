using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 도구 선택 및 사용을 담당하는 컨트롤러.
/// Player 오브젝트에 PlayerController와 함께 부착.
/// </summary>
public class ToolController : MonoBehaviour
{
    [Header("도구 목록")]
    [Tooltip("순서대로 1~4키에 매핑")]
    public SO_ToolData[] tools = new SO_ToolData[4];

    [Header("도구 사용 스프라이트 (Attack.png, 열 기반)")]
    [HideInInspector] public Sprite[] attackDownSprites;
    [HideInInspector] public Sprite[] attackUpSprites;
    [HideInInspector] public Sprite[] attackLeftSprites;
    [HideInInspector] public Sprite[] attackRightSprites;

    // ── 상수 ──
    private const float TOOL_USE_DURATION = 0.3f;  // 도구 사용 지속 시간 (초)

    // ── 현재 상태 ──
    private int _selectedIndex;
    private bool _isUsingTool;
    private float _animTimer;

    // ── 참조 ──
    private PlayerController _pc;
    private StaminaManager _stamina;
    private SpriteRenderer _sr;

    // ── 입력 ──
    private InputAction _toolSelectAction;
    private InputAction _toolUseAction;

    // ── 읽기 전용 프로퍼티 ──
    public SO_ToolData CurrentTool => (tools != null && _selectedIndex < tools.Length) ? tools[_selectedIndex] : null;
    public bool IsUsingTool => _isUsingTool;

    // ── 이벤트 ──
    /// <summary>도구가 전환될 때 발행. param: 새 도구 데이터.</summary>
    public event Action<SO_ToolData> OnToolChanged;

    /// <summary>도구 사용 완료 시 발행. param: (도구타입, 사용 위치).</summary>
    public event Action<ToolType, Vector2> OnToolUsed;

    private void Awake()
    {
        _pc = GetComponent<PlayerController>();
        _stamina = GetComponent<StaminaManager>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        // 도구 선택: 1~4 숫자키
        _toolSelectAction = new InputAction("ToolSelect", InputActionType.Button);
        _toolSelectAction.AddBinding("<Keyboard>/1");
        _toolSelectAction.AddBinding("<Keyboard>/2");
        _toolSelectAction.AddBinding("<Keyboard>/3");
        _toolSelectAction.AddBinding("<Keyboard>/4");
        _toolSelectAction.performed += OnToolSelectPerformed;
        _toolSelectAction.Enable();

        // 도구 사용: Space
        _toolUseAction = new InputAction("ToolUse", InputActionType.Button);
        _toolUseAction.AddBinding("<Keyboard>/space");
        _toolUseAction.performed += OnToolUsePerformed;
        _toolUseAction.Enable();
    }

    private void OnDisable()
    {
        _toolSelectAction.performed -= OnToolSelectPerformed;
        _toolSelectAction.Disable();
        _toolSelectAction.Dispose();

        _toolUseAction.performed -= OnToolUsePerformed;
        _toolUseAction.Disable();
        _toolUseAction.Dispose();
    }

    private void Start()
    {
        // 초기 도구 선택 이벤트 발행
        if (tools.Length > 0 && tools[0] != null)
            OnToolChanged?.Invoke(tools[0]);
    }

    private void Update()
    {
        if (!_isUsingTool) return;

        // 도구 사용 지속 시간
        _animTimer += Time.deltaTime;

        if (_animTimer >= TOOL_USE_DURATION)
        {
            // 도구 사용 완료
            FinishToolUse();
            return;
        }

        // 현재 방향에 맞는 공격 스프라이트 표시
        Sprite[] sprites = _pc.Direction switch
        {
            1 => attackUpSprites,
            2 => attackLeftSprites,
            3 => attackRightSprites,
            _ => attackDownSprites
        };

        if (sprites != null && sprites.Length > 0)
        {
            // 프레임이 여러 개면 시간 기반 인덱스, 1개면 그대로 표시
            int frame = 0;
            if (sprites.Length > 1)
            {
                float frameTime = TOOL_USE_DURATION / sprites.Length;
                frame = Mathf.Min((int)(_animTimer / frameTime), sprites.Length - 1);
            }
            _sr.sprite = sprites[frame];
        }
    }

    // ──────────────────────────────────────
    //  입력 처리
    // ──────────────────────────────────────

    private void OnToolSelectPerformed(InputAction.CallbackContext ctx)
    {
        if (_isUsingTool) return;

        // 어떤 키가 눌렸는지 확인
        string key = ctx.control.name;
        int index = key switch
        {
            "1" => 0,
            "2" => 1,
            "3" => 2,
            "4" => 3,
            _ => -1
        };

        if (index < 0 || index >= tools.Length || tools[index] == null) return;
        if (index == _selectedIndex) return;

        _selectedIndex = index;
        OnToolChanged?.Invoke(tools[_selectedIndex]);
    }

    private void OnToolUsePerformed(InputAction.CallbackContext ctx)
    {
        if (_isUsingTool) return;
        if (CurrentTool == null) return;

        // 스태미나 체크
        if (_stamina != null && !_stamina.UseStamina(CurrentTool.staminaCost))
        {
            Debug.Log("[Tool] 스태미나 부족!");
            return;
        }

        StartToolUse();
    }

    // ──────────────────────────────────────
    //  도구 사용 로직
    // ──────────────────────────────────────

    private void StartToolUse()
    {
        _isUsingTool = true;
        _animTimer = 0f;

        // 이동 차단
        _pc.isActionLocked = true;
    }

    private void FinishToolUse()
    {
        _isUsingTool = false;
        _pc.isActionLocked = false;

        // 도구 사용 완료 이벤트 (Step 3에서 FarmTile이 구독)
        // 사용 위치: 플레이어 앞 1타일
        Vector2 usePos = GetToolTargetPosition();
        OnToolUsed?.Invoke(CurrentTool.toolType, usePos);
    }

    /// <summary>
    /// 플레이어가 바라보는 방향의 1타일 앞 위치를 반환한다.
    /// </summary>
    private Vector2 GetToolTargetPosition()
    {
        Vector2 pos = transform.position;
        Vector2 offset = _pc.Direction switch
        {
            1 => Vector2.up,
            2 => Vector2.left,
            3 => Vector2.right,
            _ => Vector2.down
        };
        return pos + offset;
    }
}
