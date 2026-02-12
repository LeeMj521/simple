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
    [SerializeField] private GameManager gameManager;

    [Header("접속 시 User 생성")]
    [Tooltip("Resources 경로 또는 인스펙터에서 할당된 프리팹")]
    [SerializeField] private GameObject userPrefab;
    [SerializeField] private Transform userSpawnParent;
    [Tooltip("고정 스폰 위치 Transform 배열. 이 중에서 랜덤으로 선택하여 생성")]
    [SerializeField] private Transform[] spawnPoints = new Transform[0];

    private Dictionary<string, NPCData> _npcs = new Dictionary<string, NPCData>();
    private Dictionary<string, UserObject> _spawnedUsers = new Dictionary<string, UserObject>();
    private Dictionary<string, Transform> _userSpawnPointMap = new Dictionary<string, Transform>(); // NPC ID -> 사용 중인 스폰 위치
    private HashSet<Transform> _occupiedSpawnPoints = new HashSet<Transform>(); // 사용 중인 스폰 위치 추적
    private List<Transform> _availablePointsCache = new List<Transform>(); // 재사용 가능한 리스트 (GC 최적화)
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
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (userSpawnParent == null)
            userSpawnParent = transform;
    }

    private void Start()
    {
        RefreshNpcList();
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null || spawnPoints == null) return;
        Transform nearest = null;
        float minDist = float.MaxValue;
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            float dist = Vector3.Distance(player.transform.position, point.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = point;
            }
        }
        if (nearest != null)
            _occupiedSpawnPoints.Add(nearest);
    }

    private void Update()
    {
        // 클릭 처리
        if (Input.GetMouseButtonDown(1))
        {
            HandleSpawnPointClick();
        }
        
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
        if (!TryGetFreeSpawnPosition(parent, out Vector3 localPos, out Transform spawnPoint))
            return;

        GameObject go = Instantiate(prefab, parent);
        go.transform.localPosition = localPos;

        UserObject user = go.GetComponent<UserObject>();
        if (user == null) user = go.AddComponent<UserObject>();
        user.Set(npc);

        _spawnedUsers[npcId] = user;
        if (spawnPoint != null)
        {
            _userSpawnPointMap[npcId] = spawnPoint;
            _occupiedSpawnPoints.Add(spawnPoint);
        }
    }

    /// <summary>
    /// 고정 스폰 위치 중 사용 가능한 위치를 랜덤으로 선택. 없으면 false 반환.
    /// </summary>
    private bool TryGetFreeSpawnPosition(Transform parent, out Vector3 localPos, out Transform spawnPoint)
    {
        spawnPoint = null;
        localPos = default;

        // 고정 스폰 위치가 설정되어 있지 않으면 생성 불가
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return false;
        }

        // 사용 가능한 위치 목록 생성 (재사용 리스트 사용)
        _availablePointsCache.Clear();
        foreach (var point in spawnPoints)
        {
            if (point != null && !_occupiedSpawnPoints.Contains(point))
            {
                _availablePointsCache.Add(point);
            }
        }

        // 사용 가능한 위치가 없으면 생성 불가
        if (_availablePointsCache.Count == 0)
        {
            return false;
        }

        // 랜덤으로 선택
        int randomIndex = Random.Range(0, _availablePointsCache.Count);
        spawnPoint = _availablePointsCache[randomIndex];
        localPos = parent.InverseTransformPoint(spawnPoint.position);
        return true;
    }


    private void TryRemoveUser(string npcId)
    {
        if (!_spawnedUsers.TryGetValue(npcId, out UserObject user)) return;
        _spawnedUsers.Remove(npcId);
        
        // 사용 중이던 스폰 위치 해제
        if (_userSpawnPointMap.TryGetValue(npcId, out Transform spawnPoint))
        {
            _userSpawnPointMap.Remove(npcId);
            if (spawnPoint != null)
                _occupiedSpawnPoints.Remove(spawnPoint);
        }
        
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
        _userSpawnPointMap.Clear();
        _occupiedSpawnPoints.Clear();
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
        _userSpawnPointMap.Clear();
        _occupiedSpawnPoints.Clear();
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

    /// <summary>
    /// 왼클릭으로 스폰 위치 클릭 처리 (GameManager의 선택된 유저 이동)
    /// </summary>
    private void HandleSpawnPointClick()
    {
        if (gameManager == null || gameManager.selectedUser == null || spawnPoints == null)
            return;
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        
        // 클릭된 콜라이더 확인
        Collider2D hit = Physics2D.OverlapPoint(mousePos);
        if (hit == null) return;
        
        Transform clickedTransform = hit.transform;
        
        // 클릭된 Transform이 스폰 위치 중 하나인지 확인
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            if (point == clickedTransform)
            {
                // 스폰 위치 클릭됨
                UserObject selectedUser = gameManager.selectedUser;
                if (_occupiedSpawnPoints.Contains(point))
                {
                    // 점유된 위치 -> 스왑
                    SwapUserPosition(selectedUser, point);
                }
                else
                {
                    // 빈 위치 -> 이동
                    MoveUserToPosition(selectedUser, point);
                }
                return;
            }
        }
    }
    
    /// <summary>
    /// UserObject를 빈 위치로 이동
    /// </summary>
    private void MoveUserToPosition(UserObject user, Transform targetPoint)
    {
        if (user == null || targetPoint == null) return;
        
        // 기존 위치 해제
        string npcId = FindNpcIdByUser(user);
        if (npcId != null && _userSpawnPointMap.TryGetValue(npcId, out Transform oldPoint))
        {
            _occupiedSpawnPoints.Remove(oldPoint);
            _userSpawnPointMap.Remove(npcId);
        }
        
        // 새 위치 등록
        _userSpawnPointMap[npcId] = targetPoint;
        _occupiedSpawnPoints.Add(targetPoint);
        
        // 이동 애니메이션
        Transform parent = userSpawnParent != null ? userSpawnParent : transform;
        Vector3 targetLocalPos = parent.InverseTransformPoint(targetPoint.position);
        user.MoveToPosition(targetLocalPos);
    }
    
    /// <summary>
    /// 두 UserObject의 위치를 스왑
    /// </summary>
    private void SwapUserPosition(UserObject user1, Transform point1)
    {
        if (user1 == null || point1 == null) return;
        
        // point1에 있는 UserObject 찾기
        UserObject user2 = null;
        string npcId2 = null;
        foreach (var kv in _userSpawnPointMap)
        {
            if (kv.Value == point1)
            {
                npcId2 = kv.Key;
                _spawnedUsers.TryGetValue(npcId2, out user2);
                break;
            }
        }
        
        if (user2 == null) return;
        
        // user1의 현재 위치 찾기
        string npcId1 = FindNpcIdByUser(user1);
        if (npcId1 == null || !_userSpawnPointMap.TryGetValue(npcId1, out Transform point2))
            return;
        
        // 위치 스왑
        _userSpawnPointMap[npcId1] = point1;
        _userSpawnPointMap[npcId2] = point2;
        
        // 이동 애니메이션
        Transform parent = userSpawnParent != null ? userSpawnParent : transform;
        Vector3 targetLocalPos1 = parent.InverseTransformPoint(point1.position);
        Vector3 targetLocalPos2 = parent.InverseTransformPoint(point2.position);
        user1.MoveToPosition(targetLocalPos1);
        user2.MoveToPosition(targetLocalPos2);
    }
    
    /// <summary>
    /// UserObject로 NPC ID 찾기
    /// </summary>
    private string FindNpcIdByUser(UserObject user)
    {
        foreach (var kv in _spawnedUsers)
        {
            if (kv.Value == user)
                return kv.Key;
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;
        
        // 고정 스폰 위치 표시
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            
            bool isOccupied = _occupiedSpawnPoints.Contains(point);
            
            // 사용 중인 위치는 빨간색, 사용 가능한 위치는 초록색
            Gizmos.color = isOccupied 
                ? new Color(1f, 0.3f, 0.3f, 0.7f) 
                : new Color(0f, 1f, 0.5f, 0.7f);
            
            Gizmos.DrawWireSphere(point.position, 0.3f);
        }
    }
}
