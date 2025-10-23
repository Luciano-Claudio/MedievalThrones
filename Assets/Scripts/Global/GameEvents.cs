using System;
using UnityEngine;

public static class GameEvents
{
    // Tempo
    public static event Action<float> OnTimeOfDay01;          // 0..1 ao longo do dia
    public static event Action<int> OnDayChanged;           // dia inteiro (0,1,2...)
    // Economia
    public static event Action<FactionId, ResourceType, int> OnResourceGathered;
    // Diplomacia
    public static event Action OnReputationMatrixReady;
    public static event Action<FactionId, FactionId, float> OnReputationChanged;
    // Clock (dispara quando muda o minuto)
    public static event Action<int/*day*/, int/*hour*/, int/*minute*/> OnClockChanged;

    // Raise helpers
    public static void RaiseTimeOfDay(float t01) => OnTimeOfDay01?.Invoke(Mathf.Clamp01(t01));
    public static void RaiseDayChanged(int day) => OnDayChanged?.Invoke(day);
    public static void RaiseResourceGathered(FactionId who, ResourceType type, int amount) => OnResourceGathered?.Invoke(who, type, amount);
    public static void RaiseReputationMatrixReady() => OnReputationMatrixReady?.Invoke();
    public static void RaiseReputationChanged(FactionId a, FactionId b, float v) => OnReputationChanged?.Invoke(a, b, v);
    public static void RaiseClockChanged(int day, int hour, int minute) => OnClockChanged?.Invoke(day, hour, minute);

}
