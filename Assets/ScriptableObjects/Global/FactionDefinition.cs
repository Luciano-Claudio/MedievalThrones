using UnityEngine;

[CreateAssetMenu(fileName = "Faction", menuName = "Game/Faction")]
public class FactionDefinition : ScriptableObject
{
    public FactionId id;
    public string displayName = "Reino";
    public Color color = Color.white;
    public Sprite banner;
    [Range(0, 100)] public float initialReputation = 50f; // contra outros (neutro)
}
