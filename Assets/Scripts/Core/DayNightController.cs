using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TimeManager의 시간 변화에 따라 화면 오버레이의 색상/투명도를 조절한다.
/// 낮에는 투명, 밤에는 어두운 파랑, 새벽/황혼에는 따뜻한 주황색.
/// Built-in RP용 — URP 전환 시 Light2D로 교체 가능.
/// </summary>
public class DayNightController : MonoBehaviour
{
    [SerializeField] private SO_DayNightSettings _settings;
    [SerializeField] private Image _overlayImage;

    private bool _subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // OnEnable 시 TimeManager.Instance가 아직 null이었을 경우를 대비한 재시도
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeUpdated -= UpdateOverlay;
        }
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (TimeManager.Instance == null) return;

        TimeManager.Instance.OnTimeUpdated += UpdateOverlay;
        _subscribed = true;

        // 즉시 현재 시간에 맞게 오버레이 갱신
        UpdateOverlay(TimeManager.Instance.CurrentHourNormalized);
    }

    /// <summary>
    /// 정규화된 시간값(0~1)에 따라 오버레이 색상과 투명도를 갱신한다.
    /// </summary>
    private void UpdateOverlay(float normalizedHour)
    {
        if (_settings == null || _overlayImage == null) return;

        Color baseColor = _settings.overlayColorOverDay.Evaluate(normalizedHour);
        float alpha = _settings.overlayAlphaCurve.Evaluate(normalizedHour);
        _overlayImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }
}
