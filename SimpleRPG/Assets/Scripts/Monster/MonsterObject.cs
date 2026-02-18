using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 몬스터 공통: HP, 데미지, 이름·레벨·HP바 UI. 보스/일반 몬스터는 BossMonster, MinionMonster로 구분.
/// </summary>
public class MonsterObject : MonoBehaviour
{
    [Header("몬스터 데이터")]
    [SerializeField] protected MonsterData data;

    [Header("UI")]
    [SerializeField] protected TextMeshProUGUI nameText;
    [SerializeField] protected TextMeshProUGUI levelText;
    [SerializeField] protected Slider hpBar;
    [SerializeField] protected Transform damageTransform;
    [SerializeField] protected GameObject damageTextPrefab;
    protected Canvas _damageCanvas;

    protected int _currentHp;

    /// <summary>HP가 0이 되었을 때 발생</summary>
    public event Action OnDeath;
    /// <summary>HP가 변경될 때 발생 (피격 등)</summary>
    public event Action OnHpChanged;

    /// <summary>현재 HP</summary>
    public int CurrentHp => _currentHp;
    /// <summary>몬스터 데이터</summary>
    public MonsterData Data => data;

    /// <summary>
    /// 스폰 시 호출. 데이터 적용 후 UI 갱신. 서브클래스에서 오버라이드해 HUD 캔버스 등 추가 설정.
    /// </summary>
    /// <param name="monsterData">몬스터 데이터</param>
    /// <param name="damageCanvas">데미지 텍스트용 캔버스 (null이면 데미지 텍스트 미표시)</param>
    public virtual void Init(MonsterData monsterData, Canvas damageCanvas = null)
    {
        if (monsterData == null) return;

        data = monsterData;
        _currentHp = data.maxHP;
        _damageCanvas = damageCanvas;

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
        OnHpChanged?.Invoke();

        // 데미지 텍스트 팝업
        ShowDamageText(amount);

        if (_currentHp <= 0)
            OnDeath?.Invoke();
    }

    /// <summary>
    /// 데미지 텍스트 표시
    /// </summary>
    protected void ShowDamageText(int damage)
    {
        if (_damageCanvas == null || damageTextPrefab == null)
            return;

        Vector3 spawnWorldPos = (damageTransform != null) ? damageTransform.position : transform.position;

        GameObject damageTextObj = Instantiate(damageTextPrefab, _damageCanvas.transform);

        DamageText damageTextComponent = damageTextObj.GetComponent<DamageText>();
        if (damageTextComponent == null)
            damageTextComponent = damageTextObj.AddComponent<DamageText>();

        damageTextComponent.Show(damage, spawnWorldPos);
    }

    protected void UpdateUI()
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
