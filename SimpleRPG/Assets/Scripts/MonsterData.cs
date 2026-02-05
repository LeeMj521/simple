using System;
using UnityEngine;

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
