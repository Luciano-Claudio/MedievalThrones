using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mantém estado runtime de reputação/recursos por Faction e expõe eventos.
/// Coloque este componente em um GameObject único da cena (ex.: _GameContext).
/// </summary>
public class FactionService : MonoBehaviour
{
    [SerializeField] private FactionDatabase database;

    // Reputação [A->B] (0..100). Começa em 50 neutro.
    private readonly Dictionary<(FactionId, FactionId), float> _rep = new();

    public void Init()
    {
        foreach (var fa in database.factions)
        {
            foreach (var fb in database.factions)
            {
                var key = (fa.id, fb.id);
                if (!_rep.ContainsKey(key))
                    _rep[key] = fa.initialReputation;
            }
        }
        GameEvents.RaiseReputationMatrixReady();
    }

    public float GetReputation(FactionId a, FactionId b) => _rep[(a, b)];

    public void SetReputation(FactionId a, FactionId b, float value)
    {
        value = Mathf.Clamp(value, 0, 100);
        _rep[(a, b)] = value;
        GameEvents.RaiseReputationChanged(a, b, value);
    }

    public void DeltaReputation(FactionId a, FactionId b, float delta) => SetReputation(a, b, GetReputation(a, b) + delta);
}
