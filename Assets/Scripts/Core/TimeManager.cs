using System;
using UnityEngine;

/// <summary>
/// 계절 열거형.
/// </summary>
public enum Season
{
    Spring = 0, // 봄
    Summer = 1, // 여름
    Autumn = 2, // 가을
    Winter = 3  // 겨울
}

/// <summary>
/// 게임 내 시간 흐름을 관리하는 싱글톤.
/// 낮/밤 전환, 계절, 연도를 추적하고 이벤트를 발행한다.
/// </summary>
[DefaultExecutionOrder(-100)]
public class TimeManager : MonoBehaviour
{
    // ── 싱글톤 ──
    public static TimeManager Instance { get; private set; }

    // ── 설정 (ScriptableObject) ──
    [SerializeField] private SO_TimeSettings _settings;

    // ── 현재 상태 ──
    private float _currentMinuteRaw;   // 0~1440 (하루 총 분)
    private int _currentDay = 1;       // 1~daysPerSeason
    private Season _currentSeason = Season.Spring;
    private int _currentYear = 1;
    private bool _isTimePaused;
    private bool _isDayTime = true;
    private int _lastHour = -1;

    // ── 상수 ──
    private const float MINUTES_PER_DAY = 1440f;

    // ── 읽기 전용 프로퍼티 ──
    public int CurrentHour => Mathf.FloorToInt(_currentMinuteRaw / 60f);
    public int CurrentMinute => Mathf.FloorToInt(_currentMinuteRaw) % 60;

    /// <summary>정규화된 시간 (0=00:00, 1=24:00). 낮/밤 컨트롤러에서 사용.</summary>
    public float CurrentHourNormalized => _currentMinuteRaw / MINUTES_PER_DAY;
    public int CurrentDay => _currentDay;
    public Season CurrentSeason => _currentSeason;
    public int CurrentYear => _currentYear;
    public bool IsDayTime => _isDayTime;
    public bool IsTimePaused => _isTimePaused;

    // ── C# 이벤트 ──
    /// <summary>매 프레임 발행. param: 정규화된 시간 (0~1).</summary>
    public event Action<float> OnTimeUpdated;

    /// <summary>매 시간 변경 시 발행. param: 새 시각 (0~23).</summary>
    public event Action<int> OnHourChanged;

    /// <summary>새 날 시작 시 발행. param: 새 날짜, 현재 계절.</summary>
    public event Action<int, Season> OnDayChanged;

    /// <summary>계절 변경 시 발행. param: 새 계절.</summary>
    public event Action<Season> OnSeasonChanged;

    /// <summary>연도 변경 시 발행. param: 새 연도.</summary>
    public event Action<int> OnYearChanged;

    /// <summary>밤 시작 시 발행.</summary>
    public event Action OnNightStarted;

    /// <summary>낮 시작 시 발행.</summary>
    public event Action OnDayStarted;

    // ──────────────────────────────────────
    //  생명주기
    // ──────────────────────────────────────

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // TODO: Phase 3에서 씬 전환 시 DontDestroyOnLoad 추가

        if (_settings == null)
        {
            Debug.LogError("[TimeManager] SO_TimeSettings가 할당되지 않았습니다!");
            return;
        }

        // 06:00 AM에서 시작
        _currentMinuteRaw = _settings.dayStartHour * 60f;
        _lastHour = CurrentHour;
        _isDayTime = true;
    }

    private void Update()
    {
        if (_isTimePaused || _settings == null) return;

        // 시간 진행
        _currentMinuteRaw += _settings.gameMinutesPerRealSecond * Time.deltaTime;

        // 매 프레임 이벤트 (낮/밤 컨트롤러, HUD 갱신용)
        OnTimeUpdated?.Invoke(CurrentHourNormalized);

        // 시간 변경 체크
        if (CurrentHour != _lastHour)
        {
            HandleHourChange();
        }

        // 하루 종료 체크
        if (_currentMinuteRaw >= MINUTES_PER_DAY)
        {
            HandleNewDay();
        }
    }

    // ──────────────────────────────────────
    //  시간 전환 처리
    // ──────────────────────────────────────

    private void HandleHourChange()
    {
        _lastHour = CurrentHour;
        OnHourChanged?.Invoke(CurrentHour);

        // 밤 시작
        if (CurrentHour == _settings.nightStartHour && _isDayTime)
        {
            _isDayTime = false;
            OnNightStarted?.Invoke();
            Debug.Log($"[TimeManager] 밤이 되었습니다. ({CurrentHour}:00)");
        }

        // 낮 시작
        if (CurrentHour == _settings.dayStartHour && !_isDayTime)
        {
            _isDayTime = true;
            OnDayStarted?.Invoke();
            Debug.Log($"[TimeManager] 아침이 밝았습니다. ({CurrentHour}:00)");
        }

        // 강제 수면 체크 (새벽 2시)
        if (CurrentHour == _settings.forceSleepHour && !_isDayTime)
        {
            Debug.Log("[TimeManager] 너무 늦었습니다... 강제 수면.");
            Sleep();
        }
    }

    private void HandleNewDay()
    {
        _currentMinuteRaw -= MINUTES_PER_DAY;
        _currentDay++;

        if (_currentDay > _settings.daysPerSeason)
        {
            HandleNewSeason();
        }

        OnDayChanged?.Invoke(_currentDay, _currentSeason);
        Debug.Log($"[TimeManager] 새 날: {_currentYear}년차 {_currentSeason} {_currentDay}일");
    }

    private void HandleNewSeason()
    {
        _currentDay = 1;
        int nextSeason = ((int)_currentSeason + 1) % _settings.seasonsPerYear;
        _currentSeason = (Season)nextSeason;

        if (_currentSeason == Season.Spring)
        {
            _currentYear++;
            OnYearChanged?.Invoke(_currentYear);
            Debug.Log($"[TimeManager] 새 해: {_currentYear}년차");
        }

        OnSeasonChanged?.Invoke(_currentSeason);
        Debug.Log($"[TimeManager] 계절 변경: {_currentSeason}");
    }

    // ──────────────────────────────────────
    //  공개 메서드
    // ──────────────────────────────────────

    /// <summary>시간 흐름 일시정지.</summary>
    public void PauseTime() => _isTimePaused = true;

    /// <summary>시간 흐름 재개.</summary>
    public void ResumeTime() => _isTimePaused = false;

    /// <summary>
    /// 잠자기 — 다음 날 dayStartHour로 시간을 점프시킨다.
    /// </summary>
    public void Sleep()
    {
        _currentDay++;

        if (_currentDay > _settings.daysPerSeason)
        {
            HandleNewSeason();
        }

        _currentMinuteRaw = _settings.dayStartHour * 60f;
        _lastHour = CurrentHour;
        _isDayTime = true;

        OnDayChanged?.Invoke(_currentDay, _currentSeason);
        OnDayStarted?.Invoke();
        OnTimeUpdated?.Invoke(CurrentHourNormalized);

        Debug.Log($"[TimeManager] 기상: {_currentYear}년차 {_currentSeason} {_currentDay}일 {CurrentHour}:00");
    }
}
