using System;
using UnityEngine;

[DisallowMultipleComponent]
public class Unit : MonoBehaviour
{
    [Header("Dados")]
    public UnitDefinition def;
    public FactionId owner = FactionId.Player1;
    public int level = 1;
    public float hp = 100, hpMax = 100;

    [Header("Seleção (visuais)")]
    [SerializeField] GameObject selectionHighlight; // um “ring” ou outline

    public bool IsSelected { get; private set; }

    public event Action<Unit, bool> OnSelectionChanged;

    private void OnEnable() => UnitRegistry.Register(this);
    private void OnDisable() => UnitRegistry.Unregister(this);

    public void SetSelected(bool value)
    {
        if (IsSelected == value) return;
        IsSelected = value;
        if (selectionHighlight) selectionHighlight.SetActive(value);
        OnSelectionChanged?.Invoke(this, value);
    }

    public string DisplayName => def ? def.displayName : name;

    public void TestList()
    {
        //var minhas = UnitRegistry.GetByFaction(FactionId.Player1);
        //Debug.Log($"Player1 tem {minhas.Count} unidades");

        foreach (var u in UnitRegistry.All)
            Debug.Log($" - {u.DisplayName} (dono {u.owner})");
    }
}
