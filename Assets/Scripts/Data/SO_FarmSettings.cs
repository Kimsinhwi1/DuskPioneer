using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 농장 타일 상태별 비주얼 및 설정을 저장하는 ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "SO_FarmSettings", menuName = "DuskPioneer/Farm Settings")]
public class SO_FarmSettings : ScriptableObject
{
    [Header("타일 상태별 타일 에셋")]
    [Tooltip("경작지 (괭이 사용 후)")]
    public TileBase tilledTile;

    [Tooltip("물 준 경작지")]
    public TileBase wateredTile;

    [Tooltip("씨앗 심은 타일 (Phase 1 Step 4에서 사용)")]
    public TileBase plantedTile;

    [Header("허용 대상 타일")]
    [Tooltip("괭이로 경작 가능한 기본 타일 (잔디)")]
    public TileBase grassTile;
}
