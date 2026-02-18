using UnityEngine;

/// <summary>
/// 보스 행동 패턴: 대기. 현재는 아무 동작 없음. 다른 패턴과 조합해 타이밍 제어용으로 사용 가능.
/// </summary>
public class BossBehaviorIdle : BossBehaviorBase
{
    [Tooltip("이 시간(초) 동안 대기 후 비활성화할 때 사용 등")]
    [SerializeField] private float durationSeconds;

    private float _endTime;

    private void OnEnable()
    {
        _endTime = Time.time + durationSeconds;
    }

    private void Update()
    {
        if (Boss == null) return;
        if (durationSeconds > 0f && Time.time >= _endTime)
            enabled = false;
    }
}
