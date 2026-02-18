using UnityEngine;

/// <summary>
/// 보스 행동 패턴: 공격 패턴 플레이스홀더. 쿨다운 후 공격 로직 확장 가능.
/// </summary>
public class BossBehaviorAttackPattern : BossBehaviorBase
{
    [Header("공격 설정")]
    [Tooltip("공격 쿨다운(초)")]
    [SerializeField] private float cooldownSeconds = 3f;

    private float _nextAttackTime;

    private void Update()
    {
        if (Boss == null) return;
        if (Time.time < _nextAttackTime) return;

        ExecuteAttack();
        _nextAttackTime = Time.time + cooldownSeconds;
    }

    /// <summary>
    /// 서브클래스 또는 인스펙터 연동으로 실제 공격 로직 구현.
    /// </summary>
    protected virtual void ExecuteAttack()
    {
        // TODO: 스킬 발사, 범위 공격 등
    }
}
