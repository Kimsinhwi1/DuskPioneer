using System;

/// <summary>
/// 농장 타일의 상태 열거형.
/// </summary>
public enum FarmTileState
{
    Empty = 0,      // 잔디 (기본 상태, 오버레이 없음)
    Tilled = 1,     // 경작지 (괭이 사용 후)
    Watered = 2,    // 물 줌 (물뿌리개 사용 후) — 하위 호환용
    Planted = 3     // 씨앗 심음
}

/// <summary>
/// 개별 농장 타일의 데이터. 직렬화 가능하여 세이브/로드에 사용.
/// </summary>
[Serializable]
public struct FarmTileData
{
    public FarmTileState state;
    public bool isWatered;
    public int cropIndex;   // FarmManager.crops 배열 인덱스 (-1 = 작물 없음)
    public int growthDay;   // 성장 진행 일수

    /// <summary>
    /// 경작 상태로 초기화된 데이터를 반환한다.
    /// </summary>
    public static FarmTileData CreateTilled()
    {
        return new FarmTileData { state = FarmTileState.Tilled, isWatered = false, cropIndex = -1, growthDay = 0 };
    }

    /// <summary>
    /// 씨앗 심은 상태로 초기화된 데이터를 반환한다.
    /// </summary>
    public static FarmTileData CreatePlanted(int cropIdx)
    {
        return new FarmTileData { state = FarmTileState.Planted, isWatered = false, cropIndex = cropIdx, growthDay = 0 };
    }
}
