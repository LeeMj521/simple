using UnityEngine;

/// <summary>
/// 보스 몬스터. 고정 위치에 생성되며, BossHUD로 이름·레벨·HP바를 연결한다.
/// 행동 패턴은 BossBehaviorBase 파생 컴포넌트로 조합.
/// </summary>
public class BossMonster : MonsterObject
{
    public override void Init(MonsterData monsterData, Canvas damageCanvas = null)
    {
        base.Init(monsterData, damageCanvas);
    }
}
