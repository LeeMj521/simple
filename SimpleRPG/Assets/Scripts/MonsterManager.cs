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
    [Tooltip("몬스터 id, 비어 있으면 DataManager의 몬스터 목록 순서대로 사용")]
    [SerializeField] private List<string> monsterIdOrder = new List<string>();

    [Header("참조")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DataManager dataManager;

    private List<string> _spawnOrder = new List<string>();
    private int _currentIndex;
    private MonsterObject _currentMonster;
    private bool _initialized;

    /// <summary>현재 필드에 있는 몬스터 (없으면 null)</summary>
    public MonsterObject CurrentMonster => _currentMonster;

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
        if (monsterIdOrder != null && monsterIdOrder.Count > 0)
        {
            var monsters = dataManager.GetLoadedMonsters();
            foreach (string id in monsterIdOrder)
            {
                if (monsters.ContainsKey(id))
                    _spawnOrder.Add(id);
            }
        }
        else
        {
            foreach (var kv in dataManager.GetLoadedMonsters())
                _spawnOrder.Add(kv.Key);
        }

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

        string monsterId = _spawnOrder[_currentIndex % _spawnOrder.Count];
        _currentIndex++;

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

        _currentMonster.Init(data, gameManager.damageCanvas);
        _currentMonster.OnDeath += HandleMonsterDeath;
    }

    private void HandleMonsterDeath()
    {
        MonsterObject dying = _currentMonster;
        _currentMonster = null;
        if (dying != null)
        {
            dying.OnDeath -= HandleMonsterDeath;
            Destroy(dying.gameObject);
        }
        SpawnNext();
    }
}
