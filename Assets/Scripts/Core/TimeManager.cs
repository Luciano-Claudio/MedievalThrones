using UnityEngine;

/// <summary> Avança o relógio global e emite eventos de tempo. </summary>
public class TimeManager : MonoBehaviour
{
    public GameConfig config;
    [Range(0f, 1f)] public float startTime01 = 0.25f; // 0 = amanhecer, 0.5 = pôr-do-sol (ajuste livre)
    
    [field: SerializeField]
    public float Time01 { get; private set; }
    [field: SerializeField]
    public int DayCount { get; private set; }
    [field: SerializeField]
    public int Hour { get; private set; } 
    [field: SerializeField]
    public int Minute { get; private set; }

    private int _lastMinute = -1; // para disparar evento só quando muda

    void Awake()
    {
        Time01 = Mathf.Repeat(startTime01, 1f);
        GameEvents.RaiseTimeOfDay(Time01);
    }

    void Update()
    {
        if (config == null) return;
        var delta01 = Time.deltaTime / Mathf.Max(1f, config.SecondsPerDay);
        var old = Time01;

        Time01 = Mathf.Repeat(Time01 + delta01, 1f);
        if (Time01 < old) // virou o dia
        {
            DayCount++;
            GameEvents.RaiseDayChanged(DayCount);
        }
        // --- NOVO: converte fração do dia em HH:MM (24h) ---
        // totalMinutes = fração * 1440 (24 * 60)
        int totalMinutes = Mathf.FloorToInt(Time01 * 1440f);
        Hour = (totalMinutes / 60) % 24;
        Minute = totalMinutes % 60;

        if (Minute != _lastMinute) // dispara no "tic" do minuto
        {
            _lastMinute = Minute;
            GameEvents.RaiseClockChanged(DayCount, Hour, Minute);
        }
        GameEvents.RaiseTimeOfDay(Time01);
    }

    public bool IsNight()
    {
        // Ex.: se dayFraction=0.5, noite é [0.5,1)
        return Time01 >= config.dayFraction;
    }
}
