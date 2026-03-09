using UnityEngine;

/// <summary>
/// 아이템 종류 열거형.
/// </summary>
public enum ItemType
{
    Seed,       // 씨앗
    Crop,       // 수확물
    Tool,       // 도구
    Material,   // 재료
    Food,       // 음식
    Equipment   // 장비
}

/// <summary>
/// 아이템 데이터를 정의하는 ScriptableObject.
/// 모든 아이템(씨앗, 작물, 도구 등)의 기본 정보를 담는다.
/// </summary>
[CreateAssetMenu(fileName = "SO_ItemData", menuName = "DuskPioneer/Item Data")]
public class SO_ItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;

    [TextArea(2, 4)]
    public string description;

    public ItemType itemType;

    [Header("아이콘")]
    public Sprite icon;

    [Header("스택")]
    [Tooltip("한 슬롯에 쌓을 수 있는 최대 수량")]
    public int maxStack = 99;

    [Header("경제")]
    public int sellPrice;

    [Header("씨앗 전용")]
    [Tooltip("Seed 타입일 때 연결할 작물 데이터")]
    public SO_CropData cropData;
}
