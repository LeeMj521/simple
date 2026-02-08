using System.Collections;
using UnityEngine;

/// <summary>
/// 스킬 이펙트. 프리팹 생성 → 애니메이션 재생 → 애니메이션 이벤트로 데미지/프로젝타일 호출 → 종료 이벤트 시 쿨타임 알림 후 삭제.
/// </summary>
public class SkillEffect : MonoBehaviour
{
    [Header("애니메이션 이벤트 (Function: OnApplyDamage / OnSpawnProjectile / OnAnimationEnd)")]

    [Header("타이밍 (폴백)")]
    [Tooltip("애니메이션 종료 이벤트 없을 때 이 시간 후 자동 종료")]
    [SerializeField] private float effectDuration = 5f;

    [Header("프로젝타일 (OnSpawnProjectile 사용 시)")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("프로젝타일 생성 위치. 비어 있으면 이 오브젝트 위치")]
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 10f;

    private UserObject _owner;
    private SkillData _skill;
    private MonsterManager _monsterManager;
    private int _damage;
    private Coroutine _safetyRoutine;
    private bool _finished;

    /// <summary>
    /// 이펙트 실행. 애니메이션 재생 후 이벤트로 OnApplyDamage / OnSpawnProjectile / OnAnimationEnd 호출.
    /// </summary>
    public void Run(UserObject owner, SkillData skill, MonsterManager monsterManager, int damage)
    {
        if (owner == null || skill == null)
        {
            FinishEffect(owner, skill);
            return;
        }

        _owner = owner;
        _skill = skill;
        _monsterManager = monsterManager;
        _damage = Mathf.Max(0, damage);
        _finished = false;

        if (_safetyRoutine != null)
            StopCoroutine(_safetyRoutine);
        _safetyRoutine = StartCoroutine(SafetyTimeoutRoutine());
    }

    private IEnumerator SafetyTimeoutRoutine()
    {
        float duration = effectDuration > 0f ? effectDuration : 5f;
        yield return new WaitForSeconds(duration);
        _safetyRoutine = null;
        if (!_finished)
            FinishEffect(_owner, _skill);
    }

    /// <summary>
    /// 애니메이션 이벤트: 데미지 적용 시점에 호출.
    /// </summary>
    public void OnApplyDamage()
    {
        ApplyDamage();
    }

    /// <summary>
    /// 애니메이션 이벤트: 프로젝타일 생성 시점에 호출.
    /// </summary>
    public void OnSpawnProjectile()
    {
        SpawnProjectile();
    }

    /// <summary>
    /// 애니메이션 이벤트: 클립 마지막 프레임에 호출. 종료 전달 후 삭제.
    /// </summary>
    public void OnAnimationEnd()
    {
        if (_finished) return;
        _finished = true;
        if (_safetyRoutine != null)
        {
            StopCoroutine(_safetyRoutine);
            _safetyRoutine = null;
        }
        FinishEffect(_owner, _skill);
    }

    private void ApplyDamage()
    {
        if (_monsterManager != null && _monsterManager.CurrentMonster != null && _damage > 0)
            _monsterManager.CurrentMonster.TakeDamage(_damage);
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null || _monsterManager == null || _monsterManager.CurrentMonster == null || _damage <= 0)
            return;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        SkillProjectile proj = go.GetComponent<SkillProjectile>();
        if (proj == null) proj = go.AddComponent<SkillProjectile>();
        proj.Run(_damage, _monsterManager.CurrentMonster, projectileSpeed);
    }

    private void FinishEffect(UserObject owner, SkillData skill)
    {
        if (owner != null && skill != null)
            owner.OnSkillEffectEnd(skill.skillId);
        _owner = null;
        _skill = null;
        if (gameObject != null)
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_safetyRoutine != null)
            StopCoroutine(_safetyRoutine);
        if (_owner != null && _skill != null)
            _owner.OnSkillEffectEnd(_skill.skillId);
    }
}
