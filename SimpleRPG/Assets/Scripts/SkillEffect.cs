using System.Collections;
using UnityEngine;

/// <summary>
/// 스킬 이펙트. 프리팹 생성 → 애니메이션 재생 → 애니메이션 이벤트로 데미지/프로젝타일 호출 → 종료 이벤트 시 쿨타임 알림 후 삭제.
/// </summary>
public class SkillEffect : MonoBehaviour
{
  [Header("애니메이션 이벤트 (Function: OnApplyDamage / OnApplyDamageAll / OnSpawnProjectile / OnSpawnHitAnimation / OnAnimationEnd)")]

  [Header("타겟 향하기")]
  [Tooltip("true면 이펙트가 타겟을 향하도록 회전")]
  [SerializeField] private bool faceTarget = false;

  [Header("프로젝타일 (OnSpawnProjectile 사용 시)")]
  [SerializeField] private GameObject projectilePrefab;
  [Tooltip("프로젝타일 생성 위치. 비어 있으면 이 오브젝트 위치")]
  [SerializeField] private Transform projectileSpawnPoint;

  [Header("히트 이펙트 (OnSpawnHitAnimation 사용 시)")]
  [SerializeField] private GameObject hitAnimationPrefab;
  [Tooltip("히트 이펙트 생성 위치. 비어 있으면 몬스터 위치")]
  [SerializeField] private Transform hitSpawnPoint;

  private UserObject _owner;
  private SkillData _skill;
  private MonsterManager _monsterManager;
  private MonsterObject _targetMonster; // 수동으로 지정한 타겟 (없으면 CurrentMonster 사용)
  private bool _finished;

  void Update(){
    if(_owner == null){
      FinishEffect(null, _skill);
      return;
    }
    transform.position = _owner.transform.position;
    MonsterObject target = GetTarget();
    if(faceTarget && target != null){
      Vector3 direction = (target.transform.position - transform.position).normalized;
      if(direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }
  }

  /// <summary>
  /// 이펙트 실행. 애니메이션 재생 후 이벤트로 OnApplyDamage / OnSpawnProjectile / OnAnimationEnd 호출.
  /// 데미지는 적용 시점에 owner.CalculateSkillDamage(skill)로 계산.
  /// </summary>
  public void Run(UserObject owner, SkillData skill, MonsterManager monsterManager, MonsterObject targetMonster = null){
    if (owner == null || skill == null){
      FinishEffect(owner, skill);
      return;
    }

    _owner = owner;
    _skill = skill;
    _monsterManager = monsterManager;
    _targetMonster = targetMonster;

    // 타겟을 향하도록 회전
    MonsterObject target = GetTarget();
    if (faceTarget && target != null){
      Vector3 direction = (target.transform.position - transform.position).normalized;
      if (direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }
  }

  /// <summary>
  /// 현재 타겟을 반환 (수동 타겟이 있으면 그것을, 없으면 CurrentMonster를 반환)
  /// </summary>
  private MonsterObject GetTarget(){
    if (_targetMonster != null)
      return _targetMonster;
    if (_monsterManager != null && _monsterManager.CurrentMonster != null)
      return _monsterManager.CurrentMonster;
    return null;
  }

  /// <summary>
  /// 애니메이션 이벤트: 데미지 적용 시점에 호출.
  /// </summary>
  public void OnApplyDamage(){
    ApplyDamage();
  }

  /// <summary>
  /// 애니메이션 이벤트: 데미지 적용 시점에 호출. 씬에 있는 모든 몬스터(현재 몬스터 + 미니언 등)에게 동일 데미지 적용.
  /// </summary>
  public void OnApplyDamageAll(){
    ApplyDamageAll();
  }

  /// <summary>
  /// 애니메이션 이벤트: 프로젝타일 생성 시점에 호출.
  /// </summary>
  public void OnSpawnProjectile(){
    SpawnProjectile();
  }

  /// <summary>
  /// 애니메이션 이벤트: 클립 마지막 프레임에 호출. 종료 전달 후 삭제.
  /// </summary>
  public void OnAnimationEnd(){
    FinishEffect(_owner, _skill);
  }

  private void ApplyDamage(){
    MonsterObject target = GetTarget();
    if (_owner == null || _skill == null || target == null)
      return;
    int damage = _owner.CalculateSkillDamage(_skill);
    if (damage < 1) damage = 1;
    target.TakeDamage(damage);
    SpawnHitAnimation();
  }

  /// <summary>
  /// 씬에 있는 모든 몬스터(현재 몬스터 + 미니언)에게 각각 다른 데미지를 적용하고, 각 몬스터 위치에 히트 이펙트 생성.
  /// </summary>
  private void ApplyDamageAll(){
    if (_owner == null || _skill == null)
      return;

    MonsterObject[] monsters = Object.FindObjectsByType<MonsterObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    for (int i = 0; i < monsters.Length; i++){
      MonsterObject m = monsters[i];
      if (m == null || !m.gameObject.activeInHierarchy)
        continue;
      int damage = _owner.CalculateSkillDamage(_skill);
      if (damage < 1) damage = 1;
      m.TakeDamage(damage);
      SpawnHitAnimation(m.transform.position);
    }
  }

  private void SpawnProjectile(){
    MonsterObject target = GetTarget();
    if (projectilePrefab == null || _owner == null || _skill == null || target == null)
      return;

    Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
    Quaternion rotation = Quaternion.identity;
        
    // 타겟을 향하도록 회전
    if (faceTarget){
      Vector3 direction = (target.transform.position - spawnPos).normalized;
      if (direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }

    GameObject go = Instantiate(projectilePrefab, spawnPos, rotation);
    SkillProjectile proj = go.GetComponent<SkillProjectile>();
    if (proj != null) proj.Run(_owner, _skill, target);
  }

  private void SpawnHitAnimation(){
    Vector3 spawnPos;
    MonsterObject target = GetTarget();
    if (hitSpawnPoint != null)
      spawnPos = hitSpawnPoint.position;
    else if (target != null)
      spawnPos = target.transform.position;
    else
      spawnPos = transform.position;
    SpawnHitAnimation(spawnPos);
  }

  private void SpawnHitAnimation(Vector3 worldPosition){
    if (hitAnimationPrefab == null)
      return;
    Quaternion rotation = Quaternion.identity;
    MonsterObject target = GetTarget();
    if (faceTarget && target != null){
      Vector3 direction = (target.transform.position - worldPosition).normalized;
      if (direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }
    Instantiate(hitAnimationPrefab, worldPosition, rotation);
  }

  private void FinishEffect(UserObject owner, SkillData skill){
    if (owner != null && skill != null)
      owner.OnSkillEffectEnd(skill.skillId);
    _owner = null;
    _skill = null;
    if (gameObject != null)
      Destroy(gameObject);
  }

  private void OnDestroy(){
    if (_owner != null && _skill != null)
      _owner.OnSkillEffectEnd(_skill.skillId);
  }
}
