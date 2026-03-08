using UnityEngine;

/// <summary>
/// 게임 시간 시스템 설정값. 시간 흐름 속도, 계절 길이 등.
/// </summary>
[CreateAssetMenu(fileName = "SO_TimeSettings", menuName = "DuskPioneer/Time Settings")]
public class SO_TimeSettings : ScriptableObject
{
    [Header("시간 흐름")]
    [Tooltip("현실 1초당 게임 내 분 수 (기본 1.6 = 현실 15분이 게임 1일)")]
    public float gameMinutesPerRealSecond = 1.6f;

    [Header("하루")]
    [Tooltip("하루 시작 시각 (기상)")]
    public int dayStartHour = 6;

    [Tooltip("밤 시작 시각")]
    public int nightStartHour = 18;

    [Tooltip("강제 수면 시각 (다음날 새벽)")]
    public int forceSleepHour = 2;

    [Header("계절")]
    public int daysPerSeason = 28;
    public int seasonsPerYear = 4;
}
