using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("플레이어")]
    public GameObject player;

    [Header("유저")]
    public UserObject selectedUser;
    public Dictionary<string, UserObject> users = new Dictionary<string, UserObject>();

    public Image userProfileImage;
    public TextMeshProUGUI userNameText;

    [Header("스폰 포인트")]
    [Tooltip("고정 스폰 위치 Transform 배열")]
    [SerializeField] private Transform[] spawnPoints = new Transform[0];
    [Tooltip("경로 찾기 그리드 (비어 있으면 자동 검색)")]
    [SerializeField] private PathfindingGrid pathfindingGrid;

    [Header("데미지 UI")]
    [Tooltip("데미지 텍스트가 생성될 캔버스")]
    public Canvas damageCanvas;

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
        if (pathfindingGrid == null)
            pathfindingGrid = FindFirstObjectByType<PathfindingGrid>();

        // PathfindingGrid에 spawnPoints 설정
        if (pathfindingGrid != null && spawnPoints != null && spawnPoints.Length > 0)
        {
            pathfindingGrid.InitializeGrid();
            pathfindingGrid.SetWalkableFromSpawnPoints(spawnPoints);
        }

        if (damageCanvas == null)
            damageCanvas = FindFirstObjectByType<Canvas>(); // 폴백: 씬의 첫 캔버스

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
                userProfileImage.sprite = user.profileSprite.sprite;
                userNameText.text = user.UserName;
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
                
                // 빈 위치든 점유된 위치든 이동 (경로에 유저가 있으면 한 칸씩 교환)
                MoveUserToPoint(selectedUser, clickedPoint);
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

        // 그리드 기반으로 한 칸씩 할당된 위치로 이동
        Vector3 targetWorldPos = targetPoint.position;
        if (pathfindingGrid != null)
        {
            // 목표 위치를 그리드 좌표로 변환 후 다시 월드 좌표로 변환 (그리드 셀 중심으로 정렬)
            Vector2Int gridPos = pathfindingGrid.WorldToGrid(targetWorldPos);
            targetWorldPos = pathfindingGrid.GridToWorld(gridPos);
        }

        // 경로 계산
        List<Vector3> path = new List<Vector3>();
        if (pathfindingGrid != null)
        {
            path = pathfindingGrid.FindPath(user.transform.position, targetWorldPos);
        }
        else
        {
            path = new List<Vector3> { targetWorldPos };
        }

        if (path == null || path.Count == 0)
        {
            path = new List<Vector3> { targetWorldPos };
        }

        // 경로의 각 칸을 순차적으로 할당하면서 이동
        StartCoroutine(MoveUserAlongPath(user, userId, path, targetPoint));
    }

    /// <summary>
    /// 경로를 따라 한 칸씩 이동하면서 각 칸을 할당
    /// </summary>
    private System.Collections.IEnumerator MoveUserAlongPath(UserObject user, string userId, List<Vector3> path, Transform finalTargetPoint)
    {
        if (user == null || path == null || path.Count == 0)
            yield break;

        // 기존 위치 해제
        ReleaseSpawnPoint(userId);

        // 경로의 각 칸을 순차적으로 할당
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 cellPos = path[i];
            
            // 해당 위치에 유저가 있는지 확인
            UserObject userAtCell = GetUserAtPosition(cellPos);
            
            if (userAtCell != null && userAtCell != user)
            {
                // 경로에 유저가 있으면 한 칸씩 자리 바꾸기
                string otherUserId = userAtCell.UserId;
                if (!string.IsNullOrWhiteSpace(otherUserId))
                {
                    // 현재 유저의 이전 위치 가져오기
                    Vector3 currentUserPos = user.transform.position;
                    Transform currentUserPoint = _userSpawnPointMap.TryGetValue(userId, out Transform p1) ? p1 : null;
                    
                    // 다른 유저의 현재 위치
                    Vector3 otherUserPos = userAtCell.transform.position;
                    Transform otherUserPoint = _userSpawnPointMap.TryGetValue(otherUserId, out Transform p2) ? p2 : null;
                    
                    // 위치 교환
                    if (currentUserPoint != null && otherUserPoint != null)
                    {
                        _userSpawnPointMap[userId] = otherUserPoint;
                        _userSpawnPointMap[otherUserId] = currentUserPoint;
                    }
                    else if (currentUserPoint != null)
                    {
                        _userSpawnPointMap[userId] = otherUserPoint;
                        if (otherUserPoint != null)
                        {
                            _userSpawnPointMap[otherUserId] = currentUserPoint;
                            _occupiedSpawnPoints.Remove(otherUserPoint);
                            _occupiedSpawnPoints.Add(currentUserPoint);
                        }
                    }
                    else if (otherUserPoint != null)
                    {
                        _userSpawnPointMap[userId] = otherUserPoint;
                        _userSpawnPointMap[otherUserId] = currentUserPoint;
                        _occupiedSpawnPoints.Remove(otherUserPoint);
                        if (currentUserPoint != null)
                            _occupiedSpawnPoints.Add(currentUserPoint);
                    }
                    
                    // 한 칸씩 위치 교환 (동시에 이동)
                    yield return StartCoroutine(SwapPositionsOneStep(user, userAtCell, cellPos, currentUserPos));
                    continue;
                }
            }
            
            // 해당 위치에 가장 가까운 스폰 포인트 찾기
            Transform nearestPoint = FindNearestSpawnPoint(cellPos);
            
            if (nearestPoint != null)
            {
                // 기존 할당 해제
                if (_userSpawnPointMap.ContainsKey(userId))
                {
                    Transform oldPoint = _userSpawnPointMap[userId];
                    if (oldPoint != null)
                        _occupiedSpawnPoints.Remove(oldPoint);
                }

                // 새 칸 할당
                _userSpawnPointMap[userId] = nearestPoint;
                _occupiedSpawnPoints.Add(nearestPoint);
            }

            // 해당 칸으로 이동
            float distance = Vector3.Distance(user.transform.position, cellPos);
            float duration = user.MoveSpeed > 0f ? distance / user.MoveSpeed : 0f;
            
            float elapsed = 0f;
            Vector3 startPos = user.transform.position;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                user.transform.position = Vector3.Lerp(startPos, cellPos, t);
                yield return null;
            }
            
            user.transform.position = cellPos;
        }

        // 최종 목표 지점 할당
        if (finalTargetPoint != null)
        {
            if (_userSpawnPointMap.ContainsKey(userId))
            {
                Transform oldPoint = _userSpawnPointMap[userId];
                if (oldPoint != null)
                    _occupiedSpawnPoints.Remove(oldPoint);
            }

            _userSpawnPointMap[userId] = finalTargetPoint;
            _occupiedSpawnPoints.Add(finalTargetPoint);
        }
    }

    /// <summary>
    /// 두 유저의 위치를 한 칸씩 교환합니다
    /// </summary>
    private IEnumerator SwapPositionsOneStep(UserObject user1, UserObject user2, Vector3 targetPos1, Vector3 targetPos2)
    {
        if (user1 == null || user2 == null)
            yield break;

        float distance1 = Vector3.Distance(user1.transform.position, targetPos1);
        float distance2 = Vector3.Distance(user2.transform.position, targetPos2);
        float duration1 = user1.MoveSpeed > 0f ? distance1 / user1.MoveSpeed : 0f;
        float duration2 = user2.MoveSpeed > 0f ? distance2 / user2.MoveSpeed : 0f;
        float maxDuration = Mathf.Max(duration1, duration2);

        float elapsed = 0f;
        Vector3 startPos1 = user1.transform.position;
        Vector3 startPos2 = user2.transform.position;

        while (elapsed < maxDuration)
        {
            elapsed += Time.deltaTime;
            float t1 = Mathf.Clamp01(elapsed / duration1);
            float t2 = Mathf.Clamp01(elapsed / duration2);

            user1.transform.position = Vector3.Lerp(startPos1, targetPos1, t1);
            user2.transform.position = Vector3.Lerp(startPos2, targetPos2, t2);
            yield return null;
        }

        user1.transform.position = targetPos1;
        user2.transform.position = targetPos2;
    }

    /// <summary>
    /// 특정 위치에 가장 가까운 스폰 포인트를 찾습니다
    /// </summary>
    private Transform FindNearestSpawnPoint(Vector3 position)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        Transform nearest = null;
        float minDist = float.MaxValue;

        foreach (Transform point in spawnPoints)
        {
            if (point == null) continue;
            
            float dist = Vector3.Distance(position, point.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = point;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 특정 위치에 있는 유저를 찾습니다
    /// </summary>
    private UserObject GetUserAtPosition(Vector3 position)
    {
        float checkRadius = 0.5f; // 체크 반경
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, checkRadius);
        
        foreach (Collider2D col in colliders)
        {
            if (col == null) continue;
            
            UserObject user = col.GetComponent<UserObject>();
            if (user != null && users.ContainsValue(user))
            {
                return user;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 두 유저의 위치를 바꿉니다
    /// </summary>
    private void SwapUserPositions(UserObject user1, UserObject user2)
    {
        if (user1 == null || user2 == null)
            return;

        RegisterUser(user1);
        RegisterUser(user2);
        
        string userId1 = user1.UserId;
        string userId2 = user2.UserId;
        
        if (string.IsNullOrWhiteSpace(userId1) || string.IsNullOrWhiteSpace(userId2))
            return;

        // 각 유저의 현재 위치 가져오기
        Transform point1 = _userSpawnPointMap.TryGetValue(userId1, out Transform p1) ? p1 : null;
        Transform point2 = _userSpawnPointMap.TryGetValue(userId2, out Transform p2) ? p2 : null;

        // 위치 교환
        if (point1 != null && point2 != null)
        {
            _userSpawnPointMap[userId1] = point2;
            _userSpawnPointMap[userId2] = point1;
            
            user1.MoveToWorldPosition(point2.position);
            user2.MoveToWorldPosition(point1.position);
        }
        else if (point1 != null)
        {
            // user1은 위치가 있고 user2는 없음
            ReleaseSpawnPoint(userId1);
            _userSpawnPointMap[userId2] = point1;
            _occupiedSpawnPoints.Add(point1);
            user1.MoveToWorldPosition(user2.transform.position);
            user2.MoveToWorldPosition(point1.position);
        }
        else if (point2 != null)
        {
            // user2는 위치가 있고 user1은 없음
            ReleaseSpawnPoint(userId2);
            _userSpawnPointMap[userId1] = point2;
            _occupiedSpawnPoints.Add(point2);
            user1.MoveToWorldPosition(point2.position);
            user2.MoveToWorldPosition(user1.transform.position);
        }
        else
        {
            // 둘 다 위치가 없음 - 단순 위치 교환
            Vector3 tempPos = user1.transform.position;
            user1.MoveToWorldPosition(user2.transform.position);
            user2.MoveToWorldPosition(tempPos);
        }
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
