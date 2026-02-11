using System.Collections;
using UnityEngine;

/// <summary>
/// 스킬 이펙트. 프리팹 생성 → 애니메이션 재생 → 애니메이션 이벤트로 데미지/프로젝타일 호출 → 종료 이벤트 시 쿨타임 알림 후 삭제.
/// </summary>
public class SkillEffect : MonoBehaviour
{
  [Header("애니메이션 이벤트 (Function: OnApplyDamage / OnSpawnProjectile / OnSpawnHitAnimation / OnAnimationEnd)")]

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
  private int _damage;
  private bool _finished;

  void Update(){
    if(_owner == null){
      FinishEffect(null, _skill);
      return;
    }
    transform.position = _owner.transform.position;
    if(faceTarget && _monsterManager != null && _monsterManager.CurrentMonster != null){
      Vector3 direction = (_monsterManager.CurrentMonster.transform.position - transform.position).normalized;
      if(direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }
  }

  /// <summary>
  /// 이펙트 실행. 애니메이션 재생 후 이벤트로 OnApplyDamage / OnSpawnProjectile / OnAnimationEnd 호출.
  /// </summary>
  public void Run(UserObject owner, SkillData skill, MonsterManager monsterManager, int damage){
    if (owner == null || skill == null){
      FinishEffect(owner, skill);
      return;
    }

    _owner = owner;
    _skill = skill;
    _monsterManager = monsterManager;
    _damage = Mathf.Max(0, damage);

    // 타겟을 향하도록 회전
    if (faceTarget && _monsterManager != null && _monsterManager.CurrentMonster != null){
      Vector3 direction = (_monsterManager.CurrentMonster.transform.position - transform.position).normalized;
      if (direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }
  }

  /// <summary>
  /// 애니메이션 이벤트: 데미지 적용 시점에 호출.
  /// </summary>
  public void OnApplyDamage(){
    ApplyDamage();
    SpawnHitAnimation();
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
    if (_monsterManager != null && _monsterManager.CurrentMonster != null && _damage > 0)
      _monsterManager.CurrentMonster.TakeDamage(_damage);
  }

  private void SpawnProjectile(){
    if (projectilePrefab == null || _monsterManager == null || _monsterManager.CurrentMonster == null || _damage <= 0)
      return;

    Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
    Quaternion rotation = Quaternion.identity;
        
    // 타겟을 향하도록 회전
    if (faceTarget){
      Vector3 direction = (_monsterManager.CurrentMonster.transform.position - spawnPos).normalized;
      if (direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }

    GameObject go = Instantiate(projectilePrefab, spawnPos, rotation);
    SkillProjectile proj = go.GetComponent<SkillProjectile>();
    if (proj != null) proj.Run(_damage, _monsterManager.CurrentMonster);
  }

  private void SpawnHitAnimation(){
    if (hitAnimationPrefab == null)
      return;

    Vector3 spawnPos;
    if (hitSpawnPoint != null){
      spawnPos = hitSpawnPoint.position;
    }
    else if (_monsterManager != null && _monsterManager.CurrentMonster != null){
      spawnPos = _monsterManager.CurrentMonster.transform.position;
    }
    else{
      spawnPos = transform.position;
    }

    Quaternion rotation = Quaternion.identity;

    // 타겟을 향하도록 회전
    if (faceTarget && _monsterManager != null && _monsterManager.CurrentMonster != null){
      Vector3 direction = (_monsterManager.CurrentMonster.transform.position - spawnPos).normalized;
      if (direction != Vector3.zero){
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rotation = Quaternion.AngleAxis(angle, Vector3.forward);
      }
    }

    Instantiate(hitAnimationPrefab, spawnPos, rotation);
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
