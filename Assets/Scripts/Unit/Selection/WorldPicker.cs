using UnityEngine;

public class WorldPicker : MonoBehaviour
{
    public Camera cam;
    public LayerMask unitMask;   // Unit
    public LayerMask groundMask; // Ground
    public float maxDistance = 500f;

    void Reset() { cam = Camera.main; }

    public bool TryPickUnitAt(Vector2 screenPos, out Unit unit)
    {
        unit = null;
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, maxDistance, unitMask, QueryTriggerInteraction.Collide))
        {
            unit = hit.collider.GetComponentInParent<Unit>();
            return unit != null;
        }
        return false;
    }

    public bool TryPickGroundAt(Vector2 screenPos, out Vector3 point, out Vector3 normal)
    {
        point = default; normal = Vector3.up;
        var ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out var hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point; normal = hit.normal;
            return true;
        }
        return false;
    }
}
