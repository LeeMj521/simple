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

    [Header("접속 시 User 생성")]
    [Tooltip("Resources 경로 또는 인스펙터에서 할당된 프리팹")]
    [SerializeField] private GameObject userPrefab;
    [SerializeField] private Transform userSpawnParent;
    [Tooltip("오목 형태 중심. ■☆■ / ■■■ 에서 ☆ = 몬스터 구역(위쪽 가운데만 빈칸)")]
    [SerializeField] private Vector2 spawnAreaCenter = Vector2.zero;
    [Tooltip("전체 너비")]
    [SerializeField] private float spawnAreaTotalWidth = 12f;
    [Tooltip("전체 높이")]
    [SerializeField] private float spawnAreaTotalHeight = 8f;
    [Tooltip("위쪽 가운데 빈 구역(☆) 너비")]
    [SerializeField] private float spawnAreaGapWidth = 4f;
    [Tooltip("위쪽 가운데 빈 구역(☆) 높이")]
    [SerializeField] private float spawnAreaGapHeight = 3f;
    [Tooltip("다른 User와 이 거리 이상 떨어지게 배치")]
    [SerializeField] private float minDistanceBetweenUsers = 1.5f;
    [Tooltip("겹치지 않는 위치 찾기 최대 시도 횟수")]
    [SerializeField] private int spawnPositionAttempts = 25;

    private Dictionary<string, NPCData> _npcs = new Dictionary<string, NPCData>();
    private Dictionary<string, UserObject> _spawnedUsers = new Dictionary<string, UserObject>();
    private int _cachedGameDay = -1;
    private bool _prefabWarningLogged;
    private Dictionary<string, List<(int start, int end)>> _effectiveWindows = new Dictionary<string, List<(int, int)>>();

    private void Awake()
    {
        if (dataManager == null)
            dataManager = FindFirstObjectByType<DataManager>();
        if (gameTime == null)
            gameTime = FindFirstObjectByType<GameTimeManager>();
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
                if (s <= e && now >= s && now < e) { online = true; break; }
                if (s > e && (now >= s || now < e)) { online = true; break; }
            }
            npc.isOnline = online;

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
        if (prefab == null)
            prefab = Resources.Load<GameObject>("Prefabs/User");
        if (prefab == null)
        {
            if (!_prefabWarningLogged)
            {
                _prefabWarningLogged = true;
                Debug.LogWarning("[NPCManager] User 프리팹이 없습니다. NPCManager 인스펙터에 User Prefab을 할당하거나 Resources/Prefabs/User 에 프리팹을 두세요.");
            }
            return;
        }

        Transform parent = userSpawnParent != null ? userSpawnParent : transform;
        Vector3 localPos = GetRandomSpawnPosition(parent);
        GameObject go = Instantiate(prefab, parent);
        go.transform.localPosition = localPos;

        UserObject user = go.GetComponent<UserObject>();
        if (user == null) user = go.AddComponent<UserObject>();
        user.SetDisplayName(npc.name);
        user.SetAttack(npc.attackPower, npc.attackCooldown);

        _spawnedUsers[npcId] = user;
    }

    /// <summary>
    /// 오목 형태 ■☆■/■■■: 좌열·우열·아래막대 중 한 구역에서 랜덤. 위쪽 가운데(☆)는 몬스터 구역.
    /// </summary>
    private Vector3 GetRandomSpawnPosition(Transform parent)
    {
        float minDist = Mathf.Max(0.1f, minDistanceBetweenUsers);
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

        for (int attempt = 0; attempt < spawnPositionAttempts; attempt++)
        {
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
            Vector2 local2 = new Vector2(x, y);

            bool ok = true;
            foreach (var u in _spawnedUsers.Values)
            {
                if (u == null || u.transform == null) continue;
                Vector2 other = new Vector2(u.transform.localPosition.x, u.transform.localPosition.y);
                if (Vector2.Distance(local2, other) < minDist) { ok = false; break; }
            }
            if (ok) return new Vector3(local2.x, local2.y, 0f);
        }
        return new Vector3(spawnAreaCenter.x - halfGapW - 0.5f, spawnAreaCenter.y, 0f);
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

        int offset = Mathf.Min(120, Mathf.Max(0, npc.randomOffsetMinutes));
        var result = new List<(int, int)>();

        for (int i = 0; i < npc.onlineSchedule.Count; i++)
        {
            int s = npc.onlineSchedule[i].startMinute;
            int e = npc.onlineSchedule[i].endMinute;
            int deltaS = offset > 0 ? Random.Range(-offset, offset + 1) : 0;
            int deltaE = offset > 0 ? Random.Range(-offset, offset + 1) : 0;
            s = Mathf.Clamp(s + deltaS, 0, 1440);
            e = Mathf.Clamp(e + deltaE, 0, 1440);
            if (s != e)
                result.Add((s, e));
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
