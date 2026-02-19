using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일반 몬스터. 보스의 소환 패턴으로 생성되며 플레이어 스폰 포인트를 공유한다.
/// 스탯은 MonsterData(보스용)가 아닌 프리팹의 minionMaxHp / minionName / minionLevel 사용.
/// HUD는 프리팹 내장 캔버스 사용. 현재는 AI 없이 대기만 함.
/// </summary>
public class MinionMonster : MonsterObject
{
    [SerializeField] private int minionMaxHp = 10;
    [SerializeField] private string minionName = "Minion";
    [SerializeField] private int minionLevel = 1;
    [Header("드랍 테이블")]
    [Tooltip("프리팹에서 설정하는 드랍 테이블")]
    [SerializeField] private List<DropTableEntry> minionDropTable = new List<DropTableEntry>();

    private string _minionSpawnId;
    private GameManager _gameManager;

    /// <summary>
    /// 일반 몬스터 전용 초기화. 스폰 포인트 해제를 위해 minionId와 GameManager 필요.
    /// </summary>
    /// <param name="monsterData">몬스터 데이터</param>
    /// <param name="damageCanvas">데미지 텍스트용 캔버스</param>
    /// <param name="minionSpawnId">스폰 포인트 예약 시 사용한 ID (사망 시 Release에 사용)</param>
    /// <param name="gameManager">스폰 포인트 해제용</param>
    public override void Init(MonsterData monsterData, Canvas damageCanvas = null)
    {
        base.Init(monsterData, damageCanvas);
    }

    /// <summary>
    /// 미니언 전용 초기화. MonsterData 없이 프리팹의 minionMaxHp/minionName/minionLevel으로 스탯 적용.
    /// 프리팹에 설정된 드랍 테이블을 사용합니다.
    /// </summary>
    public void Init(Canvas damageCanvas, string minionSpawnId, GameManager gameManager)
    {
        var data = new MonsterData("minion", minionName, minionMaxHp, minionLevel, 0, 0, "");
        
        // 프리팹에 설정된 드랍 테이블 복사
        if (minionDropTable != null && minionDropTable.Count > 0)
        {
            data.dropTable = new List<DropTableEntry>(minionDropTable);
        }
        
        Init(data, damageCanvas, minionSpawnId, gameManager);
    }

    /// <summary>
    /// 스폰 포인트 공유용 ID와 GameManager를 넣을 때 사용. 사망 시 해당 스폰 포인트를 해제한다.
    /// </summary>
    public void Init(MonsterData monsterData, Canvas damageCanvas, string minionSpawnId, GameManager gameManager)
    {
        base.Init(monsterData, damageCanvas);

        _minionSpawnId = minionSpawnId;
        _gameManager = gameManager;

        if (!string.IsNullOrEmpty(_minionSpawnId) && _gameManager != null)
            OnDeath += ReleaseSpawnPointOnDeath;
    }

    private void ReleaseSpawnPointOnDeath()
    {
        OnDeath -= ReleaseSpawnPointOnDeath;
        ReleaseSpawnPoint();
    }

    /// <summary>
    /// 스폰 포인트 해제. 사망 시·OnDestroy(보스 사망 등으로 파괴 시) 양쪽에서 한 번만 해제되도록 호출 후 ID를 비운다.
    /// </summary>
    private void ReleaseSpawnPoint()
    {
        if (_gameManager == null || string.IsNullOrEmpty(_minionSpawnId))
            return;
        _gameManager.ReleaseSpawnPoint(_minionSpawnId);
        _minionSpawnId = null;
    }

    private void OnDestroy()
    {
        ReleaseSpawnPoint();
    }
}
