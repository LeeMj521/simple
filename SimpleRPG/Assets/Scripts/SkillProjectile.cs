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

    private int _damage;
    private MonsterObject _target;
    private bool _hit;

    /// <summary>
    /// 프로젝타일 실행. target으로 이동, 도달 시 데미지 적용 후 삭제.
    /// </summary>
    public void Run(int damage, MonsterObject target)
    {
        _damage = Mathf.Max(0, damage);
        _target = target;
        _hit = false;

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
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        Transform t = _target.transform;
        Vector3 dir = (t.position - transform.position).normalized;
        float dist = Vector3.Distance(transform.position, t.position);

        // 타겟을 향하도록 회전 (이동 중에도 업데이트)
        if (faceTarget && dir != Vector3.zero)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (dist <= hitRadius)
        {
            _hit = true;
            _target.TakeDamage(_damage);
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
