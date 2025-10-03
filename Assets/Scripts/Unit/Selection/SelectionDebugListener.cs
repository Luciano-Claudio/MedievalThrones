// SelectionDebugListener.cs
using UnityEngine;

public class SelectionDebugListener : MonoBehaviour
{
    public InputSelection input;

    void OnEnable()
    {
        input.OnClickUnit += (u, c, s) => Debug.Log($"Click Unit: {u.DisplayName} ctrl:{c} shift:{s}");
        input.OnDoubleClickUnit += (u) => Debug.Log($"DoubleClick Unit: {u.DisplayName}");
        input.OnClickGround += (p, c, s) => Debug.Log($"Click Ground: {p} ctrl:{c} shift:{s}");
        input.OnBeginDrag += p => Debug.Log($"BeginDrag {p}");
        input.OnEndDrag += p => Debug.Log($"EndDrag {p}");
    }
}
