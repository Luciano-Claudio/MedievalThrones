using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    public string displayName;
    public UnitType type;
    public Sprite icon;
}
