using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 유저(플레이어) 오브젝트. 장착 스킬 사용, 쿨타임 관리, 이펙트 종료 시 쿨 진행.
/// </summary>
public class UserObject : MonoBehaviour
{
    [Header("스킬")]
    [Tooltip("장착한 스킬 ID 목록 (Data/Skills.json의 skillId)")]
    [SerializeField] private List<string> equippedSkillIds = new List<string>();
    [SerializeField] private int defaultAttackPower = 5;

    [Header("참조")]
    [SerializeField] private MonsterManager monsterManager;
    [SerializeField] private DataManager dataManager;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Transform cooldownRoot;
    [Tooltip("스킬 아이콘 프리팹")]
    [SerializeField] private GameObject skillIconPrefab;
    [SerializeField] private SpriteRenderer profileSprite;

    [Header("채팅 버블")]
    [SerializeField] private GameObject chatBubbleRoot;
    [SerializeField] private TextMeshProUGUI chatBubbleText;

    [Header("표시 이름")]
    [SerializeField] private string displayName = "유저";
    [SerializeField] private Job job = Job.무직;

    private Dictionary<string, float> _skillCooldownRemaining = new Dictionary<string, float>();
    private HashSet<string> _skillEffectRunning = new HashSet<string>();
    private Dictionary<string, SkillCooldownUI> _skillCooldownUIs = new Dictionary<string, SkillCooldownUI>();
    private Coroutine _hideBubbleCoroutine;

    private struct SkillCooldownUI
    {
        public Image iconImage;
        public Image cooldownFillImage;
    }

    private void Start()
    {
        if (monsterManager == null)
            monsterManager = FindFirstObjectByType<MonsterManager>();
        if (dataManager == null)
            dataManager = FindFirstObjectByType<DataManager>();

        if (nameText != null)
            nameText.text = $"{displayName}";

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
        if (chatBubbleRoot != null)
            chatBubbleRoot.SetActive(false);
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

        UpdateCooldownUI();
        TryCastNextSkill();
    }

    /// <summary>표시 이름 (채팅/UI용)</summary>
    public string DisplayName => displayName;

    public void Set(NPCData npc){
        displayName = npc.name ?? "플레이어";
        if (nameText != null) nameText.text = $"{displayName}";
        
        // 스프라이트 설정: spritePath가 있으면 로드 시도, 없으면 기존 스프라이트 유지
        if (profileSprite != null)
        {
            // NPC 데이터에서 스프라이트가 로드되지 않았고 spritePath가 있으면 다시 시도
            if (npc.npcSprite == null && !string.IsNullOrEmpty(npc.spritePath))
            {
                npc.LoadSprite();
            }
            
            profileSprite.sprite = npc.npcSprite;
            
            // 스프라이트가 여전히 null이면 경고 로그
            if (npc.npcSprite == null && !string.IsNullOrEmpty(npc.spritePath))
            {
                Debug.LogWarning($"[UserObject] NPC '{displayName}'의 스프라이트를 로드할 수 없습니다. 경로: {npc.spritePath}");
            }
        }
        
        job = npc.job;
        defaultAttackPower = Mathf.Max(0, npc.attackPower);
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

    private void TryCastNextSkill()
    {
        if (dataManager == null || monsterManager == null || monsterManager.CurrentMonster == null)
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

            int damage = skill.damage > 0 ? skill.damage : defaultAttackPower;
            effect.Run(this, skill, monsterManager, damage);
            _skillEffectRunning.Add(skillId);
            break;
        }
    }

    private void BuildSkillCooldownUIs()
    {
        if (cooldownRoot == null || skillIconPrefab == null || equippedSkillIds == null)
            return;

        foreach (Transform child in cooldownRoot)
            Destroy(child.gameObject);
        _skillCooldownUIs.Clear();

        foreach (string skillId in equippedSkillIds)
        {
            if (string.IsNullOrEmpty(skillId)) continue;

            GameObject go = Instantiate(skillIconPrefab, cooldownRoot);
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
        if (chatBubbleRoot == null || chatBubbleText == null)
            return;

        if (_hideBubbleCoroutine != null)
        {
            StopCoroutine(_hideBubbleCoroutine);
            _hideBubbleCoroutine = null;
        }

        chatBubbleText.text = text;
        chatBubbleRoot.SetActive(true);
        _hideBubbleCoroutine = StartCoroutine(HideBubbleAfter(durationSeconds));
    }

    private IEnumerator HideBubbleAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        _hideBubbleCoroutine = null;
        if (chatBubbleRoot != null)
            chatBubbleRoot.SetActive(false);
    }
}
