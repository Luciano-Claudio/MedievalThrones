using TMPro;
using UnityEngine;

public class ClockUI : MonoBehaviour
{
    public TimeManager timeManager;
    public TMP_Text clockText;

    void OnEnable() => GameEvents.OnClockChanged += UpdateClock;
    void OnDisable() => GameEvents.OnClockChanged -= UpdateClock;

    void Start() => UpdateClock(timeManager.DayCount, timeManager.Hour, timeManager.Minute);

    void UpdateClock(int day, int hour, int minute)
    {
        clockText.text = $"Dia {day}  {hour:00}:{minute:00}";
    }
}
