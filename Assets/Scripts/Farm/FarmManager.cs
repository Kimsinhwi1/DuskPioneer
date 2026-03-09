using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 농장 타일 상태 관리 및 도구 상호작용을 처리하는 매니저.
/// ToolController.OnToolUsed 이벤트를 구독하여 타일 상태를 변경한다.
/// TimeManager.OnDayChanged를 구독하여 매일 물 마름 + 작물 성장을 처리한다.
/// </summary>
public class FarmManager : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private SO_FarmSettings _settings;

    [Header("작물 데이터")]
    [SerializeField] private SO_CropData[] _crops;

    [Header("타일맵 참조")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Tilemap _farmOverlayTilemap;

    // ── 타일 상태 저장소 ──
    private Dictionary<Vector3Int, FarmTileData> _farmTiles = new();

    // ── 참조 (런타임 검색) ──
    private ToolController _toolController;

    // ── C# 이벤트 ──
    /// <summary>타일 상태 변경 시 발행.</summary>
    public event Action<Vector3Int, FarmTileData> OnTileStateChanged;

    /// <summary>작물 수확 시 발행. param: (셀 좌표, 수확된 작물 데이터).</summary>
    public event Action<Vector3Int, SO_CropData> OnCropHarvested;

    // ── 읽기 전용 프로퍼티 ──
    public int TilledTileCount => _farmTiles.Count;
    public SO_CropData[] Crops => _crops;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            _toolController = player.GetComponent<ToolController>();
            if (_toolController != null)
                _toolController.OnToolUsed += OnToolUsed;
        }

        if (TimeManager.Instance != null)
            TimeManager.Instance.OnDayChanged += OnDayChanged;

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

    private void OnToolUsed(ToolType toolType, Vector2 worldPos)
    {
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
        }
    }

    // ──────────────────────────────────────
    //  타일 상호작용 메서드
    // ──────────────────────────────────────

    /// <summary>
    /// 괭이(Hoe)로 잔디 타일을 경작지로 변환한다.
    /// </summary>
    private void TryTillTile(Vector3Int cellPos)
    {
        if (_farmTiles.ContainsKey(cellPos)) return;

        TileBase currentTile = _groundTilemap.GetTile(cellPos);
        if (currentTile == null || currentTile != _settings.grassTile)
        {
            Debug.Log($"[Farm] 경작 불가: ({cellPos}) 잔디 타일이 아닙니다.");
            return;
        }

        var data = FarmTileData.CreateTilled();
        _farmTiles[cellPos] = data;
        UpdateTileVisual(cellPos, data);
        OnTileStateChanged?.Invoke(cellPos, data);
        Debug.Log($"[Farm] 경작 완료: ({cellPos})");
    }

    /// <summary>
    /// 물뿌리개(WateringCan)로 경작지/작물에 물을 준다.
    /// </summary>
    private void TryWaterTile(Vector3Int cellPos)
    {
        if (!_farmTiles.TryGetValue(cellPos, out var data)) return;
        if (data.state == FarmTileState.Empty) return;
        if (data.isWatered) return;

        data.isWatered = true;
        _farmTiles[cellPos] = data;
        UpdateTileVisual(cellPos, data);
        OnTileStateChanged?.Invoke(cellPos, data);
        Debug.Log($"[Farm] 물주기 완료: ({cellPos})");
    }

    /// <summary>
    /// 삽(Shovel)으로 경작지를 잔디로 되돌린다. 작물 있으면 불가.
    /// </summary>
    private void TryRevertTile(Vector3Int cellPos)
    {
        if (!_farmTiles.TryGetValue(cellPos, out var data)) return;
        if (data.state == FarmTileState.Planted) return;

        _farmTiles.Remove(cellPos);
        _farmOverlayTilemap.SetTile(cellPos, null);
        OnTileStateChanged?.Invoke(cellPos, new FarmTileData { state = FarmTileState.Empty });
        Debug.Log($"[Farm] 경작지 되돌림: ({cellPos})");
    }

    // ──────────────────────────────────────
    //  비주얼 갱신
    // ──────────────────────────────────────

    /// <summary>
    /// FarmOverlay 타일맵에 상태에 맞는 타일을 배치한다.
    /// Planted 상태일 때 성장 단계 스프라이트를 동적으로 적용.
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
                // 배경: 물 준 흙 or 마른 흙
                TileBase soilTile = data.isWatered ? _settings.wateredTile : _settings.tilledTile;
                _farmOverlayTilemap.SetTile(cellPos, soilTile);

                // 작물 스프라이트를 SpriteRenderer 오버레이로 표시하는 대신,
                // Tile의 sprite를 동적으로 교체
                if (data.cropIndex >= 0 && data.cropIndex < _crops.Length)
                {
                    var cropData = _crops[data.cropIndex];
                    int stageIdx = cropData.GetStageIndex(data.growthDay);
                    if (cropData.growthStageSprites != null && stageIdx < cropData.growthStageSprites.Length)
                    {
                        var cropSprite = cropData.growthStageSprites[stageIdx];
                        if (cropSprite != null)
                        {
                            // 동적 Tile 생성하여 작물 표시
                            var cropTile = ScriptableObject.CreateInstance<Tile>();
                            cropTile.sprite = cropSprite;
                            cropTile.color = data.isWatered
                                ? new Color(0.55f, 0.45f, 0.35f, 1f)
                                : Color.white;
                            _farmOverlayTilemap.SetTile(cellPos, cropTile);
                            return; // 작물 타일 설정 완료
                        }
                    }
                }
                return; // 이미 soilTile 설정됨

            default:
                tile = null;
                break;
        }

        _farmOverlayTilemap.SetTile(cellPos, tile);
    }

    // ──────────────────────────────────────
    //  시간 이벤트 핸들러
    // ──────────────────────────────────────

    /// <summary>
    /// 매일: 물 준 작물 성장 + 모든 타일 물 마름.
    /// </summary>
    private void OnDayChanged(int day, Season season)
    {
        var keys = new List<Vector3Int>(_farmTiles.Keys);
        int grownCount = 0;

        foreach (var key in keys)
        {
            var data = _farmTiles[key];

            // 물 준 작물만 성장
            if (data.state == FarmTileState.Planted && data.isWatered && data.cropIndex >= 0)
            {
                data.growthDay++;
                grownCount++;
            }

            // 물 마름
            data.isWatered = false;
            _farmTiles[key] = data;
            UpdateTileVisual(key, data);
        }

        if (grownCount > 0)
            Debug.Log($"[Farm] 새 날: {grownCount}개 작물 성장, 전체 물 마름.");
    }

    // ──────────────────────────────────────
    //  공개 메서드
    // ──────────────────────────────────────

    /// <summary>
    /// 해당 셀의 농장 타일 데이터를 반환한다. 없으면 Empty.
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
    /// 해당 셀에 씨앗을 심는다.
    /// </summary>
    public void PlantSeed(Vector3Int cellPos, int cropIndex)
    {
        if (!CanPlant(cellPos)) return;
        if (cropIndex < 0 || cropIndex >= _crops.Length) return;

        var prevData = _farmTiles[cellPos];
        var data = FarmTileData.CreatePlanted(cropIndex);
        data.isWatered = prevData.isWatered; // 이미 물 준 상태 유지
        _farmTiles[cellPos] = data;
        UpdateTileVisual(cellPos, data);
        OnTileStateChanged?.Invoke(cellPos, data);
        Debug.Log($"[Farm] 씨앗 심기: ({cellPos}) → {_crops[cropIndex].cropName}");
    }

    /// <summary>
    /// 성장 완료된 작물을 수확한다. 수확 후 Tilled 상태로 복귀.
    /// </summary>
    public bool TryHarvest(Vector3Int cellPos)
    {
        if (!_farmTiles.TryGetValue(cellPos, out var data)) return false;
        if (data.state != FarmTileState.Planted) return false;
        if (data.cropIndex < 0 || data.cropIndex >= _crops.Length) return false;

        var cropData = _crops[data.cropIndex];
        if (!cropData.IsMature(data.growthDay)) return false;

        // 수확 성공 → Tilled 복귀
        var newData = FarmTileData.CreateTilled();
        _farmTiles[cellPos] = newData;
        UpdateTileVisual(cellPos, newData);
        OnTileStateChanged?.Invoke(cellPos, newData);
        OnCropHarvested?.Invoke(cellPos, cropData);
        Debug.Log($"[Farm] 수확 완료: ({cellPos}) → {cropData.cropName}");
        return true;
    }

    /// <summary>
    /// 해당 셀이 수확 가능한 상태인지 반환한다.
    /// </summary>
    public bool CanHarvest(Vector3Int cellPos)
    {
        if (!_farmTiles.TryGetValue(cellPos, out var data)) return false;
        if (data.state != FarmTileState.Planted || data.cropIndex < 0) return false;
        if (data.cropIndex >= _crops.Length) return false;
        return _crops[data.cropIndex].IsMature(data.growthDay);
    }

    /// <summary>
    /// 월드 좌표를 Ground 타일맵의 셀 좌표로 변환한다.
    /// </summary>
    public Vector3Int WorldToCell(Vector2 worldPos)
    {
        return _groundTilemap.WorldToCell(worldPos);
    }
}
