using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데이터 매니저 - JSON 데이터 로드 관리
/// </summary>
public class DataManager : MonoBehaviour
{
    [Header("JSON 데이터 경로 (Resources 폴더 기준)")]
    [SerializeField] private string npcJsonPath = "Data/NPCs";
    [SerializeField] private string itemJsonPath = "Data/Items";
    [SerializeField] private string monsterJsonPath = "Data/Monsters";
    [SerializeField] private string skillJsonPath = "Data/Skills";

    private Dictionary<string, NPCData> loadedNPCs = new Dictionary<string, NPCData>();
    private Dictionary<string, ItemData> loadedItems = new Dictionary<string, ItemData>();
    private Dictionary<string, MonsterData> loadedMonsters = new Dictionary<string, MonsterData>();
    private Dictionary<string, SkillData> loadedSkills = new Dictionary<string, SkillData>();
    
    private void Awake()
    {
        LoadAllData();
    }
    
    /// <summary>
    /// 모든 데이터 로드
    /// </summary>
    public void LoadAllData()
    {
        loadedNPCs.Clear();
        loadedItems.Clear();
        loadedMonsters.Clear();
        loadedSkills.Clear();

        LoadNPCsFromJSON();
        LoadItemsFromJSON();
        LoadMonstersFromJSON();
        LoadSkillsFromJSON();
    }
    
