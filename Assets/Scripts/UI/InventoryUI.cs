using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// 인벤토리 UI. 30슬롯 그리드를 표시하고, Tab/I 키로 열고 닫는다.
/// 열려있는 동안 시간을 일시정지한다.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("UI 참조")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Transform _slotContainer;

    [Header("슬롯 프리팹 내부 참조 (인덱스 기반)")]
    [Tooltip("슬롯 배경 이미지 배열 (30개)")]
    [SerializeField] private Image[] _slotBackgrounds;
    [Tooltip("슬롯 아이콘 이미지 배열 (30개)")]
    [SerializeField] private Image[] _slotIcons;
    [Tooltip("슬롯 수량 텍스트 배열 (30개)")]
    [SerializeField] private TextMeshProUGUI[] _slotQuantityTexts;

    [Header("색상")]
    [SerializeField] private Color _normalSlotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color _selectedSlotColor = new Color(0.9f, 0.7f, 0.2f, 1f);

    // ── 상태 ──
    private bool _isOpen;
    private int _selectedSlot = -1;

    // ── 참조 ──
    private Inventory _inventory;

    // ── 입력 ──
    private InputAction _toggleAction;
    private InputAction _discardAction;
    private InputAction _navigateAction;

    // ── 읽기 전용 프로퍼티 ──
    public bool IsOpen => _isOpen;

    private void OnEnable()
    {
        _toggleAction = new InputAction("InventoryToggle", InputActionType.Button);
        _toggleAction.AddBinding("<Keyboard>/tab");
        _toggleAction.AddBinding("<Keyboard>/i");
        _toggleAction.performed += OnTogglePerformed;
        _toggleAction.Enable();

        _discardAction = new InputAction("Discard", InputActionType.Button);
        _discardAction.AddBinding("<Keyboard>/x");
        _discardAction.performed += OnDiscardPerformed;
        _discardAction.Enable();

        _navigateAction = new InputAction("Navigate", InputActionType.Value);
        _navigateAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        _navigateAction.performed += OnNavigatePerformed;
        _navigateAction.Enable();
    }

    private void OnDisable()
    {
        _toggleAction.performed -= OnTogglePerformed;
        _toggleAction.Disable();
        _toggleAction.Dispose();

        _discardAction.performed -= OnDiscardPerformed;
        _discardAction.Disable();
        _discardAction.Dispose();

        _navigateAction.performed -= OnNavigatePerformed;
        _navigateAction.Disable();
        _navigateAction.Dispose();
    }

    private void Start()
    {
        // 인벤토리 참조
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _inventory = player.GetComponent<Inventory>();

        if (_inventory != null)
            _inventory.OnInventoryChanged += OnSlotChanged;

        // 패널 초기 비활성
        if (_panel != null)
            _panel.SetActive(false);
        _isOpen = false;
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= OnSlotChanged;
    }

    // ──────────────────────────────────────
    //  입력 처리
    // ──────────────────────────────────────

    private void OnTogglePerformed(InputAction.CallbackContext ctx)
    {
        if (_isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    private void OnDiscardPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isOpen) return;
        if (_selectedSlot < 0 || _inventory == null) return;

        var slot = _inventory.GetSlot(_selectedSlot);
        if (slot.IsEmpty) return;

        _inventory.RemoveItem(_selectedSlot, 1);
        Debug.Log($"[InventoryUI] {slot.item.itemName} 1개 버림");
    }

    private void OnNavigatePerformed(InputAction.CallbackContext ctx)
    {
        if (!_isOpen) return;

        Vector2 nav = ctx.ReadValue<Vector2>();
        int col = 6; // 6열 그리드

        if (_selectedSlot < 0) _selectedSlot = 0;

        if (nav.x > 0.5f) _selectedSlot++;
        else if (nav.x < -0.5f) _selectedSlot--;
        else if (nav.y > 0.5f) _selectedSlot -= col;
        else if (nav.y < -0.5f) _selectedSlot += col;

        _selectedSlot = Mathf.Clamp(_selectedSlot, 0, Inventory.MAX_SLOTS - 1);
        UpdateSelectionHighlight();
    }

    // ──────────────────────────────────────
    //  열기/닫기
    // ──────────────────────────────────────

    public void OpenInventory()
    {
        if (_panel == null) return;

        _isOpen = true;
        _panel.SetActive(true);
        _selectedSlot = 0;

        // 시간 일시정지
        if (TimeManager.Instance != null)
            TimeManager.Instance.PauseTime();

        RefreshAllSlots();
        UpdateSelectionHighlight();
    }

    public void CloseInventory()
    {
        if (_panel == null) return;

        _isOpen = false;
        _panel.SetActive(false);
        _selectedSlot = -1;

        // 시간 재개
        if (TimeManager.Instance != null)
            TimeManager.Instance.ResumeTime();
    }

    // ──────────────────────────────────────
    //  UI 갱신
    // ──────────────────────────────────────

    private void OnSlotChanged(int slotIndex)
    {
        if (!_isOpen) return;
        RefreshSlot(slotIndex);
    }

    private void RefreshAllSlots()
    {
        if (_inventory == null) return;

        for (int i = 0; i < Inventory.MAX_SLOTS; i++)
        {
            RefreshSlot(i);
        }
    }

    private void RefreshSlot(int index)
    {
        if (_slotIcons == null || index >= _slotIcons.Length) return;
        if (_inventory == null) return;

        var slot = _inventory.GetSlot(index);

        if (slot.IsEmpty)
        {
            _slotIcons[index].sprite = null;
            _slotIcons[index].color = Color.clear;
            if (_slotQuantityTexts != null && index < _slotQuantityTexts.Length)
                _slotQuantityTexts[index].text = "";
        }
        else
        {
            _slotIcons[index].sprite = slot.item.icon;
            _slotIcons[index].color = Color.white;
            if (_slotQuantityTexts != null && index < _slotQuantityTexts.Length)
                _slotQuantityTexts[index].text = slot.quantity > 1 ? slot.quantity.ToString() : "";
        }
    }

    private void UpdateSelectionHighlight()
    {
        if (_slotBackgrounds == null) return;

        for (int i = 0; i < _slotBackgrounds.Length; i++)
        {
            _slotBackgrounds[i].color = (i == _selectedSlot) ? _selectedSlotColor : _normalSlotColor;
        }
    }
}
