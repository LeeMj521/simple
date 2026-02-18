using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 씬에 있는 보스 HUD(이름·레벨·HP바)를 현재 보스 몬스터와 연결한다.
/// Boss_HUD_Canvas에 붙이고, Inspector에서 name_Text / level_Text / hp_bar_Slider를 할당한 뒤
/// MonsterManager가 보스 스폰 시 Bind(보스)를 호출하면 된다.
/// </summary>
public class BossHUD : MonoBehaviour
{
    [Header("HUD UI (Boss_HUD_Canvas 내부 오브젝트 할당)")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider hpBar;

    private MonsterObject _boundMonster;

    /// <summary>현재 바인딩된 몬스터 (없으면 null)</summary>
    public MonsterObject BoundMonster => _boundMonster;

    /// <summary>
    /// 보스 몬스터를 이 HUD에 연결한다. 이름·레벨·HP가 갱신되며, 피격 시 HP바도 갱신된다.
    /// </summary>
    public void Bind(MonsterObject monster)
    {
        Unbind();

        if (monster == null) return;

        _boundMonster = monster;
        _boundMonster.OnHpChanged += RefreshUI;
        _boundMonster.OnDeath += HandleBoundMonsterDeath;

        RefreshUI();
    }

    /// <summary>
    /// HUD와 몬스터 연결을 해제한다.
    /// </summary>
    public void Unbind()
    {
        if (_boundMonster == null) return;

        _boundMonster.OnHpChanged -= RefreshUI;
        _boundMonster.OnDeath -= HandleBoundMonsterDeath;
        _boundMonster = null;

        ClearUI();
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void HandleBoundMonsterDeath()
    {
        Unbind();
    }

    private void RefreshUI()
    {
        if (_boundMonster == null || _boundMonster.Data == null) return;

        MonsterData d = _boundMonster.Data;
        if (nameText != null)
            nameText.text = d.monsterName;
        if (levelText != null)
            levelText.text = "Lv." + d.level;
        if (hpBar != null)
        {
            hpBar.minValue = 0f;
            hpBar.maxValue = d.maxHP;
            hpBar.value = _boundMonster.CurrentHp;
        }
    }

    private void ClearUI()
    {
        if (nameText != null)
            nameText.text = "";
        if (levelText != null)
            levelText.text = "";
        if (hpBar != null)
        {
            hpBar.minValue = 0f;
            hpBar.maxValue = 1f;
            hpBar.value = 0f;
        }
    }
}