    /// <summary>
    /// JSON에서 NPC 데이터 로드
    /// </summary>
    private void LoadNPCsFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(npcJsonPath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"NPC JSON 파일을 찾을 수 없습니다: {npcJsonPath}");
            return;
        }
        
        try
        {
            NPCJsonData jsonData = JsonUtility.FromJson<NPCJsonData>(jsonFile.text);
            if (jsonData != null && jsonData.npcs != null)
            {
                Debug.Log($"NPC JSON 파일 로드 성공: {jsonData.npcs.Count}개의 NPC 발견");
                foreach (var npcJson in jsonData.npcs)
                {
                    NPCData npcData = npcJson.ToRuntimeData();
                    if (!loadedNPCs.ContainsKey(npcData.npcId))
                    {
                        loadedNPCs[npcData.npcId] = npcData;
                        Debug.Log($"NPC 로드: {npcData.npcId} - {npcData.name}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("NPC JSON 데이터가 비어있거나 형식이 올바르지 않습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"NPC JSON 파싱 오류: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// JSON에서 아이템 데이터 로드
    /// </summary>
    private void LoadItemsFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(itemJsonPath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"아이템 JSON 파일을 찾을 수 없습니다: {itemJsonPath}");
            return;
        }
        
        try
        {
            ItemJsonData jsonData = JsonUtility.FromJson<ItemJsonData>(jsonFile.text);
            if (jsonData != null && jsonData.items != null)
            {
                Debug.Log($"아이템 JSON 파일 로드 성공: {jsonData.items.Count}개의 아이템 발견");
                foreach (var itemJson in jsonData.items)
                {
                    ItemData itemData = itemJson.ToRuntimeData();
                    if (!loadedItems.ContainsKey(itemData.itemId))
                    {
                        loadedItems[itemData.itemId] = itemData;
                        Debug.Log($"아이템 로드: {itemData.itemId} - {itemData.itemName}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("아이템 JSON 데이터가 비어있거나 형식이 올바르지 않습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"아이템 JSON 파싱 오류: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 로드된 NPC 데이터 가져오기
    /// </summary>
    public Dictionary<string, NPCData> GetLoadedNPCs()
    {
        return new Dictionary<string, NPCData>(loadedNPCs);
    }
    
    /// <summary>
    /// 로드된 아이템 데이터 가져오기
    /// </summary>
    public Dictionary<string, ItemData> GetLoadedItems()
    {
        return new Dictionary<string, ItemData>(loadedItems);
    }
    
    /// <summary>
    /// 특정 NPC 데이터 가져오기
    /// </summary>
    public NPCData GetNPC(string npcId)
    {
        return loadedNPCs.ContainsKey(npcId) ? loadedNPCs[npcId] : null;
    }
    
    /// <summary>
    /// 특정 아이템 데이터 가져오기
    /// </summary>
    public ItemData GetItem(string itemId)
    {
        return loadedItems.ContainsKey(itemId) ? loadedItems[itemId] : null;
    }
    
    /// <summary>
    /// JSON에서 몬스터 데이터 로드
    /// </summary>
    private void LoadMonstersFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(monsterJsonPath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"몬스터 JSON 파일을 찾을 수 없습니다: {monsterJsonPath}");
            return;
        }
        
        try
        {
            MonsterJsonData jsonData = JsonUtility.FromJson<MonsterJsonData>(jsonFile.text);
            if (jsonData != null && jsonData.monsters != null)
            {
                Debug.Log($"몬스터 JSON 파일 로드 성공: {jsonData.monsters.Count}개의 몬스터 발견");
                foreach (var monsterJson in jsonData.monsters)
                {
                    MonsterData monsterData = monsterJson.ToRuntimeData();
                    if (!loadedMonsters.ContainsKey(monsterData.monsterId))
                    {
                        loadedMonsters[monsterData.monsterId] = monsterData;
                        Debug.Log($"몬스터 로드: {monsterData.monsterId} - {monsterData.monsterName}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("몬스터 JSON 데이터가 비어있거나 형식이 올바르지 않습니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"몬스터 JSON 파싱 오류: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 로드된 몬스터 데이터 가져오기
    /// </summary>
    public Dictionary<string, MonsterData> GetLoadedMonsters()
    {
        return new Dictionary<string, MonsterData>(loadedMonsters);
    }
    
    /// <summary>
    /// 특정 몬스터 데이터 가져오기
    /// </summary>
    public MonsterData GetMonster(string monsterId)
    {
        return loadedMonsters.ContainsKey(monsterId) ? loadedMonsters[monsterId] : null;
    }

    /// <summary>
    /// JSON에서 스킬 데이터 로드
    /// </summary>
    private void LoadSkillsFromJSON()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(skillJsonPath);
        if (jsonFile == null)
        {
            Debug.LogWarning($"스킬 JSON 파일을 찾을 수 없습니다: {skillJsonPath}");
            return;
        }
        try
        {
            SkillJsonData jsonData = JsonUtility.FromJson<SkillJsonData>(jsonFile.text);
            if (jsonData != null && jsonData.skills != null)
            {
                foreach (var skillJson in jsonData.skills)
                {
                    SkillData data = skillJson.ToRuntimeData();
                    if (!string.IsNullOrEmpty(data.skillId) && !loadedSkills.ContainsKey(data.skillId))
                    {
                        loadedSkills[data.skillId] = data;
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"스킬 JSON 파싱 오류: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>로드된 스킬 딕셔너리</summary>
    public Dictionary<string, SkillData> GetLoadedSkills()
    {
        return new Dictionary<string, SkillData>(loadedSkills);
    }

    /// <summary>특정 스킬 데이터 가져오기</summary>
    public SkillData GetSkill(string skillId)
    {
        return loadedSkills != null && loadedSkills.TryGetValue(skillId, out var data) ? data : null;
    }
}

/// <summary>
/// NPC JSON 데이터 구조
/// </summary>
[Serializable]
public class NPCJsonData
{
    public List<NPCJson> npcs;
}

[Serializable]
public class OnlineWindowJson
{
    public int startHour;
    public int startMinute;
    public int durationHours; // 머무는 시간 (시간)
    public int durationMinutes; // 머무는 시간 (분)
    /// <summary>접속 시간에 적용할 랜덤 오프셋(분). 같은 날에는 고정.</summary>
    public int startOffsetMinutes = 15;
    /// <summary>나가는 시간에 적용할 랜덤 오프셋(분). 같은 날에는 고정.</summary>
    public int endOffsetMinutes = 15;
}

[Serializable]
public class NPCJson
{
    public string npcId;
    public string npcName;
    public string job;
    public string behaviorType;
    public string behaviorExample;
    public float speakProbability = 0.3f; // 말할 확률
    public float responseProbability = 0.4f; // 답장 확률
    public float relationshipBonus = 0.3f; // 호감도 보너스 (호감도 100일 때 최대 보너스)
    public List<RelationshipJson> initialRelationships;

    // 접속 시간대 (게임 시간). 비어 있으면 스케줄 없음
    public List<OnlineWindowJson> onlineSchedule;
    
    // 전투 관련 (선택사항, 없으면 기본값 사용)
    public int attackPower = 0; // 0이면 기본값 사용
    public float attackCooldown = 0f; // 0이면 기본값 사용
    /// <summary>장착한 스킬 ID 목록 (Skills.json의 skillId)</summary>
    public List<string> equippedSkillIds;

    // 시각적 표현
    public string spritePath = ""; // Resources 폴더 기준 스프라이트 경로
    
    public NPCData ToRuntimeData()
    {
        Job jobEnum = Job.무직;
        if (!string.IsNullOrEmpty(job) && !System.Enum.TryParse(job, true, out jobEnum))
            jobEnum = Job.무직;

        NPCData data = new NPCData(npcId, npcName)
        {
            job = jobEnum,
            behaviorType = behaviorType,
            behaviorExample = behaviorExample,
            speakProbability = speakProbability,
            responseProbability = responseProbability,
            relationshipBonus = relationshipBonus,
            spritePath = spritePath ?? ""
        };
        
        // 전투 관련 설정 (JSON에 없으면 기본값 사용)
        if (attackPower > 0)
            data.attackPower = attackPower;
        if (attackCooldown > 0)
            data.attackCooldown = attackCooldown;
        if (equippedSkillIds != null && equippedSkillIds.Count > 0)
        {
            data.equippedSkillIds = new List<string>();
            foreach (string id in equippedSkillIds)
            {
                if (!string.IsNullOrEmpty(id))
                    data.equippedSkillIds.Add(id);
            }
        }
        if (data.equippedSkillIds == null)
            data.equippedSkillIds = new List<string>();

        // 접속 시간대 변환 (시작 시간 + 머무는 시간)
        if (onlineSchedule != null && onlineSchedule.Count > 0)
        {
            data.onlineSchedule = new List<OnlineWindow>();
            foreach (var w in onlineSchedule)
            {
                int start = Mathf.Clamp(w.startHour * 60 + w.startMinute, 0, 1440);
                int duration = w.durationHours * 60 + w.durationMinutes;
                
                if (duration > 0)
                {
                    data.onlineSchedule.Add(new OnlineWindow 
                    { 
                        startMinute = start, 
                        durationMinutes = duration,
                        startOffsetMinutes = Mathf.Max(0, w.startOffsetMinutes),
                        endOffsetMinutes = Mathf.Max(0, w.endOffsetMinutes)
                    });
                }
            }
        }
        
        // 스프라이트 로드
        data.LoadSprite();
        
        if (initialRelationships != null)
        {
            foreach (var rel in initialRelationships)
            {
                if (string.IsNullOrEmpty(rel.targetId)) continue;
                float relationshipValue = Mathf.Clamp(rel.value, -100f, 100f);
                data.SetRelationship(rel.targetId, relationshipValue);
            }
        }
        // initialRelationships에 없으면 기본값 0 (초면) - GetRelationship에서 자동 처리됨
        
        return data;
    }
}

[Serializable]
public class RelationshipJson
{
    public string targetId;
    public float value;
}

/// <summary>
/// 아이템 JSON 데이터 구조
/// </summary>
[Serializable]
public class ItemJsonData
{
    public List<ItemJson> items;
}

[Serializable]
public class ItemJson
{
    public string itemId;
    public string itemName;
    public string rarity;
    public string description;
    public string iconPath;

    public ItemData ToRuntimeData()
    {
        ItemRarity itemRarity = Enum.TryParse<ItemRarity>(rarity, out var result)
            ? result
            : ItemRarity.Common;

        ItemData data = new ItemData(itemId, itemName, itemRarity, description ?? "")
        {
            iconPath = iconPath ?? ""
        };
        data.LoadIcon();
        return data;
    }
}

/// <summary>
/// 몬스터 JSON 데이터 구조
/// </summary>
[Serializable]
public class MonsterJsonData
{
    public List<MonsterJson> monsters;
}

[Serializable]
public class MonsterJson
{
    public string monsterId;
    public string monsterName;
    public string prefabPath; // Resources 폴더 기준 프리팹 경로 (예: "Monsters/Goblin")
    public int maxHP;
    public int level;
    public int expReward;
    public int goldReward;
    
    public MonsterData ToRuntimeData()
    {
        MonsterData data = new MonsterData(
            monsterId, 
            monsterName, 
            maxHP, 
            level, 
            expReward, 
            goldReward, 
            prefabPath ?? ""
        );
        
        // 프리팹 로드
        data.LoadPrefab();
        
        return data;
    }
}
