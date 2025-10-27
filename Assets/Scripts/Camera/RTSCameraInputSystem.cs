using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RTSCameraCinemachineV3Controller))]
public class RTSCameraInputSystem : MonoBehaviour
{
    [Header("Action References (arraste do .inputactions)")]
    public InputActionReference move;            // Vector2 (WASD - 2D Vector)
    public InputActionReference rotate;          // 1D Axis (Q/E)
    public InputActionReference zoom;            // Axis (Mouse scroll Y)
    public InputActionReference pointerPos;      // Vector2 (Mouse position)
    public InputActionReference pointerDelta;    // Vector2 (Mouse delta)
    public InputActionReference middleButton;    // Button (Mouse middle)

    RTSCameraCinemachineV3Controller _cam;
    bool _middleHeld;

    void Awake()
    {
        _cam = GetComponent<RTSCameraCinemachineV3Controller>();
    }

    void OnEnable()
    {
        Enable(move);
        Enable(rotate);
        Enable(zoom);
        Enable(pointerPos);
        Enable(pointerDelta);
        Enable(middleButton);

        if (middleButton) middleButton.action.performed += OnMiddle;
        if (middleButton) middleButton.action.canceled += OnMiddle;
    }

    void OnDisable()
    {
        if (middleButton) middleButton.action.performed -= OnMiddle;
        if (middleButton) middleButton.action.canceled -= OnMiddle;

        Disable(move);
        Disable(rotate);
        Disable(zoom);
        Disable(pointerPos);
        Disable(pointerDelta);
        Disable(middleButton);
    }

    void OnMiddle(InputAction.CallbackContext ctx)
    {
        _middleHeld = ctx.ReadValueAsButton();
    }

    void Update()
    {
        Vector2 wasd = Read(move);
        float rot = Read(rotate, true);
        float zm = Read(zoom, true);
        Vector2 pos = Read(pointerPos);
        Vector2 del = _middleHeld ? Read(pointerDelta) : Vector2.zero;

        bool overUI = false;
        if (EventSystem.current != null)
            overUI = EventSystem.current.IsPointerOverGameObject();

        _cam.TickInput(
            wasdMove: wasd,
            pointerPosition: pos,
            isMiddleDragging: _middleHeld,
            pointerDelta: del,
            rotateAxis: rot,
            zoomAxis: zm,
            pointerOverUI: overUI
        );
    }

    // helpers
    static void Enable(InputActionReference r) { if (r && r.action != null) r.action.Enable(); }
    static void Disable(InputActionReference r) { if (r && r.action != null) r.action.Disable(); }
    static Vector2 Read(InputActionReference r) { return (r && r.action != null) ? r.action.ReadValue<Vector2>() : Vector2.zero; }
    static float Read(InputActionReference r, bool _ = true) { return (r && r.action != null) ? r.action.ReadValue<float>() : 0f; }
}
