using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 유저(플레이어) 오브젝트. 몬스터 공격, 이름 표시, 쿨다운 바, 채팅 버블.
/// </summary>
public class UserObject : MonoBehaviour
{
    [Header("공격")]
    [SerializeField] private int attackPower = 5;
    [SerializeField] private float attackCooldown = 1f;

    [Header("참조")]
    [SerializeField] private MonsterManager monsterManager;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Slider cooldownBar;

    [Header("채팅 버블")]
    [SerializeField] private GameObject chatBubbleRoot;
    [SerializeField] private TextMeshProUGUI chatBubbleText;

    [Header("표시 이름")]
    [SerializeField] private string displayName = "유저";
    [SerializeField] private Job job = Job.무직;

    private float _cooldownRemaining;
    private Coroutine _hideBubbleCoroutine;

    private void Start()
    {
        if (monsterManager == null)
            monsterManager = FindFirstObjectByType<MonsterManager>();

        if (nameText != null)
            nameText.text = $"{displayName}";

        if (cooldownBar != null)
        {
            cooldownBar.minValue = 0f;
            cooldownBar.maxValue = 1f;
            cooldownBar.value = 1f;
        }

        _cooldownRemaining = 0f;
        if (chatBubbleRoot != null)
            chatBubbleRoot.SetActive(false);
    }

    private void Update()
    {
        if (_cooldownRemaining > 0f)
        {
            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining < 0f) _cooldownRemaining = 0f;
        }

        UpdateCooldownUI();

        if (_cooldownRemaining <= 0f && monsterManager != null && monsterManager.CurrentMonster != null)
        {
            monsterManager.CurrentMonster.TakeDamage(attackPower);
            _cooldownRemaining = attackCooldown;
        }
    }

    private void UpdateCooldownUI()
    {
        if (cooldownBar == null) return;
        // 쿨다운 진행률: 0 = 대기 중, 1 = 사용 가능
        cooldownBar.value = attackCooldown > 0f
            ? 1f - (_cooldownRemaining / attackCooldown)
            : 1f;
    }

    /// <summary>표시 이름 (채팅/UI용)</summary>
    public string DisplayName => displayName;

    /// <summary>표시 이름 설정 (채팅/UI용)</summary>
    public void SetDisplayName(string name)
    {
        displayName = name ?? "플레이어";
        if (nameText != null) nameText.text = $"{displayName}";
    }

    /// <summary>직업 설정. 이름 옆에 [직업] 표시용.</summary>
    public void SetJob(Job jobType)
    {
        job = jobType;
    }

    /// <summary>공격력/쿨타임 설정</summary>
    public void SetAttack(int power, float cooldown)
    {
        attackPower = Mathf.Max(0, power);
        attackCooldown = Mathf.Max(0.1f, cooldown);
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
