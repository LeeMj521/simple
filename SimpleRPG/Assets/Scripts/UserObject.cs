using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// 유저(플레이어) 오브젝트. 장착 스킬 사용, 쿨타임 관리, 이펙트 종료 시 쿨 진행.
/// </summary>
public class UserObject : MonoBehaviour
{
    [Header("유저 정보")]
    [Tooltip("유저 고유 ID")]
    [SerializeField] private string userId = "";
    [SerializeField] private string userName = "유저";
    public int attack;
    public Job job = Job.무직;
    [SerializeField] [Range(0, 100)] private int proficiency = 50;

    [Header("스킬")]
    [Tooltip("장착한 스킬 ID 목록 (Data/Skills.json의 skillId)")]
    [SerializeField] private List<string> equippedSkillIds = new List<string>();

    [Header("참조")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private DataManager dataManager;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform cooldownParent;
    [Tooltip("스킬 아이콘 프리팹")]
    [SerializeField] private GameObject skillIconPrefab;
    public SpriteRenderer profileSprite;

    [Header("채팅 버블")]
    [SerializeField] private GameObject chatBubble;
    [SerializeField] private TextMeshProUGUI chatBubbleText;

    
    
    [Header("이동")]
    [Tooltip("이동 속도 (초당 유닛)")]
    [SerializeField] private float moveSpeed = 5f;
    public float MoveSpeed => moveSpeed;
    [Tooltip("경로 찾기 그리드 (비어 있으면 자동 검색)")]
    [SerializeField] private PathfindingGrid pathfindingGrid;

    private Dictionary<string, float> _skillCooldownRemaining = new Dictionary<string, float>();
    private HashSet<string> _skillEffectRunning = new HashSet<string>();
    private Dictionary<string, SkillCooldownUI> _skillCooldownUIs = new Dictionary<string, SkillCooldownUI>();
    private Coroutine _hideBubbleCoroutine;
    private Tween _moveTween;
    private MonsterObject _targetMonster; // 수동으로 지정한 타겟 몬스터
    
    // A* 경로 찾기 관련
    private List<Vector3> _currentPath = new List<Vector3>();
    private int _currentPathIndex = 0;
    private bool _isMoving = false;

    private struct SkillCooldownUI{
        public Image iconImage;
        public Image cooldownFillImage;
    }

    private void Start()
    {
        if (monsterManager == null)
            monsterManager = FindFirstObjectByType<MonsterManager>();
        if (dataManager == null)
            dataManager = FindFirstObjectByType<DataManager>();
        if (pathfindingGrid == null)
            pathfindingGrid = FindFirstObjectByType<PathfindingGrid>();

        if (nameText != null)
            nameText.text = $"{userName}";

        _skillCooldownRemaining.Clear();
        _skillCooldownUIs.Clear();
        _skillEffectRunning.Clear();
        if (equippedSkillIds != null)
        {
            foreach (string id in equippedSkillIds)
            {
                if (!string.IsNullOrEmpty(id) && !_skillCooldownRemaining.ContainsKey(id))
                    _skillCooldownRemaining[id] = 0f;
            }
        }
        BuildSkillCooldownUIs();
        if (chatBubble != null)
            chatBubble.SetActive(false);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (equippedSkillIds != null)
        {
            foreach (string skillId in equippedSkillIds)
            {
                if (string.IsNullOrEmpty(skillId)) continue;
                if (_skillEffectRunning.Contains(skillId)) continue;
                if (_skillCooldownRemaining.TryGetValue(skillId, out float remaining))
                {
                    remaining -= dt;
                    if (remaining < 0f) remaining = 0f;
                    _skillCooldownRemaining[skillId] = remaining;
                }
            }
        }

        // 타겟이 사라졌거나 비활성화되었으면 타겟 해제
        if (_targetMonster != null && (!_targetMonster.gameObject.activeInHierarchy || _targetMonster.CurrentHp <= 0))
        {
            _targetMonster = null;
        }

        UpdateCooldownUI();
        TryCastNextSkill();
    }

    public string UserId => userId;
    public string UserName => userName;
    public MonsterObject TargetMonster => _targetMonster;

    /// <summary>
    /// 유저 식별자/이름 설정 (NPC 스폰, 플레이어 초기화 등에서 사용)
    /// </summary>
    public void SetIdentity(string id, string name)
    {
        if (!string.IsNullOrWhiteSpace(id))
            userId = id.Trim();
        if (!string.IsNullOrWhiteSpace(name))
            userName = name.Trim();

        if (nameText != null)
            nameText.text = userName;
    }

    /// <summary>
    /// 타겟 몬스터 설정 (좌클릭으로 적 선택 시 호출)
    /// </summary>
    public void SetTargetMonster(MonsterObject target)
    {
        _targetMonster = target;
    }

    public void Set(NPCData npc){
        if (npc == null)
        {
            SetIdentity(userId, string.IsNullOrWhiteSpace(userName) ? "플레이어" : userName);
            return;
        }

        SetIdentity(npc.npcId, npc.name ?? "플레이어");
        
        // 스프라이트 설정: spritePath가 있으면 로드 시도, 없으면 기존 스프라이트 유지
        if (profileSprite != null)
        {
            // NPC 데이터에서 스프라이트가 로드되지 않았고 spritePath가 있으면 다시 시도
            if (npc.npcSprite == null && !string.IsNullOrEmpty(npc.spritePath))
            {
                npc.LoadSprite();
            }
            
            // 스프라이트가 성공적으로 로드된 경우에만 설정 (null이면 기존 스프라이트 유지)
            if (npc.npcSprite != null)
            {
                profileSprite.sprite = npc.npcSprite;
            }
            else if (!string.IsNullOrEmpty(npc.spritePath))
            {
                // 스프라이트 로드 실패 시 경고 로그만 출력 (스프라이트는 변경하지 않음)
                Debug.LogWarning($"[UserObject] NPC '{userName}'의 스프라이트를 로드할 수 없습니다. 경로: {npc.spritePath}. 기존 스프라이트를 유지합니다.");
            }
        }
        
        job = npc.job;
        attack = Mathf.Max(0, npc.attackPower);
        if (npc.equippedSkillIds != null && npc.equippedSkillIds.Count > 0)
            SetEquippedSkills(npc.equippedSkillIds);
    }

    /// <summary>장착 스킬 ID 목록 설정</summary>
    public void SetEquippedSkills(IList<string> skillIds)
    {
        equippedSkillIds.Clear();
        if (skillIds != null)
        {
            foreach (string id in skillIds)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    equippedSkillIds.Add(id);
                    if (!_skillCooldownRemaining.ContainsKey(id))
                        _skillCooldownRemaining[id] = 0f;
                }
            }
        }
        BuildSkillCooldownUIs();
    }

    /// <summary>
    /// 스킬 데미지 계산. 호출 시마다 숙련도 랜덤이 적용되어 여러 타격 시 각각 다른 데미지.
    /// </summary>
    public int CalculateSkillDamage(SkillData skill)
    {
        if (skill == null) return 1;
        float attackMultiplier = skill.damage > 0 ? skill.damage / 100.0f : 1.0f;
        float baseDamage = attack * attackMultiplier;
        float minRatio = Mathf.Clamp01(proficiency / 100f);
        float randomMultiplier = Random.Range(minRatio, 1f);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * randomMultiplier));
    }

    private void TryCastNextSkill()
    {
        if (dataManager == null || monsterManager == null)
            return;
        
        // 타겟이 없으면 스킬 시전하지 않음 (수동 타겟 지정 모드)
        MonsterObject target = _targetMonster != null ? _targetMonster : monsterManager.CurrentMonster;
        if (target == null)
            return;
        
        if (equippedSkillIds == null) return;

        foreach (string skillId in equippedSkillIds)
        {
            if (string.IsNullOrEmpty(skillId)) continue;
            if (_skillEffectRunning.Contains(skillId)) continue;
            if (!_skillCooldownRemaining.TryGetValue(skillId, out float remaining) || remaining > 0f)
                continue;

            SkillData skill = dataManager.GetSkill(skillId);
            if (skill == null || skill.prefab == null) continue;

            GameObject effectGo = Instantiate(skill.prefab, transform.position, Quaternion.identity);
            SkillEffect effect = effectGo.GetComponent<SkillEffect>();
            if (effect == null) effect = effectGo.GetComponentInChildren<SkillEffect>();
            if (effect == null)
            {
                Debug.LogWarning($"[UserObject] 스킬 프리팹에 SkillEffect가 없습니다: {skillId}");
                Destroy(effectGo);
                continue;
            }

            effect.Run(this, skill, monsterManager, target);
            _skillEffectRunning.Add(skillId);
            break;
        }
    }

    private void BuildSkillCooldownUIs()
    {
        if (cooldownParent == null || skillIconPrefab == null || equippedSkillIds == null)
            return;

        foreach (Transform child in cooldownParent)
            Destroy(child.gameObject);
        _skillCooldownUIs.Clear();

        foreach (string skillId in equippedSkillIds)
        {
            if (string.IsNullOrEmpty(skillId)) continue;

            GameObject go = Instantiate(skillIconPrefab, cooldownParent);
            go.name = $"skill_icon_{skillId}";

            Image iconImage = go.GetComponent<Image>();
            GameObject cooldownFill = go.transform.GetChild(0).gameObject;
            Image cooldownFillImage = cooldownFill.GetComponent<Image>();

            if (iconImage != null && dataManager != null)
            {
                SkillData skill = dataManager.GetSkill(skillId);
                if (skill != null && !string.IsNullOrEmpty(skill.iconPath))
                {
                    Sprite sprite = Resources.Load<Sprite>(skill.iconPath);
                    if (sprite != null)
                        iconImage.sprite = sprite;
                }
            }

            if (cooldownFillImage != null)
            {
                cooldownFillImage.type = Image.Type.Filled;
                cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
                cooldownFillImage.fillOrigin = (int)Image.Origin360.Top;
                cooldownFillImage.fillClockwise = false;
                cooldownFillImage.fillAmount = 0f;
            }

            _skillCooldownUIs[skillId] = new SkillCooldownUI { iconImage = iconImage, cooldownFillImage = cooldownFillImage };
        }
    }

    private void UpdateCooldownUI()
    {
        if (equippedSkillIds == null) return;
        foreach (string skillId in equippedSkillIds)
        {
            if (string.IsNullOrEmpty(skillId) || !_skillCooldownUIs.TryGetValue(skillId, out SkillCooldownUI ui))
                continue;
            if (ui.cooldownFillImage == null) continue;

            float remaining = _skillCooldownRemaining.TryGetValue(skillId, out float r) ? r : 0f;
            SkillData skill = dataManager != null ? dataManager.GetSkill(skillId) : null;
            float total = skill != null ? skill.cooldown : 1f;
            ui.cooldownFillImage.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
        }
    }

    /// <summary>
    /// 스킬 이펙트가 종료될 때 호출. 해당 스킬 쿨타임을 시작한다.
    /// </summary>
    public void OnSkillEffectEnd(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return;
        _skillEffectRunning.Remove(skillId);
        SkillData skill = dataManager != null ? dataManager.GetSkill(skillId) : null;
        float cooldown = skill != null ? skill.cooldown : 1f;
        _skillCooldownRemaining[skillId] = cooldown;
    }

    /// <summary>해당 유저 머리 위에 채팅 버블 표시. 일정 시간 후 자동 숨김.</summary>
    public void ShowChatBubble(string text, float durationSeconds)
    {
        if (chatBubble == null || chatBubbleText == null)
            return;

        if (_hideBubbleCoroutine != null)
        {
            StopCoroutine(_hideBubbleCoroutine);
            _hideBubbleCoroutine = null;
        }

        chatBubbleText.text = text;
        chatBubble.SetActive(true);
        _hideBubbleCoroutine = StartCoroutine(HideBubbleAfter(durationSeconds));
    }

    private IEnumerator HideBubbleAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _hideBubbleCoroutine = null;
        if (chatBubble != null)
            chatBubble.SetActive(false);
    }
    
    /// <summary>
    /// 지정된 로컬 위치로 이동 (A* 경로 찾기 사용)
    /// </summary>
    public void MoveToPosition(Vector3 targetLocalPos)
    {
        Vector3 worldTarget = transform.parent != null ? transform.parent.TransformPoint(targetLocalPos) : targetLocalPos;
        MoveToWorldPosition(worldTarget);
    }

    /// <summary>
    /// 지정된 월드 위치로 이동 (A* 경로 찾기 사용, 한 칸씩 이동)
    /// </summary>
    public void MoveToWorldPosition(Vector3 targetWorldPos)
    {
        if (_moveTween != null && _moveTween.IsActive())
            _moveTween.Kill();

        _currentPath.Clear();
        _currentPathIndex = 0;
        _isMoving = false;

        // 경로 찾기 그리드가 없으면 직접 이동
        if (pathfindingGrid == null)
        {
            float distance = Vector3.Distance(transform.position, targetWorldPos);
            float duration = moveSpeed > 0f ? distance / moveSpeed : 0f;
            _moveTween = transform.DOMove(targetWorldPos, duration)
                .SetEase(Ease.OutQuad);
            return;
        }

        // A* 경로 찾기
        _currentPath = pathfindingGrid.FindPath(transform.position, targetWorldPos);
        
        if (_currentPath == null || _currentPath.Count == 0)
        {
            // 경로를 찾지 못하면 직접 이동
            float distance = Vector3.Distance(transform.position, targetWorldPos);
            float duration = moveSpeed > 0f ? distance / moveSpeed : 0f;
            _moveTween = transform.DOMove(targetWorldPos, duration)
                .SetEase(Ease.OutQuad);
            return;
        }

        // 첫 번째 목표 지점으로 이동 시작
        _isMoving = true;
        MoveToNextPathPoint();
    }

    /// <summary>
    /// 경로의 다음 지점으로 이동
    /// </summary>
    private void MoveToNextPathPoint()
    {
        if (_currentPath == null || _currentPathIndex >= _currentPath.Count)
        {
            _isMoving = false;
            return;
        }

        Vector3 targetPos = _currentPath[_currentPathIndex];
        float distance = Vector3.Distance(transform.position, targetPos);
        float duration = moveSpeed > 0f ? distance / moveSpeed : 0f;

        _moveTween = transform.DOMove(targetPos, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                _currentPathIndex++;
                if (_currentPathIndex < _currentPath.Count)
                {
                    // 다음 지점으로 이동
                    MoveToNextPathPoint();
                }
                else
                {
                    // 경로 완료
                    _isMoving = false;
                }
            });
    }
    
    private void OnDestroy()
    {
        if (_moveTween != null && _moveTween.IsActive())
            _moveTween.Kill();
    }
}
