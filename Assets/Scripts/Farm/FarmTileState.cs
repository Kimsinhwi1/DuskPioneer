using System;

/// <summary>
/// 농장 타일의 상태 열거형.
/// </summary>
public enum FarmTileState
{
    Empty = 0,      // 잔디 (기본 상태, 오버레이 없음)
    Tilled = 1,     // 경작지 (괭이 사용 후)
    Watered = 2,    // 물 줌 (물뿌리개 사용 후) — 하위 호환용
    Planted = 3     // 씨앗 심음 (Phase 1 Step 4)
}

/// <summary>
/// 개별 농장 타일의 데이터. 직렬화 가능하여 세이브/로드에 사용.
/// </summary>
[Serializable]
public struct FarmTileData
{
    public FarmTileState state;
    public bool isWatered;       // 물 상태 (Tilled+Watered, Planted+Watered 모두 가능)
    // public string cropId;     // Phase 1 Step 4에서 추가
    // public int growthDay;     // Phase 1 Step 4에서 추가

    /// <summary>
    /// 경작 상태로 초기화된 데이터를 반환한다.
    /// </summary>
    public static FarmTileData CreateTilled()
    {
        return new FarmTileData { state = FarmTileState.Tilled, isWatered = false };
    }
}
