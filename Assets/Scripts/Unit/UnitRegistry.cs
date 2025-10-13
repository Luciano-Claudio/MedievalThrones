using System;
using System.Collections.Generic;
using UnityEngine;

public static class UnitRegistry
{
    public static event Action<Unit> OnUnitSpawned;
    public static event Action<Unit> OnUnitDespawned;

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
            OnUnitSpawned?.Invoke(u);
        }
    }

    public static void Unregister(Unit u)
    {
        if (_all.Remove(u))
        {
            if (_perFaction.TryGetValue(u.owner, out var list)) list.Remove(u);
            OnUnitDespawned?.Invoke(u);
        }
    }

}