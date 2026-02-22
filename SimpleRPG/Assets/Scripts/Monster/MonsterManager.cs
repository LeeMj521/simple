using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 방치형 전투: 지정 위치에 몬스터 프리팹 생성. HP가 0이 되면 다음 몬스터 스폰.
/// </summary>
public class MonsterManager : MonoBehaviour
{
    [Header("스폰 설정")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("스테이지 ID (DataManager의 Stages.json). 일반=랜덤 등장, 도전=순서대로 등장. 비어 있으면 전체 몬스터 순서대로.")]
    [SerializeField] private string stageId;

    [Header("참조")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DataManager dataManager;

    private List<string> _spawnOrder = new List<string>();
    private int _currentIndex;
    private MonsterObject _currentMonster;
    private bool _initialized;
    private StageType _stageType = StageType.Challenge;

    /// <summary>현재 필드에 있는 몬스터 (없으면 null)</summary>
    public MonsterObject CurrentMonster => _currentMonster;

    /// <summary>런타임에 스테이지 변경. 적용 후 즉시 새 스테이지 기준으로 몬스터를 스폰합니다.</summary>
    public void SetStage(string newStageId)
    {
        stageId = newStageId ?? "";
        _currentIndex = 0;
        if (_initialized)
        {
            BuildSpawnOrder();
            SpawnNext();
        }
    }

    private void Start()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        if (dataManager == null)
            dataManager = FindFirstObjectByType<DataManager>();

        if (dataManager == null)
        {
            Debug.LogError("[MonsterManager] DataManager를 찾을 수 없습니다.");
            return;
        }

        BuildSpawnOrder();
        _initialized = true;
        SpawnNext();
    }

    private void BuildSpawnOrder()
    {
        _spawnOrder.Clear();
        var monsters = dataManager.GetLoadedMonsters();

        if (!string.IsNullOrEmpty(stageId))
        {
            StageData stage = dataManager.GetStage(stageId);
            if (stage == null)
            {
                Debug.LogWarning($"[MonsterManager] 스테이지 ID를 찾을 수 없습니다: '{stageId}'. Stages.json을 확인하세요. 전체 몬스터 순서로 대체합니다.");
            }
            else if (stage.monsterIds != null)
            {
                foreach (string id in stage.monsterIds)
                {
                    if (!string.IsNullOrEmpty(id) && monsters.ContainsKey(id))
                        _spawnOrder.Add(id);
                }
                _stageType = stage.stageType;
                if (_spawnOrder.Count == 0)
                    Debug.LogWarning($"[MonsterManager] 스테이지 '{stageId}'에 유효한 몬스터가 없습니다. Monsters.json ID를 확인하세요.");
                return;
            }
        }

        _stageType = StageType.Challenge;
        foreach (var kv in monsters)
            _spawnOrder.Add(kv.Key);

        if (_spawnOrder.Count == 0)
            Debug.LogWarning("[MonsterManager] 스폰할 몬스터가 없습니다. Monsters.json을 확인하세요.");
    }

    /// <summary>
    /// 다음 몬스터를 스폰합니다. 현재 몬스터가 있으면 제거 후 스폰.
    /// </summary>
    public void SpawnNext()
    {
        if (!_initialized || _spawnOrder.Count == 0)
            return;

        if (_currentMonster != null)
        {
            _currentMonster.OnDeath -= HandleMonsterDeath;
            Destroy(_currentMonster.gameObject);
            _currentMonster = null;
        }

        string monsterId;
        if (_stageType == StageType.Normal)
            monsterId = _spawnOrder[UnityEngine.Random.Range(0, _spawnOrder.Count)];
        else
        {
            monsterId = _spawnOrder[_currentIndex % _spawnOrder.Count];
            _currentIndex++;
        }

        MonsterData data = dataManager.GetMonster(monsterId);
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning($"[MonsterManager] 몬스터 데이터 또는 프리팹 없음: {monsterId}. 다음 시도.");
            SpawnNext();
            return;
        }

        GameObject go = Instantiate(data.prefab, spawnPoint.position, spawnPoint.rotation);
        _currentMonster = go.GetComponent<MonsterObject>();
        if (_currentMonster == null)
            _currentMonster = go.AddComponent<MonsterObject>();

        if (_currentMonster is BossMonster boss)
        {
            boss.Init(data, gameManager.damageCanvas);
            if (gameManager.bossHud != null)
                gameManager.bossHud.Bind(_currentMonster);
        }
        else
        {
            _currentMonster.Init(data, gameManager.damageCanvas);
            if (gameManager.bossHud != null)
                gameManager.bossHud.Unbind();
        }

        _currentMonster.OnDeath += HandleMonsterDeath;
    }

    private void HandleMonsterDeath()
    {
        MonsterObject dying = _currentMonster;
        _currentMonster = null;
        if (gameManager != null && gameManager.bossHud != null)
            gameManager.bossHud.Unbind();
        if (dying != null)
        {
            // 드랍 테이블 처리
            dying.ProcessDropTable();
            
            dying.OnDeath -= HandleMonsterDeath;
            Destroy(dying.gameObject);
        }
        SpawnNext();
    }
}
