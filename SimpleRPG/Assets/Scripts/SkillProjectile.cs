using UnityEngine;

/// <summary>
/// 스킬 프로젝타일. 목표(몬스터)로 이동 후 데미지 적용하고 삭제.
/// </summary>
public class SkillProjectile : MonoBehaviour
{
    [Header("충돌 판정")]
    [Tooltip("이 거리 이내면 타격 처리 후 삭제")]
    [SerializeField] private float hitRadius = 0.5f;

    [Header("타겟 향하기")]
    [Tooltip("true면 프로젝타일이 타겟을 향하도록 회전")]
    [SerializeField] private bool faceTarget = true;

    [Header("이동 속도")]
    [Tooltip("프로젝타일 이동 속도")]
    [SerializeField] private float speed = 10f;

    [Header("히트 이펙트")]
    [SerializeField] private GameObject hitAnimationPrefab;
    [Tooltip("true면 히트 이펙트가 투사체 각도를 따라감")]
    [SerializeField] private bool hitAnimationFollowProjectileAngle = false;

    private UserObject _owner;
    private SkillData _skill;
    private MonsterObject _target;
    private bool _hit;
    private Vector3 _lastTargetPosition;
    private bool _targetLost = false;

    /// <summary>
    /// 프로젝타일 실행. target으로 이동, 도달 시 owner.CalculateSkillDamage(skill)로 데미지 계산 후 적용.
    /// </summary>
    public void Run(UserObject owner, SkillData skill, MonsterObject target)
    {
        _owner = owner;
        _skill = skill;
        _target = target;
        _hit = false;
        _targetLost = false;

        // 타겟을 향하도록 회전
        if (faceTarget && _target != null)
        {
            Vector3 direction = (_target.transform.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    private void Update()
    {
        if (_hit) return;

        Vector3 targetPos;
        Vector3 dir;
        float dist;

        // 타겟이 없어진 경우 마지막 위치로 이동
        if (_target == null)
        {
            if (!_targetLost)
            {
                // 타겟이 처음으로 없어진 경우, 마지막 위치 저장
                _targetLost = true;
                _lastTargetPosition = transform.position + transform.right * 10f; // 기본값: 현재 방향으로 10유닛
            }
            targetPos = _lastTargetPosition;
        }
        else
        {
            // 타겟이 있는 경우, 마지막 위치 업데이트
            _lastTargetPosition = _target.transform.position;
            targetPos = _lastTargetPosition;
        }

        dir = (targetPos - transform.position).normalized;
        dist = Vector3.Distance(transform.position, targetPos);

        // 타겟을 향하도록 회전 (이동 중에도 업데이트)
        if (faceTarget && dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // 타겟이 있고 충돌 거리 이내면 데미지 적용 (호출 시점에 데미지 계산)
        if (!_targetLost && _target != null && dist <= hitRadius)
        {
            _hit = true;
            if (_owner != null && _skill != null)
            {
                int damage = _owner.CalculateSkillDamage(_skill);
                _target.TakeDamage(damage);
            }
            SpawnHitAnimation();
            Destroy(gameObject);
            return;
        }

        // 마지막 위치에 도달하면 삭제
        if (_targetLost && dist <= hitRadius)
        {
            _hit = true;
            SpawnHitAnimation();
            Destroy(gameObject);
            return;
        }

        float move = speed * Time.deltaTime;
        if (move > dist) move = dist;
        transform.position += dir * move;
    }

    private void SpawnHitAnimation()
    {
        if (hitAnimationPrefab == null)
            return;

        Vector3 spawnPos = _target != null ? _target.transform.position : transform.position;
        Quaternion rotation = Quaternion.identity;

        // 투사체 각도를 따라가도록 설정
        if (hitAnimationFollowProjectileAngle)
        {
            rotation = transform.rotation;
        }

        Instantiate(hitAnimationPrefab, spawnPos, rotation);
    }
}
