using System;
using System.Collections.Generic;

/// <summary>
/// 스테이지 타입: 일반(랜덤 등장) / 도전(순서대로 등장)
/// </summary>
public enum StageType
{
    Normal,
    Challenge
}

/// <summary>
/// 런타임 스테이지 데이터
/// </summary>
[Serializable]
public class StageData
{
    public string stageId;
    public string stageName;
    public StageType stageType;
    public List<string> monsterIds;

    public StageData(string id, string name, StageType type, List<string> monsterIds)
    {
        stageId = id ?? "";
        stageName = name ?? "";
        stageType = type;
        this.monsterIds = monsterIds != null ? new List<string>(monsterIds) : new List<string>();
    }
}
