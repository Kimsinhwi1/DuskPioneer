using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 하단 도구 선택 바 HUD.
/// ToolController의 OnToolChanged 이벤트를 구독하여 선택 표시를 갱신한다.
/// </summary>
public class ToolHUD : MonoBehaviour
{
    [SerializeField] private Image[] _slotIcons;      // 슬롯별 도구 아이콘
    [SerializeField] private Image[] _slotBorders;    // 슬롯별 테두리 (선택 표시용)

    [Header("색상")]
    [SerializeField] private Color _selectedColor = Color.yellow;
    [SerializeField] private Color _normalColor = new Color(1f, 1f, 1f, 0.5f);

    private ToolController _toolController;

    private void Start()
    {
        // ToolController 찾기
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _toolController = player.GetComponent<ToolController>();

        if (_toolController != null)
        {
            _toolController.OnToolChanged += OnToolChanged;

            // 초기 상태: 아이콘 설정 + 첫 번째 슬롯 선택
            SetupIcons();
            HighlightSlot(0);
        }
    }

    private void OnDestroy()
    {
        if (_toolController != null)
            _toolController.OnToolChanged -= OnToolChanged;
    }

    /// <summary>
    /// 도구 아이콘을 슬롯에 설정한다.
    /// </summary>
    private void SetupIcons()
    {
        if (_toolController.tools == null) return;

        for (int i = 0; i < _slotIcons.Length && i < _toolController.tools.Length; i++)
        {
            if (_toolController.tools[i] != null && _toolController.tools[i].icon != null)
            {
                _slotIcons[i].sprite = _toolController.tools[i].icon;
                _slotIcons[i].color = Color.white;
            }
        }
    }

    private void OnToolChanged(SO_ToolData tool)
    {
        if (tool == null) return;

        // 선택된 도구의 인덱스 찾기
        for (int i = 0; i < _toolController.tools.Length; i++)
        {
            if (_toolController.tools[i] == tool)
            {
                HighlightSlot(i);
                break;
            }
        }
    }

    private void HighlightSlot(int index)
    {
        for (int i = 0; i < _slotBorders.Length; i++)
        {
            _slotBorders[i].color = (i == index) ? _selectedColor : _normalColor;
        }
    }
}
