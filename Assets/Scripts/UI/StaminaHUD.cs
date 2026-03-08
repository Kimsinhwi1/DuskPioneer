using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상단 우측 스태미나 바 HUD.
/// StaminaManager의 OnStaminaChanged 이벤트를 구독하여 바를 갱신한다.
/// anchorMax.x를 조절하여 바 크기를 변경하는 방식 (Sprite 불필요).
/// </summary>
public class StaminaHUD : MonoBehaviour
{
    [SerializeField] private Image _fillImage;  // 채워지는 바 이미지

    private StaminaManager _stamina;
    private RectTransform _fillRect;

    private void Start()
    {
        if (_fillImage != null)
            _fillRect = _fillImage.rectTransform;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            _stamina = player.GetComponent<StaminaManager>();

        if (_stamina != null)
        {
            _stamina.OnStaminaChanged += OnStaminaChanged;
            // 초기 상태 반영
            OnStaminaChanged(_stamina.CurrentStamina, _stamina.maxStamina);
        }
    }

    private void OnDestroy()
    {
        if (_stamina != null)
            _stamina.OnStaminaChanged -= OnStaminaChanged;
    }

    private void OnStaminaChanged(float current, float max)
    {
        if (_fillRect == null) return;
        float ratio = Mathf.Clamp01(current / max);
        // anchorMax.x를 조절하여 바 너비 변경 (0=빈, 1=가득)
        _fillRect.anchorMax = new Vector2(ratio, _fillRect.anchorMax.y);
    }
}
