using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactionDatabase", menuName = "Game/Faction Database")]
public class FactionDatabase : ScriptableObject
{
    public List<FactionDefinition> factions = new();
    public FactionDefinition Get(FactionId id) => factions.Find(f => f.id == id);
}
