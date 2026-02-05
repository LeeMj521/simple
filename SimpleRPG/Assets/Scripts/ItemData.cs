using System;
using UnityEngine;

/// <summary>
/// 아이템의 등급/품질을 나타내는 열거형
/// </summary>
public enum ItemRarity
{
    Common,     // 일반 (흰색)
    Uncommon,   // 고급 (녹색)
    Rare,       // 희귀 (파란색)
    Epic,       // 영웅 (보라색)
    Legendary   // 전설 (주황색)
}

/// <summary>
/// 아이템 데이터 클래스
/// </summary>
[Serializable]
public class ItemData
{
    public string itemId;
    public string itemName;
    public ItemRarity rarity;
    public string description;
    /// <summary>Resources 폴더 기준 아이콘 경로</summary>
    public string iconPath;
    [NonSerialized] public Sprite icon;

    public ItemData(string id, string name, ItemRarity itemRarity, string desc = "")
    {
        itemId = id;
        itemName = name;
        rarity = itemRarity;
        description = desc;
    }

    /// <summary>
    /// iconPath로부터 아이콘 스프라이트를 Resources에서 로드합니다.
    /// </summary>
    public void LoadIcon()
    {
        if (string.IsNullOrEmpty(iconPath))
            return;
        icon = Resources.Load<Sprite>(iconPath);
        if (icon == null)
            UnityEngine.Debug.LogWarning($"[ItemData] 아이콘을 찾을 수 없습니다: {iconPath}");
    }

    /// <summary>
    /// 등급에 따른 색상을 반환합니다
    /// </summary>
    public Color GetRarityColor()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Color.white;
            case ItemRarity.Uncommon:
                return Color.green;
            case ItemRarity.Rare:
                return Color.cyan;
            case ItemRarity.Epic:
                return new Color(0.5f, 0f, 1f); // 보라색
            case ItemRarity.Legendary:
                return new Color(1f, 0.5f, 0f); // 주황색
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// 등급 이름을 반환합니다
    /// </summary>
    public string GetRarityName()
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return "일반";
            case ItemRarity.Uncommon:
                return "고급";
            case ItemRarity.Rare:
                return "희귀";
            case ItemRarity.Epic:
                return "영웅";
            case ItemRarity.Legendary:
                return "전설";
            default:
                return "일반";
        }
    }
}
