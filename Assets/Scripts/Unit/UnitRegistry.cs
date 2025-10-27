using System;
using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry
{
    // REFATORAÇÃO: Eventos estáticos removidos, agora usa GameEvents
    // public static event Action<Unit> OnUnitSpawned;    // REMOVIDO
    // public static event Action<Unit> OnUnitDespawned;  // REMOVIDO

    static readonly List<Unit> _all = new();
    static readonly Dictionary<FactionId, List<Unit>> _perFaction = new();

    public static IReadOnlyList<Unit> All => _all;
    public static IReadOnlyList<Unit> GetByFaction(FactionId f) =>
        _perFaction.TryGetValue(f, out var list) ? list : Array.Empty<Unit>();

    public static void Register(Unit u)
    {
        if (!_all.Contains(u))
        {
            _all.Add(u);
            if (!_perFaction.TryGetValue(u.owner, out var list))
            {
                list = new List<Unit>();
                _perFaction[u.owner] = list;
            }
            list.Add(u);

            // REFATORAÇÃO: Usar GameEvents em vez de evento estático local
            GameEvents.RaiseUnitSpawned(u);
        }
    }

    public static void Unregister(Unit u)
    {
        if (_all.Remove(u))
        {
            if (_perFaction.TryGetValue(u.owner, out var list)) list.Remove(u);

            // REFATORAÇÃO: Usar GameEvents em vez de evento estático local
            GameEvents.RaiseUnitDespawned(u);
        }
    }
}
