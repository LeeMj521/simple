using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유저/NPC 직업
/// </summary>
public enum Job
{
    무직,

    전사,
    용기사,

    메이지,
    아크위저드,

    프리스트,
    비숍,

    궁수,
    샤프슈터,
    
    도적,
    나이트로드,
}

/// <summary>
/// NPC 데이터 클래스
/// </summary>
[Serializable]
public class NPCData
{
    public string npcId;
    public string name;
    public Job job;
    public bool isOnline;
    public bool isFriend;
    public string behaviorType;
    public string behaviorExample;
    public Dictionary<string, float> relationships; // 다른 NPC/플레이어와의 호감도
    public float speakProbability = 0.3f; // 말할 확률 (랜덤 주기에서 말할 확률)
    public float responseProbability = 0.4f; // 답장 확률
    public float relationshipBonus = 0.3f; // 호감도에 따른 확률 보너스 (호감도 100일 때 최대 보너스)
    
    // 전투 관련 (JSON에서 설정)
    public int attackPower = 5; // NPC 공격력
    /// <summary>장착한 스킬 ID 목록 (Skills.json의 skillId). 비어 있으면 기본 공격만 사용</summary>
    public List<string> equippedSkillIds;
    
    // 시각적 표현 (JSON에서 설정)
    public string spritePath; // Resources 폴더 기준 스프라이트 경로
    
    // 런타임에서만 사용 (JSON에서 로드)
    [NonSerialized] public Sprite npcSprite;

    /// <summary>접속 가능 시간대 (게임 시간 0~1440분). 비어 있으면 스케줄 없음.</summary>
    public List<OnlineWindow> onlineSchedule;

    public NPCData(string id, string npcName)
    {
        npcId = id;
        name = npcName;
        job = Job.무직;
        isOnline = false;
        isFriend = false;
        behaviorType = "";
        behaviorExample = "";
        relationships = new Dictionary<string, float>();
        onlineSchedule = null;
        equippedSkillIds = new List<string>();
    }
    
    /// <summary>
    /// 특정 대상과의 호감도를 가져옵니다 (없으면 0 = 초면)
    /// </summary>
    public float GetRelationship(string targetId)
    {
        // initialRelationships에 없으면 0 (초면)
        return relationships.ContainsKey(targetId) ? relationships[targetId] : 0f;
    }
    
    /// <summary>
    /// 호감도를 설정합니다
    /// </summary>
    public void SetRelationship(string targetId, float value)
    {
        relationships[targetId] = Mathf.Clamp(value, -100f, 100f);
    }
    
    /// <summary>
    /// 호감도를 증가시킵니다
    /// </summary>
    public void AddRelationship(string targetId, float amount)
    {
        if (!relationships.ContainsKey(targetId))
            relationships[targetId] = 0f;
        relationships[targetId] = Mathf.Clamp(relationships[targetId] + amount, -100f, 100f);
    }
    
    /// <summary>
    /// 답장 확률을 계산합니다 (답장 확률 + 호감도 보너스)
    /// 이름 언급은 별도로 처리되므로 여기서는 확률만 계산합니다.
    /// </summary>
    public float CalculateResponseProbability(string message, string senderId)
    {
        float probability = responseProbability;
        
        // 호감도 보너스 (호감도 100일 때 최대 보너스)
        float relationship = GetRelationship(senderId);
        float relationshipBonusValue = (relationship / 100f) * relationshipBonus;
        probability += relationshipBonusValue;
        
        return Mathf.Clamp01(probability);
    }
    
