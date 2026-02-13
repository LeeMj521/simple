using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 몬스터 프리팹에 붙이는 스크립트. HP바, 레벨, 이름 관리. 캔버스는 프리팹 내부에 있음.
/// </summary>
public class MonsterObject : MonoBehaviour
{
    [Header("몬스터 데이터")]
    [SerializeField] private MonsterData data;

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider hpBar;
    [Tooltip("데미지 텍스트 프리팹")]
    [SerializeField] private GameObject damageTextPrefab;

    private int _currentHp;

    /// <summary>HP가 0이 되었을 때 발생</summary>
    public event Action OnDeath;

    /// <summary>현재 HP</summary>
    public int CurrentHp => _currentHp;
    /// <summary>몬스터 데이터</summary>
    public MonsterData Data => data;

    /// <summary>
    /// MonsterManager가 스폰 시 호출. 데이터 적용 후 UI 갱신.
    /// </summary>
    public void Init(MonsterData monsterData)
    {
        if (monsterData == null) return;

        data = monsterData;
        _currentHp = data.maxHP;

        if (hpBar != null)
        {
            hpBar.minValue = 0f;
            hpBar.maxValue = data.maxHP;
            hpBar.value = _currentHp;
        }

        UpdateUI();
    }

    /// <summary>
    /// 피격 처리. HP가 0 이하가 되면 OnDeath 호출.
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (data == null || amount <= 0) return;

        _currentHp = Mathf.Max(0, _currentHp - amount);
        UpdateUI();

        // 데미지 텍스트 팝업
        ShowDamageText(amount);

        if (_currentHp <= 0)
            OnDeath?.Invoke();
    }

    /// <summary>
    /// 데미지 텍스트 표시
    /// </summary>
    private void ShowDamageText(int damage)
    {
        if (canvas == null || damageTextPrefab == null)
            return;

        GameObject damageTextObj = Instantiate(damageTextPrefab, canvas.transform);

        DamageText damageTextComponent = damageTextObj.GetComponent<DamageText>();
        if (damageTextComponent == null)
            damageTextComponent = damageTextObj.AddComponent<DamageText>();

        // 몬스터 위치에서 약간 위로 오프셋
        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        damageTextComponent.Show(damage, spawnPos);
    }

    private void UpdateUI()
    {
        if (data == null) return;

        if (nameText != null)
            nameText.text = data.monsterName;

        if (levelText != null)
            levelText.text = "Lv." + data.level;

        if (hpBar != null)
            hpBar.value = _currentHp;
    }
}
