using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 도구 선택 및 사용을 담당하는 컨트롤러.
/// Player 오브젝트에 PlayerController와 함께 부착.
/// 8방향 지원. E키로 심기/수확, Q키로 씨앗 전환.
/// </summary>
public class ToolController : MonoBehaviour
{
    [Header("도구 목록")]
    [Tooltip("순서대로 1~4키에 매핑")]
    public SO_ToolData[] tools = new SO_ToolData[4];

    // ── 상수 ──
    private const float TOOL_USE_DURATION = 0.3f;

    // ── 현재 상태 ──
    private int _selectedIndex;
    private bool _isUsingTool;
    private float _animTimer;
    private int _selectedCropIndex;

    // ── 참조 ──
    private PlayerController _pc;
    private StaminaManager _stamina;
    private SpriteRenderer _sr;
    private FarmManager _farmManager;
    private Inventory _inventory;

    // ── 입력 ──
    private InputAction _toolSelectAction;
    private InputAction _toolUseAction;
    private InputAction _interactAction;
    private InputAction _cropCycleAction;
    private InputAction _sleepAction;

    // ── 읽기 전용 프로퍼티 ──
    public SO_ToolData CurrentTool => (tools != null && _selectedIndex < tools.Length) ? tools[_selectedIndex] : null;
    public bool IsUsingTool => _isUsingTool;
    public int SelectedCropIndex => _selectedCropIndex;

    // ── 이벤트 ──
    /// <summary>도구가 전환될 때 발행. param: 새 도구 데이터.</summary>
    public event Action<SO_ToolData> OnToolChanged;

    /// <summary>도구 사용 완료 시 발행. param: (도구타입, 사용 위치).</summary>
    public event Action<ToolType, Vector2> OnToolUsed;

    /// <summary>씨앗 전환 시 발행. param: (작물 인덱스, 작물 데이터).</summary>
    public event Action<int, SO_CropData> OnCropSelectionChanged;

    private void Awake()
    {
        _pc = GetComponent<PlayerController>();
        _stamina = GetComponent<StaminaManager>();
        _sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        _toolSelectAction = new InputAction("ToolSelect", InputActionType.Button);
        _toolSelectAction.AddBinding("<Keyboard>/1");
        _toolSelectAction.AddBinding("<Keyboard>/2");
        _toolSelectAction.AddBinding("<Keyboard>/3");
        _toolSelectAction.AddBinding("<Keyboard>/4");
        _toolSelectAction.performed += OnToolSelectPerformed;
        _toolSelectAction.Enable();

        _toolUseAction = new InputAction("ToolUse", InputActionType.Button);
        _toolUseAction.AddBinding("<Keyboard>/space");
        _toolUseAction.performed += OnToolUsePerformed;
        _toolUseAction.Enable();

        _interactAction = new InputAction("Interact", InputActionType.Button);
        _interactAction.AddBinding("<Keyboard>/e");
        _interactAction.performed += OnInteractPerformed;
        _interactAction.Enable();

        _cropCycleAction = new InputAction("CropCycle", InputActionType.Button);
        _cropCycleAction.AddBinding("<Keyboard>/q");
        _cropCycleAction.performed += OnCropCyclePerformed;
        _cropCycleAction.Enable();

        _sleepAction = new InputAction("Sleep", InputActionType.Button);
        _sleepAction.AddBinding("<Keyboard>/r");
        _sleepAction.performed += OnSleepPerformed;
        _sleepAction.Enable();
    }

    private void OnDisable()
    {
        _toolSelectAction.performed -= OnToolSelectPerformed;
        _toolSelectAction.Disable();
        _toolSelectAction.Dispose();

        _toolUseAction.performed -= OnToolUsePerformed;
        _toolUseAction.Disable();
        _toolUseAction.Dispose();

        _interactAction.performed -= OnInteractPerformed;
        _interactAction.Disable();
        _interactAction.Dispose();

        _cropCycleAction.performed -= OnCropCyclePerformed;
        _cropCycleAction.Disable();
        _cropCycleAction.Dispose();

        _sleepAction.performed -= OnSleepPerformed;
        _sleepAction.Disable();
        _sleepAction.Dispose();
    }

