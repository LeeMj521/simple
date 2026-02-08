using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 런타임 데이터 (JSON 로드용)
/// </summary>
[Serializable]
public class SkillData
{
    public string skillId;
    public string skillName;
    /// <summary>쿨타임(초)</summary>
    public float cooldown;
    public string description;
    public string iconPath;
    public int maxSkillLevel;
    /// <summary>Resources 기준 스킬 이펙트 프리팹 경로</summary>
    public string prefabPath;
    /// <summary>런타임 로드된 프리팹. 공격력 0이면 유저 기본 공격력 사용</summary>
    public int damage;

    [NonSerialized] public GameObject prefab;

    public SkillData(string id, string name, float cooldownSec, string desc, string icon, int maxLv, string path, int dmg = 0)
    {
        skillId = id ?? "";
        skillName = name ?? "";
        cooldown = Mathf.Max(0.1f, cooldownSec);
        description = desc ?? "";
        iconPath = icon ?? "";
        maxSkillLevel = Mathf.Max(1, maxLv);
        prefabPath = path ?? "";
        damage = Mathf.Max(0, dmg);
    }

    /// <summary>
    /// prefabPath로 Resources에서 프리팹 로드
    /// </summary>
    public void LoadPrefab()
    {
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"[SkillData] {skillId}: prefabPath가 비어 있습니다.");
            return;
        }
        prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
            Debug.LogWarning($"[SkillData] 프리팹을 찾을 수 없습니다: {prefabPath}");
    }
}

/// <summary>
/// 스킬 JSON 래퍼
/// </summary>
[Serializable]
public class SkillJsonData
{
    public List<SkillJson> skills;
}

[Serializable]
public class SkillJson
{
    public string skillId;
    public string skillName;
    public float cooldown;
    public string description;
    public string iconPath;
    public int maxSkillLevel;
    public string prefabPath;
    public int damage;

    public SkillData ToRuntimeData()
    {
        SkillData data = new SkillData(
            skillId ?? "",
            skillName ?? "",
            cooldown > 0 ? cooldown : 1f,
            description ?? "",
            iconPath ?? "",
            maxSkillLevel > 0 ? maxSkillLevel : 1,
            prefabPath ?? "",
            damage > 0 ? damage : 0
        );
        data.LoadPrefab();
        return data;
    }
}
