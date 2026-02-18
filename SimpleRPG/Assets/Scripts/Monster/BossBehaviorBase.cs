using UnityEngine;

/// <summary>
/// 보스 몬스터 행동 패턴 컴포넌트의 베이스. 같은 GameObject에 BossMonster가 있어야 함.
/// </summary>
public abstract class BossBehaviorBase : MonoBehaviour
{
    protected BossMonster Boss { get; private set; }

    protected virtual void Awake()
    {
        Boss = GetComponent<BossMonster>();
        if (Boss == null)
            Debug.LogWarning($"[{GetType().Name}] 같은 오브젝트에 BossMonster가 없습니다.", this);
    }
}
