using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/Config")]
public class GameConfig : ScriptableObject
{
    [Header("Tempo")]
    [Tooltip("Duração de 1 dia do jogo (em segundos reais). Demo: 600s = 10min")]
    public float secondsPerDay = 600f;
    [Range(0.1f, 0.9f)] public float dayFraction = 0.5f;   // 50% dia / 50% noite

    [Header("Clima/Modificadores (demo - provisório)")]
    [Tooltip("Penalidade de visão à noite (ex.: 0.2 = -20%)")]
    [Range(0f, 1f)] public float nightVisionPenalty = 0.20f;
    [Range(0f, 1f)] public float fogVisionPenalty = 0.10f;
    [Range(0f, 1f)] public float mudSandMovePenalty = 0.20f;
    [Range(0f, 1f)] public float snowExtraUpkeep = 0.15f;

    [Header("Economia — baseline (demo)")]
    [Tooltip("Em cenário ideal (depósito ~3 hex), um operário colhe 10 a cada 3 min.")]
    public float baselineGatherMinTotal = 3;
    public int baselineGatherPer3Min = 10;
    [Tooltip("Capacidade do operário por viagem.")]
    public int workerCarryCapacity = 10;

    [Header("Unidades especiais (demo)")]
    public float merchantSpeedHexPerSec = 0.5f;
    public int merchantCapacity = 20;
    public int merchantCostGold = 30;

    [Header("Reparos")]
    public float structureRepairHpPerSec = 0.5f;

    // Helpers
    public float BaselinePerSecond => baselineGatherPer3Min / (baselineGatherMinTotal * 60); // 3 min = 180s
    public float SecondsPerDay => secondsPerDay;
}
