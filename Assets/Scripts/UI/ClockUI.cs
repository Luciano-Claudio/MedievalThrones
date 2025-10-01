using TMPro;
using UnityEngine;

public class ClockUI : MonoBehaviour
{
    public TimeManager timeManager;
    public TMP_Text clockText;
    public TMP_Text dayText;

    void OnEnable() => GameEvents.OnClockChanged += UpdateClock;
    void OnDisable() => GameEvents.OnClockChanged -= UpdateClock;

    private int day, hour, minute;

    void Start() => UpdateClock(timeManager.DayCount, timeManager.Hour, timeManager.Minute);

    private void Update()
    {

        clockText.text = $" {hour:00}:{minute:00}";
        dayText.text = $"Day {day}";

    }

    public void UpdateClock(int day, int hour, int minute)
    {
        this.day = day;
        this.hour = hour;
        this.minute = minute;
    }
}
