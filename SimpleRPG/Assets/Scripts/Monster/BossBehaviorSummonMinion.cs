using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보스 행동 패턴: 일정 간격으로 일반 몬스터를 플레이어 스폰 포인트에 소환.
/// 보스 사망 시 소환된 미니언도 함께 제거됨.
/// </summary>
public class BossBehaviorSummonMinion : BossBehaviorBase
{
    [Header("소환 설정")]
    [Tooltip("소환할 일반 몬스터 프리팹 목록. 이 중 하나를 랜덤으로 소환. 스탯은 각 프리팹의 MinionMonster(minionMaxHp 등) 사용")]
    [SerializeField] private GameObject[] minionMonsterPrefabs = System.Array.Empty<GameObject>();
    [Tooltip("소환 쿨다운(초). 0이면 한 번만 실행 가능")]
    [SerializeField] private float cooldownSeconds = 5f;
    [Tooltip("동시에 존재 가능한 최대 소환수 (0 = 제한 없음)")]
    [SerializeField] private int maxMinionsAlive = 5;

    [Header("참조")]
    [SerializeField] private GameManager gameManager;

    private float _nextSummonTime;
    private int _minionsSpawnedCount;
    private readonly List<MinionMonster> _spawnedMinions = new List<MinionMonster>();

    private void Start()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();
        _nextSummonTime = Time.time;
        if (Boss != null)
            Boss.OnDeath += OnBossDeath;
    }

    private void OnDestroy()
    {
        if (Boss != null)
            Boss.OnDeath -= OnBossDeath;
        KillAllMinions();
    }

    private void OnBossDeath()
    {
        KillAllMinions();
    }

    private void KillAllMinions()
    {
        for (int i = _spawnedMinions.Count - 1; i >= 0; i--)
        {
            MinionMonster m = _spawnedMinions[i];
            if (m != null && m.gameObject != null)
                Destroy(m.gameObject);
        }
        _spawnedMinions.Clear();
        _minionsSpawnedCount = 0;
    }

    private void Update()
    {
        if (Boss == null || gameManager == null)
            return;
        if (minionMonsterPrefabs == null || minionMonsterPrefabs.Length == 0)
            return;
        if (Time.time < _nextSummonTime)
            return;
        if (maxMinionsAlive > 0 && _minionsSpawnedCount >= maxMinionsAlive)
            return;

        int index = Random.Range(0, minionMonsterPrefabs.Length);
        GameObject prefab = minionMonsterPrefabs[index];
        if (prefab == null)
            return;

        MinionMonster existingMinion = prefab.GetComponent<MinionMonster>();
        if (existingMinion == null)
        {
            Debug.LogWarning($"[BossBehaviorSummonMinion] 프리팹({prefab.name})에 MinionMonster가 없습니다.", this);
            return;
        }

        string spawnId = "minion_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        if (!gameManager.TryReserveFreeSpawnPoint(spawnId, null, out _, out Transform spawnPoint))
            return;

        GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        MinionMonster minion = go.GetComponent<MinionMonster>();
        if (minion == null)
            minion = go.AddComponent<MinionMonster>();

        minion.Init(gameManager.damageCanvas, spawnId, gameManager);
        _spawnedMinions.Add(minion);
        minion.OnDeath += () =>
        {
            _spawnedMinions.Remove(minion);
            OnMinionDeath();
            if (minion != null && minion.gameObject != null)
                Destroy(minion.gameObject);
        };

        _minionsSpawnedCount++;
        _nextSummonTime = Time.time + cooldownSeconds;
    }

    private void OnMinionDeath()
    {
        _minionsSpawnedCount = Mathf.Max(0, _minionsSpawnedCount - 1);
    }
}