    /// <summary>
    /// 이름이 언급되었는지 확인 (정확한 매칭 → 유사도 검사 → 부분 매칭)
    /// 오타와 줄임말도 처리합니다.
    /// </summary>
    public bool IsNameMentioned(string message, string npcName)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(npcName))
            return false;
        
        // 대소문자 무시 비교
        string lowerMessage = message.ToLower();
        string lowerName = npcName.ToLower();
        
        // 1단계: 정확한 이름 매칭 (단어 경계 고려)
        if (IsExactNameMatch(lowerMessage, lowerName))
        {
            return true;
        }
        
        // 2단계: 유사도 검사 (오타 처리)
        if (IsSimilarNameMatch(lowerMessage, lowerName))
        {
            Debug.Log($"[NPCData] {npcName} 이름 유사도 매칭 감지 (오타 가능성)");
            return true;
        }
        
        // 3단계: 부분 문자열 매칭 (줄임말 처리)
        if (IsPartialNameMatch(lowerMessage, lowerName))
        {
            Debug.Log($"[NPCData] {npcName} 이름 부분 매칭 감지 (줄임말 가능성)");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 이름 언급 신뢰도 계산 (0~1, 높을수록 확실함)
    /// </summary>
    public float CalculateNameMentionConfidence(string message, string npcName)
    {
        if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(npcName))
            return 0f;
        
        string lowerMessage = message.ToLower();
        string lowerName = npcName.ToLower();
        
        // 1단계: 정확한 매칭 = 신뢰도 1.0
        if (IsExactNameMatch(lowerMessage, lowerName))
        {
            return 1.0f;
        }
        
        // 2단계: 유사도 검사 = 유사도 값 그대로
        string[] words = lowerMessage.Split(new char[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        float maxSimilarity = 0f;
        foreach (string word in words)
        {
            int distance = LevenshteinDistance(word, lowerName);
            int maxLength = Mathf.Max(word.Length, lowerName.Length);
            
            if (maxLength == 0)
                continue;
            
            float similarity = 1f - ((float)distance / maxLength);
            maxSimilarity = Mathf.Max(maxSimilarity, similarity);
        }
        
        if (maxSimilarity >= 0.6f)
        {
            return maxSimilarity; // 유사도가 높으면 그대로 반환
        }
        
        // 3단계: 부분 매칭 = 부분 비율에 비례
        if (lowerName.Length <= 2)
            return 0f;
        
        float maxPartialRatio = 0f;
        foreach (string word in words)
        {
            if (word.Length < 2)
                continue;
            
            float ratio = 0f;
            if (lowerName.StartsWith(word))
                ratio = (float)word.Length / lowerName.Length;
            else if (lowerName.EndsWith(word))
                ratio = (float)word.Length / lowerName.Length;
            else if (lowerName.Contains(word))
                ratio = (float)word.Length / lowerName.Length;
            
            maxPartialRatio = Mathf.Max(maxPartialRatio, ratio);
        }
        
        if (maxPartialRatio >= 0.4f)
        {
            return maxPartialRatio * 0.8f; // 부분 매칭은 신뢰도 낮게 (0.8 배율)
        }
        
        return 0f;
    }
    
    /// <summary>
    /// 정확한 이름 매칭 (단어 경계 고려)
    /// </summary>
    private bool IsExactNameMatch(string message, string name)
    {
        int index = message.IndexOf(name);
        if (index == -1)
            return false;
        
        // 단어 경계 확인 (앞뒤가 공백, 구두점, 또는 문자열의 시작/끝인지)
        bool isWordBoundaryBefore = index == 0 || 
            char.IsWhiteSpace(message[index - 1]) || 
            char.IsPunctuation(message[index - 1]);
        
        bool isWordBoundaryAfter = (index + name.Length) >= message.Length ||
            char.IsWhiteSpace(message[index + name.Length]) ||
            char.IsPunctuation(message[index + name.Length]);
        
        return isWordBoundaryBefore && isWordBoundaryAfter;
    }
    
    /// <summary>
    /// 유사도 기반 이름 매칭 (오타 처리)
    /// 편집 거리(Levenshtein Distance)를 사용하여 유사도 계산
    /// </summary>
    private bool IsSimilarNameMatch(string message, string name)
    {
        // 메시지를 단어 단위로 분리하여 각 단어와 비교
        string[] words = message.Split(new char[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string word in words)
        {
            // 편집 거리 계산
            int distance = LevenshteinDistance(word, name);
            int maxLength = Mathf.Max(word.Length, name.Length);
            
            if (maxLength == 0)
                continue;
            
            // 유사도 = 1 - (편집 거리 / 최대 길이)
            float similarity = 1f - ((float)distance / maxLength);
            
            // 유사도가 0.6 이상이면 매칭 (40% 이하의 오차 허용)
            if (similarity >= 0.6f)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 부분 문자열 매칭 (줄임말 처리)
    /// 이름의 일부가 포함되어 있고 충분히 긴 경우
    /// </summary>
    private bool IsPartialNameMatch(string message, string name)
    {
        // 이름이 너무 짧으면 부분 매칭 비활성화 (오탐지 방지)
        if (name.Length <= 2)
            return false;
        
        // 메시지를 단어 단위로 분리
        string[] words = message.Split(new char[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ':', ';', '(', ')', '[', ']', '{', '}' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string word in words)
        {
            // 단어가 너무 짧으면 무시 (오탐지 방지)
            if (word.Length < 2)
                continue;
            
            // 이름의 시작 부분과 일치하는지 확인 (앞에서부터)
            if (name.StartsWith(word) && word.Length >= name.Length * 0.4f) // 이름의 40% 이상
            {
                return true;
            }
            
            // 이름의 끝 부분과 일치하는지 확인 (뒤에서부터)
            if (name.EndsWith(word) && word.Length >= name.Length * 0.4f) // 이름의 40% 이상
            {
                return true;
            }
            
            // 이름의 중간 부분이 포함되어 있는지 확인 (연속된 부분 문자열)
            if (name.Contains(word) && word.Length >= name.Length * 0.4f) // 이름의 40% 이상
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 편집 거리(Levenshtein Distance) 계산
    /// 두 문자열을 같게 만들기 위해 필요한 최소 편집 횟수
    /// </summary>
    private int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        
        if (string.IsNullOrEmpty(t))
            return s.Length;
        
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        
        // 초기화
        for (int i = 0; i <= n; i++)
            d[i, 0] = i;
        
        for (int j = 0; j <= m; j++)
            d[0, j] = j;
        
        // 동적 프로그래밍으로 편집 거리 계산
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                
                d[i, j] = Mathf.Min(
                    d[i - 1, j] + 1,      // 삭제
                    d[i, j - 1] + 1,      // 삽입
                    d[i - 1, j - 1] + cost // 치환
                );
            }
        }
        
        return d[n, m];
    }
    
    /// <summary>
    /// 랜덤 주기에서 이 NPC가 말할 확률 가중치 (대상과 무관한 기본 확률)
    /// </summary>
    public float GetSpeakWeight()
    {
        return speakProbability;
    }
    
    /// <summary>
    /// 특정 대상에 대한 말할 확률 가중치 (말할 확률 + 호감도 보너스)
    /// </summary>
    public float GetSpeakWeightForTarget(string targetId)
    {
        float weight = speakProbability;
        
        // 호감도 보너스 적용
        float relationship = GetRelationship(targetId);
        float relationshipBonusValue = (relationship / 100f) * relationshipBonus;
        weight += relationshipBonusValue;
        
        return Mathf.Clamp01(weight);
    }
    
    /// <summary>
    /// 스프라이트 로드 (spritePath를 사용하여 Resources에서 로드)
    /// </summary>
    public void LoadSprite()
    {
        if (string.IsNullOrEmpty(spritePath))
        {
            npcSprite = null;
            return;
        }
        
        // Sprite로 직접 로드 시도
        npcSprite = Resources.Load<Sprite>(spritePath);
        
        // Sprite로 로드 실패 시 Texture2D로 로드 후 Sprite로 변환 시도
        if (npcSprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(spritePath);
            if (texture != null)
            {
                // Texture2D를 Sprite로 변환
                npcSprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f // pixelsPerUnit
                );
                Debug.Log($"[NPCData] Texture2D를 Sprite로 변환했습니다: {spritePath}");
            }
            else
            {
                Debug.LogWarning($"[NPCData] 스프라이트를 찾을 수 없습니다: {spritePath} (Resources 폴더 기준 경로를 확인하세요)");
            }
        }
    }
}

/// <summary>
/// 게임 시간 기준 접속 가능 구간 (자정 기준 분)
/// </summary>
[Serializable]
public class OnlineWindow
{
    public int startMinute; // 시작 시간 (분, 0~1440)
    public int durationMinutes; // 머무는 시간 (분)
    /// <summary>접속 시간에 적용할 랜덤 오프셋(분). 같은 날에는 고정.</summary>
    public int startOffsetMinutes = 15;
    /// <summary>나가는 시간에 적용할 랜덤 오프셋(분). 같은 날에는 고정.</summary>
    public int endOffsetMinutes = 15;
}
