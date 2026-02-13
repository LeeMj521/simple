using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A* 경로 찾기를 위한 그리드 시스템
/// </summary>
public class PathfindingGrid : MonoBehaviour
{
    [Header("그리드 설정")]
    [Tooltip("그리드 셀 크기")]
    [SerializeField] private float cellSize = 1f;
    [Tooltip("그리드 너비 (셀 개수)")]
    [SerializeField] private int gridWidth = 50;
    [Tooltip("그리드 높이 (셀 개수)")]
    [SerializeField] private int gridHeight = 50;
    [Tooltip("그리드 중심 위치 (월드 좌표)")]
    [SerializeField] private Vector3 gridCenter = Vector3.zero;

    private bool[,] _walkableGrid; // true = 이동 가능, false = 이동 불가

    private void Awake()
    {
        InitializeGrid();
    }

    private void InitializeGrid()
    {
        _walkableGrid = new bool[gridWidth, gridHeight];
        
        // 기본적으로 모든 셀을 이동 불가능으로 설정
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                _walkableGrid[x, y] = false;
            }
        }
    }

    /// <summary>
    /// spawnPoints 위치에 해당하는 그리드 셀만 이동 가능하도록 설정
    /// </summary>
    public void SetWalkableFromSpawnPoints(Transform[] spawnPoints)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

        // 모든 셀을 이동 불가능으로 초기화
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                _walkableGrid[x, y] = false;
            }
        }

        // spawnPoints 위치에 해당하는 셀만 이동 가능으로 설정
        foreach (Transform spawnPoint in spawnPoints)
        {
            if (spawnPoint == null) continue;

            Vector2Int gridPos = WorldToGrid(spawnPoint.position);
            if (gridPos.x >= 0 && gridPos.x < gridWidth && gridPos.y >= 0 && gridPos.y < gridHeight)
            {
                _walkableGrid[gridPos.x, gridPos.y] = true;
            }
        }
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - gridCenter;
        int x = Mathf.FloorToInt(localPos.x / cellSize) + gridWidth / 2;
        int y = Mathf.FloorToInt(localPos.y / cellSize) + gridHeight / 2;
        return new Vector2Int(Mathf.Clamp(x, 0, gridWidth - 1), Mathf.Clamp(y, 0, gridHeight - 1));
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = (gridPos.x - gridWidth / 2) * cellSize + cellSize * 0.5f;
        float y = (gridPos.y - gridHeight / 2) * cellSize + cellSize * 0.5f;
        return gridCenter + new Vector3(x, y, 0);
    }

    /// <summary>
    /// 해당 그리드 셀이 이동 가능한지 확인
    /// </summary>
    public bool IsWalkable(Vector2Int gridPos)
    {
        if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight)
            return false;
        return _walkableGrid[gridPos.x, gridPos.y];
    }

    /// <summary>
    /// 그리드 셀의 이동 가능 여부 설정
    /// </summary>
    public void SetWalkable(Vector2Int gridPos, bool walkable)
    {
        if (gridPos.x < 0 || gridPos.x >= gridWidth || gridPos.y < 0 || gridPos.y >= gridHeight)
            return;
        _walkableGrid[gridPos.x, gridPos.y] = walkable;
    }


    /// <summary>
    /// A* 경로 찾기
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
    {
        Vector2Int startGrid = WorldToGrid(startWorld);
        Vector2Int endGrid = WorldToGrid(endWorld);

        if (!IsWalkable(startGrid) || !IsWalkable(endGrid))
        {
            Debug.LogWarning($"[PathfindingGrid] 시작 또는 목표 지점이 이동 불가능합니다. Start: {startGrid}, End: {endGrid}");
            return new List<Vector3> { endWorld }; // 목표 지점만 반환
        }

        List<Vector2Int> gridPath = FindPathAStar(startGrid, endGrid);
        
        if (gridPath == null || gridPath.Count == 0)
        {
            return new List<Vector3> { endWorld };
        }

        // 그리드 경로를 월드 좌표로 변환
        List<Vector3> worldPath = new List<Vector3>();
        foreach (Vector2Int gridPos in gridPath)
        {
            worldPath.Add(GridToWorld(gridPos));
        }

        return worldPath;
    }

    private List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int end)
    {
        // A* 알고리즘 구현
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
        
        List<Vector2Int> openSet = new List<Vector2Int> { start };
        gScore[start] = 0;
        fScore[start] = Heuristic(start, end);

        while (openSet.Count > 0)
        {
            // fScore가 가장 낮은 노드 선택
            Vector2Int current = openSet[0];
            float lowestF = fScore.ContainsKey(current) ? fScore[current] : float.MaxValue;
            
            foreach (Vector2Int node in openSet)
            {
                float nodeF = fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;
                if (nodeF < lowestF)
                {
                    current = node;
                    lowestF = nodeF;
                }
            }

            if (current == end)
            {
                // 경로 재구성
                List<Vector2Int> path = new List<Vector2Int>();
                Vector2Int node = end;
                while (cameFrom.ContainsKey(node))
                {
                    path.Add(node);
                    node = cameFrom[node];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            // 인접 노드 검사 (상하좌우만, 대각선 제거)
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(current.x + 1, current.y),     // 우
                new Vector2Int(current.x - 1, current.y),     // 좌
                new Vector2Int(current.x, current.y + 1),   // 상
                new Vector2Int(current.x, current.y - 1)    // 하
            };

            foreach (Vector2Int neighbor in neighbors)
            {
                if (closedSet.Contains(neighbor))
                    continue;

                if (!IsWalkable(neighbor))
                    continue;

                float tentativeG = (gScore.ContainsKey(current) ? gScore[current] : float.MaxValue) + 1f;

                if (!openSet.Contains(neighbor))
                    openSet.Add(neighbor);
                else if (gScore.ContainsKey(neighbor) && tentativeG >= gScore[neighbor])
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, end);
            }
        }

        // 경로를 찾지 못함
        return null;
    }

    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        // 맨해튼 거리
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private void OnDrawGizmos()
    {
        if (_walkableGrid == null)
            return;

        // 그리드 시각화 (에디터에서만)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 worldPos = GridToWorld(new Vector2Int(x, y));
                Gizmos.color = _walkableGrid[x, y] ? Color.green : Color.red;
                Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.8f);
            }
        }
    }
}
