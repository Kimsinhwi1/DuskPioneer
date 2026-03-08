using UnityEngine;

/// <summary>
/// 낮/밤 시각 전환에 사용되는 색상 및 밝기 설정 데이터.
/// Gradient의 time 축: 0.0 = 00:00, 1.0 = 24:00.
/// </summary>
[CreateAssetMenu(fileName = "SO_DayNightSettings", menuName = "DuskPioneer/DayNight Settings")]
public class SO_DayNightSettings : ScriptableObject
{
    [Tooltip("시간대별 오버레이 색상 (0=자정, 0.5=정오, 1=자정)")]
    public Gradient overlayColorOverDay;

    [Tooltip("시간대별 오버레이 투명도 (0=투명/낮, 0.55=밤 최대 어두움)")]
    public AnimationCurve overlayAlphaCurve;
}
