// UnitHitProxy.cs
using UnityEngine;

[DisallowMultipleComponent]
public class UnitHitProxy : MonoBehaviour
{
    public Unit unit;

    void Reset() => unit = GetComponentInParent<Unit>();
}