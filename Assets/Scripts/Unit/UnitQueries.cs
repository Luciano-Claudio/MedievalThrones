using System.Collections.Generic;
using UnityEngine;

public static class UnitQueries
{
    /// <summary>
    /// Retorna as Units do jogador. Se você tiver uma lista do seu UnitRegistry,
    /// passe em <paramref name="source"/> para evitar varrer a cena.
    /// Caso passe null, cai no fallback otimizado que usa GetByFaction.
    /// </summary>
    public static IEnumerable<Unit> GetUnitsForPlayer(PlayerController player, IEnumerable<Unit> source = null)
    {
        if (player == null) yield break;

        var faction = player.myFaction;

        // REFATORAÇÃO: Otimização - se source for null, usar diretamente GetByFaction (mais rápido)
        if (source == null)
        {
            var directList = UnitRegistry.GetByFaction(faction);
            foreach (var u in directList)
            {
                yield return u;
            }
            yield break;
        }

        // Caso contrário, filtrar source fornecido
        foreach (var u in source)
            if (u != null && u.owner == faction)
                yield return u;
    }

    /// <summary>Fallback seguro: varre a cena atrás de Units.</summary>
    public static IEnumerable<Unit> EnumerateSceneUnits()
    {
#if UNITY_2023_1_OR_NEWER
        var arr = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
#else
        var arr = Object.FindObjectsOfType<Unit>(true);
#endif
        for (int i = 0; i < arr.Length; i++)
            yield return arr[i];
    }
}
