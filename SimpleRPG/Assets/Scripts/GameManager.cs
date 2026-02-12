using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("플레이어")]
    public GameObject player;

    [Header("유저")]
    public UserObject selectedUser;
    public Dictionary<string, UserObject> users = new Dictionary<string, UserObject>();

    [Header("스폰 포인트")]
    [Tooltip("고정 스폰 위치 Transform 배열")]
    [SerializeField] private Transform[] spawnPoints = new Transform[0];

    private readonly Dictionary<string, Transform> _userSpawnPointMap = new Dictionary<string, Transform>(); // userId -> 스폰 위치
    private readonly HashSet<Transform> _occupiedSpawnPoints = new HashSet<Transform>(); // 사용 중인 스폰 위치
    private readonly List<Transform> _availablePointsCache = new List<Transform>(); // 재사용 가능한 리스트 (GC 최적화)
    private readonly List<string> _nullUserIdsCache = new List<string>(); // users 정리용

    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        RegisterPlayerIfPossible();
        // NPC 스폰보다 먼저 플레이어 위치를 점유시켜 충돌을 줄임
        ReserveNearestSpawnPointForPlayer();
    }

    private void Update()
    {
        PruneNullUsers();

        // 좌클릭: 유저 선택
        if (Input.GetMouseButtonDown(0))
            HandleUserSelectClick();

        // 우클릭: 스폰포인트 클릭 이동/스왑
        if (Input.GetMouseButtonDown(1))
            HandleSpawnPointClick();
    }

    private void PruneNullUsers()
    {
        if (users == null || users.Count == 0)
            return;

        _nullUserIdsCache.Clear();
        foreach (var kv in users)
        {
            if (kv.Value == null)
                _nullUserIdsCache.Add(kv.Key);
        }

        for (int i = 0; i < _nullUserIdsCache.Count; i++)
            UnregisterUser(_nullUserIdsCache[i]);

        _nullUserIdsCache.Clear();
    }

    private void HandleUserSelectClick()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null)
            return;

        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);
        if (hits == null || hits.Length == 0)
            return;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            UserObject user = hits[i].GetComponent<UserObject>();
            if (user != null)
            {
                selectedUser = user;
                RegisterUser(user); // 선택된 유저가 users에 없다면 등록
                break;
            }
        }
    }

    private void HandleSpawnPointClick()
    {
        if (selectedUser == null)
            return;

        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null)
            return;

        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);
        if (hits == null || hits.Length == 0)
            return;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            if (hits[i].transform != null && hits[i].transform.CompareTag("SpawnPoint"))
            {
                Transform clickedPoint = hits[i].transform;
                if (_occupiedSpawnPoints.Contains(clickedPoint))
                {
                    // 점유된 위치 -> 스왑
                    SwapUserPosition(selectedUser, clickedPoint);
                }
                else
                {
                    // 빈 위치 -> 이동
                    MoveUserToPoint(selectedUser, clickedPoint);
                }
                return;
            }
        }
    }

    public void RegisterUser(UserObject user)
    {
        if (user == null)
            return;

        string id = user.UserId;
        if (string.IsNullOrWhiteSpace(id))
        {
            bool isPlayer = user.gameObject != null && user.gameObject.CompareTag("Player");
            string fallbackId = isPlayer ? "player" : $"user_{user.GetInstanceID()}";
            string fallbackName = !string.IsNullOrWhiteSpace(user.UserName)
                ? user.UserName
                : (user.gameObject != null ? user.gameObject.name : "유저");
            user.SetIdentity(fallbackId, fallbackName);
            id = user.UserId;
        }

        users[id] = user;
    }

    public void UnregisterUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        users.Remove(userId);
        ReleaseSpawnPoint(userId);
        if (selectedUser != null && selectedUser.UserId == userId)
            selectedUser = null;
    }

    public bool TryGetUser(string userId, out UserObject user)
    {
        user = null;
        if (string.IsNullOrWhiteSpace(userId))
            return false;
        return users.TryGetValue(userId, out user) && user != null;
    }

    /// <summary>
    /// 고정 스폰 위치 중 사용 가능한 위치를 랜덤으로 선택하고 즉시 점유 처리.
    /// </summary>
    public bool TryReserveFreeSpawnPoint(string userId, Transform parent, out Vector3 localPos, out Transform spawnPoint)
    {
        spawnPoint = null;
        localPos = default;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return false;

        // 기존 점유 해제(재배치/재스폰 대비)
        ReleaseSpawnPoint(userId);

        _availablePointsCache.Clear();
        foreach (var point in spawnPoints)
        {
            if (point != null && !_occupiedSpawnPoints.Contains(point))
                _availablePointsCache.Add(point);
        }

        if (_availablePointsCache.Count == 0)
            return false;

        int randomIndex = Random.Range(0, _availablePointsCache.Count);
        spawnPoint = _availablePointsCache[randomIndex];

        _userSpawnPointMap[userId] = spawnPoint;
        _occupiedSpawnPoints.Add(spawnPoint);

        localPos = parent != null ? parent.InverseTransformPoint(spawnPoint.position) : spawnPoint.position;
        return true;
    }

    public void ReleaseSpawnPoint(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        if (_userSpawnPointMap.TryGetValue(userId, out Transform point))
        {
            _userSpawnPointMap.Remove(userId);
            if (point != null)
                _occupiedSpawnPoints.Remove(point);
        }
    }

    private void MoveUserToPoint(UserObject user, Transform targetPoint)
    {
        if (user == null || targetPoint == null)
            return;

        RegisterUser(user);
        string userId = user.UserId;
        if (string.IsNullOrWhiteSpace(userId))
            return;

        // 기존 위치 해제
        ReleaseSpawnPoint(userId);

        // 새 위치 등록
        _userSpawnPointMap[userId] = targetPoint;
        _occupiedSpawnPoints.Add(targetPoint);

        // 이동 애니메이션 (월드 기준으로 안전하게)
        user.MoveToWorldPosition(targetPoint.position);
    }

    private void SwapUserPosition(UserObject user1, Transform point1)
    {
        if (user1 == null || point1 == null)
            return;

        RegisterUser(user1);
        string userId1 = user1.UserId;
        if (string.IsNullOrWhiteSpace(userId1))
            return;

        // point1에 있는 userId 찾기
        string userId2 = null;
        foreach (var kv in _userSpawnPointMap)
        {
            if (kv.Value == point1)
            {
                userId2 = kv.Key;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(userId2) || userId2 == userId1)
            return;

        if (!TryGetUser(userId2, out UserObject user2) || user2 == null)
            return;

        // user1의 현재 스폰 위치 찾기
        if (!_userSpawnPointMap.TryGetValue(userId1, out Transform point2) || point2 == null)
            return;

        // 맵만 스왑(점유 집합은 그대로)
        _userSpawnPointMap[userId1] = point1;
        _userSpawnPointMap[userId2] = point2;

        user1.MoveToWorldPosition(point1.position);
        user2.MoveToWorldPosition(point2.position);
    }

    private void RegisterPlayerIfPossible()
    {
        if (player == null)
            return;

        UserObject playerUser = player.GetComponent<UserObject>();
        if (playerUser == null)
            playerUser = player.AddComponent<UserObject>();

        if (string.IsNullOrWhiteSpace(playerUser.UserId) || string.IsNullOrWhiteSpace(playerUser.UserName))
        {
            string id = string.IsNullOrWhiteSpace(playerUser.UserId) ? "player" : playerUser.UserId;
            string name = string.IsNullOrWhiteSpace(playerUser.UserName) ? "나" : playerUser.UserName;
            playerUser.SetIdentity(id, name);
        }

        RegisterUser(playerUser);
    }

    private void ReserveNearestSpawnPointForPlayer()
    {
        if (player == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        UserObject playerUser = player.GetComponent<UserObject>();
        if (playerUser == null)
            return;

        RegisterUser(playerUser);
        string playerId = playerUser.UserId;
        if (string.IsNullOrWhiteSpace(playerId))
            return;

        Transform nearest = null;
        float minDist = float.MaxValue;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null) continue;
            float dist = Vector3.Distance(player.transform.position, point.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = point;
            }
        }

        if (nearest == null)
            return;

        // 이미 누군가 점유 중이면 건드리지 않음(초기엔 보통 비어 있음)
        if (_occupiedSpawnPoints.Contains(nearest))
            return;

        _userSpawnPointMap[playerId] = nearest;
        _occupiedSpawnPoints.Add(nearest);
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        foreach (var point in spawnPoints)
        {
            if (point == null) continue;

            bool isOccupied = _occupiedSpawnPoints.Contains(point);
            Gizmos.color = isOccupied
                ? new Color(1f, 0.3f, 0.3f, 0.7f)
                : new Color(0f, 1f, 0.5f, 0.7f);

            Gizmos.DrawWireSphere(point.position, 0.3f);
        }
    }
}
