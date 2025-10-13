using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Unit : MonoBehaviour
{
    [Header("Dados")]
    public UnitDefinition def;
    public FactionId owner = FactionId.Player1;
    public float hp = 100, hpMax = 100;

    [Header("Seleção (visuais)")]
    [SerializeField] GameObject selectionHighlight; // um “ring” ou outline

    [Header("Progressão")]
    [SerializeField] int level = 1;
    [SerializeField] float xp = 0f;

    [Tooltip("XP base para upar do Lv 1→2")]
    [SerializeField] float baseXpToLevel = 100f;

    [Tooltip("Multiplicador por nível (ex.: 1.35 => 35% a mais por nível)")]
    [SerializeField] float xpGrowth = 1.35f;

    public bool IsSelected { get; private set; }

    public event Action<Unit, bool> OnSelectionChanged;

    private void OnEnable() => UnitRegistry.Register(this);
    private void OnDisable() => UnitRegistry.Unregister(this);

    public int Level => level;
    public float Xp => xp;
    public float XpToNext
        => baseXpToLevel * Mathf.Pow(xpGrowth, Mathf.Max(0, level - 1));

    /// Fração 0..1 rumo ao próximo nível (para a barra)
    public float Xp01 => XpToNext <= 0f ? 0f : Mathf.Clamp01(xp / XpToNext);

    /// Disparado quando XP ou level mudarem (UI se inscreve)
    public event Action<Unit> OnProgressChanged;

    public void SetSelected(bool value)
    {
        if (IsSelected == value) return;
        IsSelected = value;
        if (selectionHighlight) selectionHighlight.SetActive(value);
        OnSelectionChanged?.Invoke(this, value);
    }


    // Helper para nome que a UI usa (caso você já tenha algo similar)
    public string DisplayName => def ? def.displayName : gameObject.name;

    /// Adiciona XP e lida com múltiplos ups (se ultrapassar)
    public void AddXp(float amount)
    {
        if (amount <= 0f) return;

        xp += amount;
        var guard = 64; // evita loop infinito em valores absurdos

        while (xp >= XpToNext && guard-- > 0)
        {
            xp -= XpToNext;
            level++;
        }

        OnProgressChanged?.Invoke(this);
    }


    public void TestList()
    {
        //var minhas = UnitRegistry.GetByFaction(FactionId.Player1);
        //Debug.Log($"Player1 tem {minhas.Count} unidades");

        foreach (var u in UnitRegistry.All)
            Debug.Log($" - {u.DisplayName} (dono {u.owner})");
    }
}