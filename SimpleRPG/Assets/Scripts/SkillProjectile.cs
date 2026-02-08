using UnityEngine;

/// <summary>
/// 스킬 프로젝타일. 목표(몬스터)로 이동 후 데미지 적용하고 삭제.
/// </summary>
public class SkillProjectile : MonoBehaviour
{
    [Header("충돌 판정")]
    [Tooltip("이 거리 이내면 타격 처리 후 삭제")]
    [SerializeField] private float hitRadius = 0.5f;

    private int _damage;
    private MonsterObject _target;
    private float _speed;
    private bool _hit;

    /// <summary>
    /// 프로젝타일 실행. target으로 이동, 도달 시 데미지 적용 후 삭제.
    /// </summary>
    public void Run(int damage, MonsterObject target, float speed)
    {
        _damage = Mathf.Max(0, damage);
        _target = target;
        _speed = speed > 0f ? speed : 10f;
        _hit = false;
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

        if (dist <= hitRadius)
        {
            _hit = true;
            _target.TakeDamage(_damage);
            Destroy(gameObject);
            return;
        }

        float move = _speed * Time.deltaTime;
        if (move > dist) move = dist;
        transform.position += dir * move;
    }
}
