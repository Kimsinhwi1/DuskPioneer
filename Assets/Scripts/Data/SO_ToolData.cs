using UnityEngine;

/// <summary>
/// 도구 타입 열거형.
/// </summary>
public enum ToolType
{
    Hoe = 0,          // 괭이 (땅 파기)
    WateringCan = 1,  // 물뿌리개 (물주기)
    Axe = 2,          // 도끼 (나무 베기)
    Shovel = 3        // 삽 (땅 파기)
}

/// <summary>
/// 도구 데이터를 정의하는 ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "SO_ToolData", menuName = "DuskPioneer/Tool Data")]
public class SO_ToolData : ScriptableObject
{
    [Header("기본 정보")]
    public string toolName;
    public ToolType toolType;
    public Sprite icon;

    [Header("사용 비용")]
    [Tooltip("도구 사용 시 소모되는 스태미나")]
    public float staminaCost = 5f;

    [Header("애니메이션")]
    [Tooltip("도구 사용 애니메이션 속도 (프레임/초)")]
    public float animationSpeed = 10f;
}
