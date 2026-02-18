using UnityEngine;

/// <summary>
/// 스킬 프로젝타일. 타겟의 저장된 위치로 이동(저장 위치는 타겟이 있는 동안 매 프레임 갱신).
/// 타겟이 없어져도 마지막으로 저장된 위치까지 이동 후 히트 처리. 도달 시 데미지 적용 후 삭제.
/// </summary>
public class SkillProjectile : MonoBehaviour
{
    [Header("충돌 판정")]
    [Tooltip("이 거리 이내면 타격 처리 후 삭제")]
    [SerializeField] private float hitRadius = 0.5f;

    [Header("타겟 향하기")]
    [Tooltip("true면 프로젝타일이 목표 위치를 향하도록 회전")]
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
    /// <summary>이동 목표 위치. 타겟이 있는 동안 매 프레임 갱신, 타겟이 없어지면 갱신 중단 후 마지막 위치 유지.</summary>
    private Vector3 _storedTargetPosition;

    /// <summary>
    /// 프로젝타일 실행. 타겟의 위치를 저장하고, 그 저장 위치로 이동. 도달 시 데미지 적용 후 삭제.
    /// </summary>
    public void Run(UserObject owner, SkillData skill, MonsterObject target)
    {
        _owner = owner;
        _skill = skill;
        _target = target;
        _hit = false;
        _storedTargetPosition = target != null ? target.transform.position : transform.position;

        if (faceTarget)
        {
            Vector3 direction = (_storedTargetPosition - transform.position).normalized;
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

        // 타겟이 있으면 저장 위치 계속 업데이트, 없으면 업데이트 중단(마지막 저장 위치 유지)
        if (_target != null)
            _storedTargetPosition = _target.transform.position;

        Vector3 dir = (_storedTargetPosition - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, _storedTargetPosition);

        if (faceTarget && dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (dist <= hitRadius)
        {
            _hit = true;
            if (_target != null && _owner != null && _skill != null)
            {
                int damage = _owner.CalculateSkillDamage(_skill);
                _target.TakeDamage(damage);
            }
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

        Vector3 spawnPos = _target != null ? _target.transform.position : _storedTargetPosition;
        Quaternion rotation = hitAnimationFollowProjectileAngle ? transform.rotation : Quaternion.identity;
        Instantiate(hitAnimationPrefab, spawnPos, rotation);
    }
}
