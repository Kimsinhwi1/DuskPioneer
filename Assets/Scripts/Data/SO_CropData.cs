using UnityEngine;

/// <summary>
/// 작물 데이터. 성장일, 성장 단계 스프라이트, 수확 정보를 담는다.
/// </summary>
[CreateAssetMenu(fileName = "SO_CropData", menuName = "DuskPioneer/Crop Data")]
public class SO_CropData : ScriptableObject
{
    [Header("기본 정보")]
    public string cropName;

    [Header("성장")]
    [Tooltip("총 성장 일수 (물 준 날만 카운트)")]
    public int growthDays = 3;

    [Header("스프라이트")]
    [Tooltip("성장 단계별 타일 스프라이트 (씨앗 → 새싹 → ... → 수확가능). 최소 2개.")]
    public Sprite[] growthStageSprites;

    [Tooltip("수확 시 표시할 아이콘 (인벤토리용, Step 5 준비)")]
    public Sprite harvestedSprite;

    /// <summary>
    /// 현재 growthDay에 해당하는 성장 단계 인덱스를 반환한다.
    /// </summary>
    public int GetStageIndex(int growthDay)
    {
        if (growthStageSprites == null || growthStageSprites.Length == 0) return 0;
        int stages = growthStageSprites.Length;
        // 마지막 스테이지 = 수확가능
        float ratio = Mathf.Clamp01((float)growthDay / growthDays);
        int idx = Mathf.FloorToInt(ratio * (stages - 1));
        return Mathf.Clamp(idx, 0, stages - 1);
    }

    /// <summary>
    /// 성장 완료 여부.
    /// </summary>
    public bool IsMature(int growthDay) => growthDay >= growthDays;
}
