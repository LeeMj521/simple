using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 내 시간. 현실보다 빠르게 흐르며, NPC 접속 스케줄 등에 사용.
/// 시계 UI(시간/일수)도 여기서 표시.
/// </summary>
public class GameTimeManager : MonoBehaviour
{
    [Header("게임 시간 속도")]
    [Tooltip("실제 1초당 흐르는 게임 내 분 (예: 60 = 1실제초당 1게임시간)")]
    [SerializeField] private float gameMinutesPerRealSecond = 60f;

    [Header("시계 UI")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dayText;
    [Tooltip("24시간 형식. false면 12시간 AM/PM")]
    [SerializeField] private bool use24Hour = true;

    private double _totalGameMinutes;

    /// <summary>자정(0:00) 기준 경과한 게임 내 분 (누적)</summary>
    public double TotalGameMinutes => _totalGameMinutes;

    /// <summary>오늘 자정 기준 경과 분 (0~1440, 1440=24시)</summary>
    public int TimeOfDayMinutes => (int)(_totalGameMinutes % 1440) % 1440;

    /// <summary>경과한 게임 일수 (0부터)</summary>
    public int GameDay => (int)(_totalGameMinutes / 1440);

    /// <summary>현재 게임 시 (0~23)</summary>
    public int Hour => TimeOfDayMinutes / 60;

    /// <summary>현재 게임 분 (0~59)</summary>
    public int Minute => TimeOfDayMinutes % 60;

    private void Update()
    {
        _totalGameMinutes += Time.deltaTime * gameMinutesPerRealSecond;

        if (timeText != null)
            timeText.text = use24Hour ? GetTimeString() : GetTimeString12();
        if (dayText != null)
            dayText.text = "Day " + (GameDay + 1);
    }

    /// <summary>게임 시간을 "HH:MM" 형식으로 반환</summary>
    public string GetTimeString()
    {
        return $"{Hour:D2}:{Minute:D2}";
    }

    private string GetTimeString12()
    {
        int h = Hour;
        int m = Minute;
        bool pm = h >= 12;
        if (h == 0) h = 12;
        else if (h > 12) h -= 12;
        return $"{h:D2}:{m:D2} {(pm ? "PM" : "AM")}";
    }
}
