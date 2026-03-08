using System;
using UnityEngine;

/// <summary>
/// 플레이어 스태미나를 관리하는 컴포넌트.
/// Player 오브젝트에 부착. Sleep 시 전체 회복.
/// </summary>
public class StaminaManager : MonoBehaviour
{
    [Header("스태미나 설정")]
    public float maxStamina = 100f;

    private float _currentStamina;

    // ── 읽기 전용 프로퍼티 ──
    public float CurrentStamina => _currentStamina;
    public float StaminaRatio => _currentStamina / maxStamina;

    // ── 이벤트 ──
    /// <summary>스태미나 변경 시 발행. param: (현재값, 최대값).</summary>
    public event Action<float, float> OnStaminaChanged;

    private void Awake()
    {
        _currentStamina = maxStamina;
    }

    private void OnEnable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayStarted += OnDayStarted;
    }

    private void Start()
    {
        // OnEnable에서 TimeManager가 아직 초기화 안 됐을 경우 재시도
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayStarted -= OnDayStarted; // 중복 방지
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayStarted += OnDayStarted;

        // 초기 이벤트 발행
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayStarted -= OnDayStarted;
    }

    /// <summary>
    /// 스태미나를 소모한다. 충분하면 true, 부족하면 false.
    /// </summary>
    public bool UseStamina(float amount)
    {
        if (_currentStamina < amount) return false;

        _currentStamina -= amount;
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
        return true;
    }

    /// <summary>
    /// 스태미나를 회복한다.
    /// </summary>
    public void RestoreStamina(float amount)
    {
        _currentStamina = Mathf.Min(_currentStamina + amount, maxStamina);
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
    }

    /// <summary>
    /// Sleep 시 스태미나 전체 회복.
    /// </summary>
    private void OnDayStarted()
    {
        _currentStamina = maxStamina;
        OnStaminaChanged?.Invoke(_currentStamina, maxStamina);
        Debug.Log("[Stamina] 새 아침! 스태미나 전체 회복.");
    }
}