    private void Start()
    {
        if (tools.Length > 0 && tools[0] != null)
            OnToolChanged?.Invoke(tools[0]);

        // 참조 캐싱
        _farmManager = FindFirstObjectByType<FarmManager>();
        _inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        if (!_isUsingTool) return;

        _animTimer += Time.deltaTime;

        if (_animTimer >= TOOL_USE_DURATION)
        {
            FinishToolUse();
        }
    }

    // ──────────────────────────────────────
    //  입력 처리
    // ──────────────────────────────────────

    private void OnToolSelectPerformed(InputAction.CallbackContext ctx)
    {
        if (_isUsingTool) return;

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

        if (_stamina != null && !_stamina.UseStamina(CurrentTool.staminaCost))
        {
            Debug.Log("[Tool] 스태미나 부족!");
            return;
        }

        StartToolUse();
    }

    /// <summary>
    /// E키: 심기 또는 수확.
    /// Tilled 타일 → 인벤토리에서 씨앗 소모 후 심기.
    /// 성장 완료 타일 → 수확.
    /// </summary>
    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        if (_isUsingTool) return;
        if (_farmManager == null) return;

        Vector2 targetPos = GetToolTargetPosition();
        Vector3Int cellPos = _farmManager.WorldToCell(targetPos);

        // 수확 우선 시도
        if (_farmManager.CanHarvest(cellPos))
        {
            _farmManager.TryHarvest(cellPos);
            return;
        }

        // 심기 시도 — 인벤토리에서 씨앗 소모
        if (_farmManager.CanPlant(cellPos))
        {
            if (_inventory != null)
            {
                var seedItem = _inventory.FindSeedForCrop(_selectedCropIndex);
                if (seedItem == null)
                {
                    Debug.Log("[Tool] 씨앗이 없습니다!");
                    return;
                }
                _farmManager.PlantSeed(cellPos, _selectedCropIndex);
                _inventory.RemoveItemByData(seedItem, 1);
            }
            else
            {
                // 인벤토리 없으면 기존 방식 (폴백)
                _farmManager.PlantSeed(cellPos, _selectedCropIndex);
            }
            return;
        }

        Debug.Log("[Tool] 심기/수확 불가: 해당 위치에 경작지가 없거나 이미 작물이 있습니다.");
    }

    /// <summary>
    /// R키: 잠자기 — 다음 날로 넘긴다.
    /// </summary>
    private void OnSleepPerformed(InputAction.CallbackContext ctx)
    {
        if (_isUsingTool) return;
        if (TimeManager.Instance == null) return;

        TimeManager.Instance.Sleep();
    }

    /// <summary>
    /// Q키: 씨앗 종류 전환 (0→1→2→...→0).
    /// </summary>
    private void OnCropCyclePerformed(InputAction.CallbackContext ctx)
    {
        if (_farmManager == null || _farmManager.Crops == null || _farmManager.Crops.Length == 0) return;

        _selectedCropIndex = (_selectedCropIndex + 1) % _farmManager.Crops.Length;
        var cropData = _farmManager.Crops[_selectedCropIndex];
        string name = cropData != null ? cropData.cropName : "없음";
        Debug.Log($"[Tool] 씨앗 전환: {_selectedCropIndex} → {name}");
        OnCropSelectionChanged?.Invoke(_selectedCropIndex, cropData);
    }

    // ──────────────────────────────────────
    //  도구 사용 로직
    // ──────────────────────────────────────

    private void StartToolUse()
    {
        _isUsingTool = true;
        _animTimer = 0f;
        _pc.isActionLocked = true;
    }

    private void FinishToolUse()
    {
        _isUsingTool = false;
        _pc.isActionLocked = false;

        Vector2 usePos = GetToolTargetPosition();
        OnToolUsed?.Invoke(CurrentTool.toolType, usePos);
    }

    /// <summary>
    /// 플레이어가 바라보는 방향의 1타일 앞 위치를 반환한다 (8방향 지원).
    /// </summary>
    private Vector2 GetToolTargetPosition()
    {
        Vector2 pos = transform.position;
        Vector2 offset = PlayerController.DIR_OFFSETS[_pc.Direction];
        return pos + offset;
    }
}
