using UnityEngine;
using TMPro;

/// <summary>
/// 화면 상단에 현재 시간, 날짜, 계절을 표시하는 HUD.
/// </summary>
public class TimeHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _timeText;

    // 계절 이름 (한국어)
    private static readonly string[] SEASON_NAMES = { "봄", "여름", "가을", "겨울" };

    private bool _subscribed;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnTimeUpdated -= UpdateDisplay;
        }
        _subscribed = false;
    }

    private void TrySubscribe()
    {
        if (_subscribed) return;
        if (TimeManager.Instance == null) return;

        TimeManager.Instance.OnTimeUpdated += UpdateDisplay;
        _subscribed = true;

        // 즉시 현재 시간 표시
        UpdateDisplay(TimeManager.Instance.CurrentHourNormalized);
    }

    private void UpdateDisplay(float normalizedHour)
    {
        if (_timeText == null || TimeManager.Instance == null) return;

        var tm = TimeManager.Instance;
        string seasonName = SEASON_NAMES[(int)tm.CurrentSeason];

        // 형식: "1년차 봄 3일 | 14:30"
        _timeText.text = $"{tm.CurrentYear}년차 {seasonName} {tm.CurrentDay}일 | {tm.CurrentHour:D2}:{tm.CurrentMinute:D2}";
    }
}
