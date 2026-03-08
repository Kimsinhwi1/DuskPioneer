using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 농장 타일 상태 관리 및 도구 상호작용을 처리하는 매니저.
/// ToolController.OnToolUsed 이벤트를 구독하여 타일 상태를 변경한다.
/// TimeManager.OnDayChanged를 구독하여 매일 물 마름 처리를 수행한다.
/// </summary>
public class FarmManager : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private SO_FarmSettings _settings;

    [Header("타일맵 참조")]
    [SerializeField] private Tilemap _groundTilemap;      // Ground 레이어 (잔디 체크용)
    [SerializeField] private Tilemap _farmOverlayTilemap;  // FarmOverlay 레이어 (비주얼 표시)

    // ── 타일 상태 저장소 ──
    private Dictionary<Vector3Int, FarmTileData> _farmTiles = new();

    // ── 참조 (런타임 검색) ──
    private ToolController _toolController;

    // ── C# 이벤트 ──
    /// <summary>타일 상태 변경 시 발행. param: (셀 좌표, 새 상태).</summary>
    public event Action<Vector3Int, FarmTileData> OnTileStateChanged;

    // ── 읽기 전용 프로퍼티 ──
    public int TilledTileCount => _farmTiles.Count;

    private void Start()
    {
        // Player의 ToolController 찾아서 이벤트 구독
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _toolController = player.GetComponent<ToolController>();
            if (_toolController != null)
                _toolController.OnToolUsed += OnToolUsed;
        }

        // TimeManager 이벤트 구독 (날 변경 시 물 마름)
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged += OnDayChanged;

        // 유효성 체크
        if (_settings == null)
            Debug.LogError("[FarmManager] SO_FarmSettings가 할당되지 않았습니다!");
        if (_groundTilemap == null)
            Debug.LogError("[FarmManager] Ground Tilemap이 할당되지 않았습니다!");
        if (_farmOverlayTilemap == null)
            Debug.LogError("[FarmManager] FarmOverlay Tilemap이 할당되지 않았습니다!");
    }

    private void OnDestroy()
    {
        if (_toolController != null)
            _toolController.OnToolUsed -= OnToolUsed;
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged -= OnDayChanged;
    }

    // ──────────────────────────────────────
    //  도구 이벤트 핸들러
    // ──────────────────────────────────────

    /// <summary>
    /// ToolController.OnToolUsed 이벤트 핸들러.
    /// 도구 타입에 따라 해당 셀의 상태를 변경한다.
    /// </summary>
    private void OnToolUsed(ToolType toolType, Vector2 worldPos)
    {
        // 월드 좌표 → 셀 좌표 변환
        Vector3Int cellPos = _groundTilemap.WorldToCell(worldPos);

        switch (toolType)
        {
            case ToolType.Hoe:
                TryTillTile(cellPos);
                break;
            case ToolType.WateringCan:
                TryWaterTile(cellPos);
                break;
            case ToolType.Shovel:
                TryRevertTile(cellPos);
                break;
            // Axe는 나무/바위 상호작용 (Phase 2+)
        }
    }

    // ──────────────────────────────────────
    //  타일 상호작용 메서드
    // ──────────────────────────────────────

    /// <summary>
    /// 괭이(Hoe)로 잔디 타일을 경작지로 변환한다.
    /// 조건: 해당 셀이 잔디 타일이고 아직 경작되지 않은 경우.
    /// </summary>
    private void TryTillTile(Vector3Int cellPos)
    {
        // 이미 경작된 타일이면 무시
        if (_farmTiles.ContainsKey(cellPos)) return;

        // 잔디 타일인지 확인
        TileBase currentTile = _groundTilemap.GetTile(cellPos);
        if (currentTile == null || currentTile != _settings.grassTile)
        {
            Debug.Log($"[Farm] 경작 불가: ({cellPos}) 잔디 타일이 아닙니다.");
            return;
        }

        // 경작 상태로 전환
        var data = FarmTileData.CreateTilled();
        _farmTiles[cellPos] = data;
        UpdateTileVisual(cellPos, data);

        OnTileStateChanged?.Invoke(cellPos, data);
        Debug.Log($"[Farm] 경작 완료: ({cellPos})");
    }

    /// <summary>
    /// 물뿌리개(WateringCan)로 경작지에 물을 준다.
    /// 조건: 해당 셀이 Tilled 또는 Planted 상태인 경우.
    /// </summary>
    private void TryWaterTile(Vector3Int cellPos)
    {
        if (!_farmTiles.TryGetValue(cellPos, out var data)) return;
        if (data.state == FarmTileState.Empty) return;
        if (data.isWatered) return; // 이미 물 줌

        data.isWatered = true;
        _farmTiles[cellPos] = data;
        UpdateTileVisual(cellPos, data);

        OnTileStateChanged?.Invoke(cellPos, data);
        Debug.Log($"[Farm] 물주기 완료: ({cellPos})");
    }

    /// <summary>
    /// 삽(Shovel)으로 경작지를 잔디로 되돌린다.
    /// 조건: 해당 셀이 경작된 상태이고 작물이 없는 경우.
    /// </summary>
    private void TryRevertTile(Vector3Int cellPos)
    {
        if (!_farmTiles.TryGetValue(cellPos, out var data)) return;
        if (data.state == FarmTileState.Planted) return; // 작물 있으면 되돌리기 불가

        _farmTiles.Remove(cellPos);
        _farmOverlayTilemap.SetTile(cellPos, null); // 오버레이 제거 (잔디가 보임)

        OnTileStateChanged?.Invoke(cellPos, new FarmTileData { state = FarmTileState.Empty });
        Debug.Log($"[Farm] 경작지 되돌림: ({cellPos})");
    }

    // ──────────────────────────────────────
    //  비주얼 갱신
    // ──────────────────────────────────────

    /// <summary>
    /// FarmOverlay 타일맵에 상태에 맞는 타일을 배치한다.
    /// </summary>
    private void UpdateTileVisual(Vector3Int cellPos, FarmTileData data)
    {
        if (_farmOverlayTilemap == null || _settings == null) return;

        TileBase tile = null;

        switch (data.state)
        {
            case FarmTileState.Tilled:
                tile = data.isWatered ? _settings.wateredTile : _settings.tilledTile;
                break;
            case FarmTileState.Planted:
                // Step 4에서 작물 스프라이트로 확장 예정
                tile = data.isWatered ? _settings.wateredTile : _settings.plantedTile;
                // plantedTile이 null이면 tilledTile로 대체
                if (tile == null) tile = data.isWatered ? _settings.wateredTile : _settings.tilledTile;
                break;
            default:
                tile = null; // Empty = 오버레이 없음
                break;
        }

        _farmOverlayTilemap.SetTile(cellPos, tile);
    }

    // ──────────────────────────────────────
    //  시간 이벤트 핸들러
    // ──────────────────────────────────────

    /// <summary>
    /// 매일 아침 모든 물 준 타일의 물 상태를 제거한다 (물 마름).
    /// </summary>
    private void OnDayChanged(int day, Season season)
    {
        var keysToUpdate = new List<Vector3Int>();

        foreach (var kvp in _farmTiles)
        {
            if (kvp.Value.isWatered)
                keysToUpdate.Add(kvp.Key);
        }

        foreach (var key in keysToUpdate)
        {
            var data = _farmTiles[key];
            data.isWatered = false;
            _farmTiles[key] = data;
            UpdateTileVisual(key, data);
        }

        if (keysToUpdate.Count > 0)
            Debug.Log($"[Farm] 새 날: {keysToUpdate.Count}개 타일의 물이 말랐습니다.");
    }

    // ──────────────────────────────────────
    //  공개 메서드 (Phase 1 Step 4 준비)
    // ──────────────────────────────────────

    /// <summary>
    /// 해당 셀의 농장 타일 데이터를 반환한다. 등록되지 않은 셀은 Empty 반환.
    /// </summary>
    public FarmTileData GetTileData(Vector3Int cellPos)
    {
        return _farmTiles.TryGetValue(cellPos, out var data) ? data : default;
    }

    /// <summary>
    /// 해당 셀이 씨앗을 심을 수 있는 상태인지 반환한다.
    /// </summary>
    public bool CanPlant(Vector3Int cellPos)
    {
        return _farmTiles.TryGetValue(cellPos, out var data)
               && data.state == FarmTileState.Tilled;
    }

    /// <summary>
    /// 해당 셀에 씨앗을 심는다 (Step 4에서 구현 확장).
    /// </summary>
    public void PlantSeed(Vector3Int cellPos /*, SO_CropData crop */)
    {
        if (!CanPlant(cellPos)) return;

        var data = _farmTiles[cellPos];
        data.state = FarmTileState.Planted;
        _farmTiles[cellPos] = data;
        UpdateTileVisual(cellPos, data);
        OnTileStateChanged?.Invoke(cellPos, data);
        Debug.Log($"[Farm] 씨앗 심기 완료: ({cellPos})");
    }
}
