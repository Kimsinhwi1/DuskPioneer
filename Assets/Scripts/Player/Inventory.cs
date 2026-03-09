using System;
using UnityEngine;

/// <summary>
/// 인벤토리 슬롯 데이터.
/// </summary>
[Serializable]
public struct InventorySlot
{
    public SO_ItemData item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;
}

/// <summary>
/// 30슬롯 인벤토리를 관리하는 컴포넌트.
/// Player 오브젝트에 부착. FarmManager.OnCropHarvested를 구독하여 수확물을 자동 추가한다.
/// </summary>
public class Inventory : MonoBehaviour
{
    // ── 상수 ──
    public const int MAX_SLOTS = 30;

    // ── 시작 아이템 (에디터에서 설정) ──
    [Header("시작 아이템")]
    [SerializeField] private SO_ItemData[] _startItems;
    [SerializeField] private int[] _startQuantities;

    // ── 수확 시 추가할 아이템 매핑 ──
    [Header("수확물 아이템 매핑")]
    [Tooltip("FarmManager._crops 순서와 동일하게 매핑")]
    [SerializeField] private SO_ItemData[] _harvestItems;

    // ── 슬롯 배열 ──
    private InventorySlot[] _slots;

    // ── 참조 ──
    private FarmManager _farmManager;

    // ── 이벤트 ──
    /// <summary>특정 슬롯이 변경될 때 발행. param: 슬롯 인덱스.</summary>
    public event Action<int> OnInventoryChanged;

    /// <summary>아이템 추가 시 발행 (HUD 알림용). param: (아이템 데이터, 수량).</summary>
    public event Action<SO_ItemData, int> OnItemAdded;

    // ── 읽기 전용 프로퍼티 ──
    public int SlotCount => MAX_SLOTS;

    // ──────────────────────────────────────
    //  생명주기
    // ──────────────────────────────────────

    private void Awake()
    {
        _slots = new InventorySlot[MAX_SLOTS];
    }

    private void Start()
    {
        // FarmManager 구독
        _farmManager = FindFirstObjectByType<FarmManager>();
        if (_farmManager != null)
            _farmManager.OnCropHarvested += OnCropHarvested;

        // 시작 아이템 추가
        if (_startItems != null)
        {
            for (int i = 0; i < _startItems.Length; i++)
            {
                if (_startItems[i] == null) continue;
                int qty = (_startQuantities != null && i < _startQuantities.Length)
                    ? _startQuantities[i] : 1;
                AddItem(_startItems[i], qty);
            }
        }
    }

    private void OnDestroy()
    {
        if (_farmManager != null)
            _farmManager.OnCropHarvested -= OnCropHarvested;
    }

    // ──────────────────────────────────────
    //  공개 메서드
    // ──────────────────────────────────────

    /// <summary>
    /// 아이템을 인벤토리에 추가한다.
    /// 기존 스택에 먼저 추가하고, 없으면 빈 슬롯에 넣는다.
    /// </summary>
    /// <returns>추가 성공 여부.</returns>
    public bool AddItem(SO_ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;

        int remaining = amount;

        // 1단계: 기존 스택에 추가
        for (int i = 0; i < MAX_SLOTS && remaining > 0; i++)
        {
            if (_slots[i].item == itemData && _slots[i].quantity < itemData.maxStack)
            {
                int canAdd = Mathf.Min(remaining, itemData.maxStack - _slots[i].quantity);
                _slots[i].quantity += canAdd;
                remaining -= canAdd;
                OnInventoryChanged?.Invoke(i);
            }
        }

        // 2단계: 빈 슬롯에 추가
        for (int i = 0; i < MAX_SLOTS && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty)
            {
                int canAdd = Mathf.Min(remaining, itemData.maxStack);
                _slots[i].item = itemData;
                _slots[i].quantity = canAdd;
                remaining -= canAdd;
                OnInventoryChanged?.Invoke(i);
            }
        }

        if (remaining < amount)
        {
            int added = amount - remaining;
            OnItemAdded?.Invoke(itemData, added);
            Debug.Log($"[Inventory] {itemData.itemName} x{added} 추가");
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"[Inventory] 가방이 가득 찼습니다! {remaining}개 추가 불가.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 슬롯 인덱스로 아이템을 제거한다.
    /// </summary>
    public bool RemoveItem(int slotIndex, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= MAX_SLOTS) return false;
        if (_slots[slotIndex].IsEmpty) return false;
        if (_slots[slotIndex].quantity < amount) return false;

        _slots[slotIndex].quantity -= amount;
        if (_slots[slotIndex].quantity <= 0)
        {
            _slots[slotIndex].item = null;
            _slots[slotIndex].quantity = 0;
        }

        OnInventoryChanged?.Invoke(slotIndex);
        return true;
    }

    /// <summary>
    /// SO_ItemData 기준으로 아이템을 제거한다. 첫 번째로 발견된 슬롯에서 제거.
    /// </summary>
    public bool RemoveItemByData(SO_ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;

        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (_slots[i].item == itemData && _slots[i].quantity >= amount)
            {
                return RemoveItem(i, amount);
            }
        }
        return false;
    }

    /// <summary>
    /// 슬롯 데이터를 반환한다.
    /// </summary>
    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= MAX_SLOTS) return default;
        return _slots[index];
    }

    /// <summary>
    /// 해당 아이템을 보유하고 있는지 확인한다.
    /// </summary>
    public bool HasItem(SO_ItemData itemData)
    {
        if (itemData == null) return false;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (_slots[i].item == itemData && _slots[i].quantity > 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 해당 아이템의 총 보유 수량을 반환한다.
    /// </summary>
    public int GetItemCount(SO_ItemData itemData)
    {
        if (itemData == null) return 0;
        int total = 0;
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (_slots[i].item == itemData)
                total += _slots[i].quantity;
        }
        return total;
    }

    /// <summary>
    /// cropIndex에 해당하는 씨앗 SO_ItemData를 인벤토리에서 찾는다.
    /// </summary>
    public SO_ItemData FindSeedForCrop(int cropIndex)
    {
        for (int i = 0; i < MAX_SLOTS; i++)
        {
            if (_slots[i].IsEmpty) continue;
            if (_slots[i].item.itemType != ItemType.Seed) continue;
            if (_slots[i].item.cropData == null) continue;

            // FarmManager.Crops 배열에서 cropIndex와 매칭
            if (_farmManager != null && _farmManager.Crops != null
                && cropIndex >= 0 && cropIndex < _farmManager.Crops.Length)
            {
                if (_slots[i].item.cropData == _farmManager.Crops[cropIndex])
                    return _slots[i].item;
            }
        }
        return null;
    }

    // ──────────────────────────────────────
    //  내부 핸들러
    // ──────────────────────────────────────

    /// <summary>
    /// 작물 수확 시 인벤토리에 수확물을 추가한다.
    /// </summary>
    private void OnCropHarvested(UnityEngine.Vector3Int cellPos, SO_CropData cropData)
    {
        if (cropData == null) return;

        // _harvestItems 매핑에서 찾기
        if (_harvestItems != null && _farmManager != null && _farmManager.Crops != null)
        {
            for (int i = 0; i < _farmManager.Crops.Length; i++)
            {
                if (_farmManager.Crops[i] == cropData && i < _harvestItems.Length && _harvestItems[i] != null)
                {
                    AddItem(_harvestItems[i]);
                    return;
                }
            }
        }

        Debug.LogWarning($"[Inventory] 수확물 아이템 매핑이 없습니다: {cropData.cropName}");
    }
}
