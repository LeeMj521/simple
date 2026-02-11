using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 접속 상태 관리. 게임 시간대 + 소량 랜덤으로 isOnline 갱신.
/// 접속 시 User 프리팹 생성해 함께 몬스터 공격, 오프라인 시 제거.
/// </summary>
public class NPCManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DataManager dataManager;
    [SerializeField] private GameTimeManager gameTime;
    [SerializeField] private ChatManager chatManager;

    [Header("접속 시 User 생성")]
    [Tooltip("Resources 경로 또는 인스펙터에서 할당된 프리팹")]
    [SerializeField] private GameObject userPrefab;
    [SerializeField] private Transform userSpawnParent;
    [Tooltip("중심")]
    [SerializeField] private Vector2 spawnAreaCenter = Vector2.zero;
    [Tooltip("전체 너비")]
    [SerializeField] private float spawnAreaTotalWidth = 12f;
    [Tooltip("전체 높이")]
    [SerializeField] private float spawnAreaTotalHeight = 8f;
    [Tooltip("위쪽 가운데 빈 구역 너비")]
    [SerializeField] private float spawnAreaGapWidth = 4f;
    [Tooltip("위쪽 가운데 빈 구역 높이")]
    [SerializeField] private float spawnAreaGapHeight = 3f;
    [Tooltip("User 원 강체 반지름. 스폰 전 겹침 검사에 사용")]
    [SerializeField] private float userCircleRadius = 1.2f;
    [Tooltip("겹치지 않는 위치 찾기 최대 시도 횟수")]
    [SerializeField] private int spawnPositionAttempts = 25;

    private Dictionary<string, NPCData> _npcs = new Dictionary<string, NPCData>();
    private Dictionary<string, UserObject> _spawnedUsers = new Dictionary<string, UserObject>();
    private int _cachedGameDay = -1;
    private bool _prefabWarningLogged;
    private Dictionary<string, List<(int start, int end)>> _effectiveWindows = new Dictionary<string, List<(int, int)>>();
    private Dictionary<string, bool> _previousOnlineState = new Dictionary<string, bool>(); // 이전 접속 상태 추적

    private void Awake()
    {
        if (dataManager == null)
            dataManager = FindFirstObjectByType<DataManager>();
        if (gameTime == null)
            gameTime = FindFirstObjectByType<GameTimeManager>();
        if (chatManager == null)
            chatManager = FindFirstObjectByType<ChatManager>();
        if (userSpawnParent == null)
            userSpawnParent = transform;
    }

    private void Start()
    {
        RefreshNpcList();
    }

    private void Update()
    {
        if (gameTime == null) return;

        int day = gameTime.GameDay;
        if (day != _cachedGameDay)
        {
            _cachedGameDay = day;
            _effectiveWindows.Clear();
        }

        int now = gameTime.TimeOfDayMinutes;

        foreach (var kv in _npcs)
        {
            string npcId = kv.Key;
            NPCData npc = kv.Value;
            if (npc.onlineSchedule == null || npc.onlineSchedule.Count == 0)
            {
                npc.isOnline = false;
                TryRemoveUser(npcId);
                continue;
            }

            var windows = GetEffectiveWindows(npc);
            bool online = false;
            for (int i = 0; i < windows.Count; i++)
            {
                int s = windows[i].start, e = windows[i].end;
                
                // 무한 접속 (머무는 시간이 24시간 이상)
                if (e == int.MaxValue)
                {
                    // 시작 시간 이후면 계속 접속 상태
                    if (now >= s) { online = true; break; }
                    continue;
                }
                
                // 같은 날 범위 (예: 9시-17시)
                if (e <= 1440 && now >= s && now < e) { online = true; break; }
                
                // 자정을 넘어가는 범위 (예: 18시-다음날 8시)
                // e가 1440을 넘으면 다음날까지인 경우
                if (e > 1440)
                {
                    // now >= s (시작 시간 이후) 또는 now < (e - 1440) (다음날 종료 시간 이전)
                    if (now >= s || now < (e - 1440)) { online = true; break; }
                }
            }
            // 접속 상태 변경 감지 및 시스템 메시지 전송
            bool wasOnline = _previousOnlineState.TryGetValue(npcId, out bool prev) && prev;
            if (online && !wasOnline)
            {
                // 접속 시
                if (chatManager != null)
                    chatManager.AddSystemMessage($"{npc.name}님이 접속했습니다.");
            }
            else if (!online && wasOnline)
            {
                // 접속 해제 시 (선택사항)
                // if (chatManager != null)
                //     chatManager.AddSystemMessage($"{npc.name}님이 접속을 종료했습니다.");
            }

            npc.isOnline = online;
            _previousOnlineState[npcId] = online;

            if (online)
                TrySpawnUser(npcId, npc);
            else
                TryRemoveUser(npcId);
        }
    }

    private void TrySpawnUser(string npcId, NPCData npc)
    {
        if (_spawnedUsers.ContainsKey(npcId)) return;

        GameObject prefab = userPrefab;

        Transform parent = userSpawnParent != null ? userSpawnParent : transform;
        if (!TryGetFreeSpawnPosition(parent, out Vector3 localPos))
            return;

        GameObject go = Instantiate(prefab, parent);
        go.transform.localPosition = localPos;

        UserObject user = go.GetComponent<UserObject>();
        if (user == null) user = go.AddComponent<UserObject>();
        user.Set(npc);

        _spawnedUsers[npcId] = user;
    }

    /// <summary>
    /// 스폰 전 Physics2D.OverlapCircle(반지름 userCircleRadius)로 겹침 검사.
    /// </summary>
    private bool TryGetFreeSpawnPosition(Transform parent, out Vector3 localPos)
    {
        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
            Vector2 candidate = GetRandomPointInArea();
            Vector2 candidateWorld = parent.TransformPoint(candidate.x, candidate.y, 0f);

            Collider2D[] hits = Physics2D.OverlapCircleAll(candidateWorld, userCircleRadius);
            bool overlapsUser = false;
            foreach (var col in hits)
            {
                if (col == null) continue;
                var u = col.GetComponent<UserObject>();
                if (u != null)
                {
                    overlapsUser = true;
                    break;
                }
            }
            if (!overlapsUser)
            {
                localPos = new Vector3(candidate.x, candidate.y, 0f);
                return true;
            }
        }
        localPos = default;
        return false;
    }

    /// <summary>
    /// 오목 형태 ■☆■/■■■: 좌열·우열·아래막대 중 한 구역에서 랜덤. 위쪽 가운데(☆)는 몬스터 구역.
    /// </summary>
    private Vector2 GetRandomPointInArea()
    {
        float halfW = spawnAreaTotalWidth * 0.5f;
        float halfH = spawnAreaTotalHeight * 0.5f;
        float halfGapW = Mathf.Clamp(spawnAreaGapWidth * 0.5f, 0f, halfW - 0.1f);
        float gapH = Mathf.Clamp(spawnAreaGapHeight, 0f, spawnAreaTotalHeight - 0.1f);

        float leftMin = spawnAreaCenter.x - halfW;
        float leftMax = spawnAreaCenter.x - halfGapW;
        float rightMin = spawnAreaCenter.x + halfGapW;
        float rightMax = spawnAreaCenter.x + halfW;
        float yBottom = spawnAreaCenter.y - halfH;
        float yTop = spawnAreaCenter.y + halfH;
        float yGapBottom = yTop - gapH;

        float x, y;
        float zone = Random.value;
        if (zone < 1f / 3f)
        {
            x = Random.Range(leftMin, leftMax);
            y = Random.Range(yBottom, yTop);
        }
        else if (zone < 2f / 3f)
        {
            x = Random.Range(rightMin, rightMax);
            y = Random.Range(yBottom, yTop);
        }
        else
        {
            x = Random.Range(spawnAreaCenter.x - halfGapW, spawnAreaCenter.x + halfGapW);
            y = Random.Range(yBottom, yGapBottom);
        }
        return new Vector2(x, y);
    }

    private void TryRemoveUser(string npcId)
    {
        if (!_spawnedUsers.TryGetValue(npcId, out UserObject user)) return;
        _spawnedUsers.Remove(npcId);
        if (user != null && user.gameObject != null)
            Destroy(user.gameObject);
    }

    /// <summary>
    /// 해당 게임일 기준 랜덤 오프셋이 적용된 시간대 목록 (캐시)
    /// </summary>
    private List<(int start, int end)> GetEffectiveWindows(NPCData npc)
    {
        string id = npc.npcId;
        if (_effectiveWindows.TryGetValue(id, out var list))
            return list;

        var result = new List<(int, int)>();

        for (int i = 0; i < npc.onlineSchedule.Count; i++)
        {
            var window = npc.onlineSchedule[i];
            int baseStart = window.startMinute;
            int duration = window.durationMinutes;
            
            // 접속 시간 랜덤 오프셋 적용
            int startOffset = Mathf.Min(120, Mathf.Max(0, window.startOffsetMinutes));
            int deltaS = startOffset > 0 ? Random.Range(-startOffset, startOffset + 1) : 0;
            int s = Mathf.Clamp(baseStart + deltaS, 0, 1440);
            
            // 머무는 시간이 정확히 24시간(1440분)이면 무한 접속 (랜덤 오프셋 무시)
            int e;
            if (duration == 1440)
            {
                // 무한 접속: 매우 큰 값으로 설정
                e = int.MaxValue;
                result.Add((s, e));
            }
            else
            {
                // 나가는 시간 랜덤 오프셋 적용
                int endOffset = Mathf.Min(120, Mathf.Max(0, window.endOffsetMinutes));
                int deltaE = endOffset > 0 ? Random.Range(-endOffset, endOffset + 1) : 0;
                int actualDuration = Mathf.Max(0, duration + deltaE);
                
                // 종료 시간 계산 (자정을 넘어갈 수 있음)
                e = s + actualDuration;
                
                if (actualDuration > 0)
                    result.Add((s, e));
            }
        }

        _effectiveWindows[id] = result;
        return result;
    }

    /// <summary>
    /// DataManager에서 NPC 목록을 다시 가져옵니다. 생성된 User 오브젝트는 모두 제거됩니다.
    /// </summary>
    public void RefreshNpcList()
    {
        foreach (var u in _spawnedUsers.Values)
        {
            if (u != null && u.gameObject != null)
                Destroy(u.gameObject);
        }
        _spawnedUsers.Clear();
        _npcs.Clear();
        _effectiveWindows.Clear();
        _previousOnlineState.Clear();
        _cachedGameDay = -1;
        if (dataManager != null)
        {
            foreach (var kv in dataManager.GetLoadedNPCs())
                _npcs[kv.Key] = kv.Value;
        }
    }

    /// <summary>
    /// 특정 NPC 접속 상태 설정 (수동 오버라이드 시 사용)
    /// </summary>
    public void SetOnline(string npcId, bool online)
    {
        if (_npcs.TryGetValue(npcId, out NPCData npc))
            npc.isOnline = online;
    }

    /// <summary>
    /// 특정 NPC 접속 여부 조회
    /// </summary>
    public bool IsOnline(string npcId)
    {
        return _npcs.TryGetValue(npcId, out NPCData npc) && npc.isOnline;
    }

    /// <summary>
    /// 모든 NPC를 오프라인으로 설정하고 생성된 User 오브젝트를 제거합니다.
    /// </summary>
    public void SetAllOffline()
    {
        foreach (var npc in _npcs.Values)
            npc.isOnline = false;
        foreach (var u in _spawnedUsers.Values)
        {
            if (u != null && u.gameObject != null)
                Destroy(u.gameObject);
        }
        _spawnedUsers.Clear();
    }

    /// <summary>
    /// NPC 데이터 가져오기 (없으면 null)
    /// </summary>
    public NPCData GetNPC(string npcId)
    {
        return _npcs.TryGetValue(npcId, out NPCData npc) ? npc : null;
    }

    /// <summary>
    /// 접속 중인 NPC 수
    /// </summary>
    public int OnlineCount
    {
        get
        {
            int n = 0;
            foreach (var npc in _npcs.Values)
                if (npc.isOnline) n++;
            return n;
        }
    }

    /// <summary>
    /// 로드된 모든 NPC 데이터 가져오기
    /// </summary>
    public Dictionary<string, NPCData> GetLoadedNPCs()
    {
        return new Dictionary<string, NPCData>(_npcs);
    }

    /// <summary>
    /// 해당 NPC(유저) 머리 위에 채팅 버블 표시. 일정 시간 후 자동 숨김.
    /// </summary>
    public void ShowChatBubbleForUser(string npcId, string text, float durationSeconds)
    {
        if (string.IsNullOrEmpty(npcId) || durationSeconds <= 0f)
            return;
        if (!_spawnedUsers.TryGetValue(npcId, out UserObject user) || user == null)
            return;
        user.ShowChatBubble(text, durationSeconds);
    }

    private void OnDrawGizmos()
    {
        Transform parent = userSpawnParent != null ? userSpawnParent : transform;
        Gizmos.matrix = parent.localToWorldMatrix;
        float halfW = spawnAreaTotalWidth * 0.5f;
        float halfH = spawnAreaTotalHeight * 0.5f;
        float halfGapW = Mathf.Clamp(spawnAreaGapWidth * 0.5f, 0f, halfW - 0.01f);
        float gapH = Mathf.Clamp(spawnAreaGapHeight, 0f, spawnAreaTotalHeight - 0.01f);
        Vector3 center3 = new Vector3(spawnAreaCenter.x, spawnAreaCenter.y, 0f);
        float wingW = halfW - halfGapW;
        float yTop = spawnAreaCenter.y + halfH;
        float yGapBottom = yTop - gapH;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.7f);
        Vector3 leftCenter = center3 + new Vector3(-(halfGapW + wingW * 0.5f), 0f, 0f);
        Gizmos.DrawWireCube(leftCenter, new Vector3(wingW, spawnAreaTotalHeight, 0.01f));
        Vector3 rightCenter = center3 + new Vector3(halfGapW + wingW * 0.5f, 0f, 0f);
        Gizmos.DrawWireCube(rightCenter, new Vector3(wingW, spawnAreaTotalHeight, 0.01f));
        Vector3 bottomCenter = new Vector3(spawnAreaCenter.x, spawnAreaCenter.y - gapH * 0.5f, 0f);
        Gizmos.DrawWireCube(bottomCenter, new Vector3(spawnAreaGapWidth, spawnAreaTotalHeight - gapH, 0.01f));

        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        Vector3 gapCenter = new Vector3(spawnAreaCenter.x, (yTop + yGapBottom) * 0.5f, 0f);
        Gizmos.DrawWireCube(gapCenter, new Vector3(spawnAreaGapWidth, gapH, 0.01f));
    }
}
