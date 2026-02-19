using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 드랍 테이블 엔트리 (아이템 ID와 드랍 확률)
/// </summary>
[Serializable]
public class DropTableEntry
{
    public string itemId;
    [Tooltip("드랍 확률 (0~100)")]
    public float dropRate;

    public DropTableEntry()
    {
        itemId = "";
        dropRate = 0f;
    }

    public DropTableEntry(string id, float rate)
    {
        itemId = id;
        dropRate = Mathf.Clamp(rate, 0f, 100f);
    }
}

/// <summary>
/// 몬스터 데이터 클래스
/// </summary>
[Serializable]
public class MonsterData
{
    public string monsterId;
    public string monsterName;
    public int maxHP;
    public int level;
    public int expReward;
    public int goldReward;
    
    /// <summary>Resources 폴더 기준 프리팹 경로 (예: Prefabs/Monsters/Goblin)</summary>
    public string prefabPath;
    
    /// <summary>런타임에서 로드된 프리팹 참조</summary>
    [NonSerialized] public GameObject prefab;

    /// <summary>드랍 테이블 (아이템 ID와 드랍 확률 목록)</summary>
    public List<DropTableEntry> dropTable = new List<DropTableEntry>();

    public MonsterData(string id, string name, int hp, int lv, int exp, int gold, string path)
    {
        monsterId = id;
        monsterName = name;
        maxHP = hp;
        level = lv;
        expReward = exp;
        goldReward = gold;
        prefabPath = path ?? "";
    }

    /// <summary>
    /// prefabPath로부터 프리팹을 Resources에서 로드합니다.
    /// </summary>
    public void LoadPrefab()
    {
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"[MonsterData] {monsterId}: prefabPath가 비어 있습니다.");
            return;
        }

        prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[MonsterData] 프리팹을 찾을 수 없습니다: {prefabPath}");
        }
    }
}
