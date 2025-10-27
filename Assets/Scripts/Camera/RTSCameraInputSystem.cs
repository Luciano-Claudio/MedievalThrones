using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Tradutor de Input System para RTSCameraCinemachineV3Controller.
/// Lê ações configuradas e repassa para o controlador via TickInput.
/// Refatoração: pequenos ajustes de documentação e organização.
/// </summary>
[RequireComponent(typeof(RTSCameraCinemachineV3Controller))]
public class RTSCameraInputSystem : MonoBehaviour
{
    [Header("Action References (arraste do .inputactions)")]
    [Tooltip("Movimento WASD (Vector2 - 2D Vector)")]
    public InputActionReference move;

    [Tooltip("Rotação Q/E (1D Axis)")]
    public InputActionReference rotate;

    [Tooltip("Zoom com scroll do mouse (Axis)")]
    public InputActionReference zoom;

    [Tooltip("Posição do ponteiro na tela (Vector2)")]
    public InputActionReference pointerPos;

    [Tooltip("Delta de movimento do ponteiro (Vector2)")]
    public InputActionReference pointerDelta;

    [Tooltip("Botão do meio do mouse (Button)")]
    public InputActionReference middleButton;

    private RTSCameraCinemachineV3Controller _cam;
    private bool _middleHeld;

    void Awake()
    {
        _cam = GetComponent<RTSCameraCinemachineV3Controller>();

        if (_cam == null)
        {
            Debug.LogError($"[RTSCameraInput] Componente RTSCameraCinemachineV3Controller " +
                          $"não encontrado em {gameObject.name}!", this);
        }
    }

    void OnEnable()
    {
        Enable(move);
        Enable(rotate);
        Enable(zoom);
        Enable(pointerPos);
        Enable(pointerDelta);
        Enable(middleButton);

        if (middleButton?.action != null)
        {
            middleButton.action.performed += OnMiddle;
            middleButton.action.canceled += OnMiddle;
        }
    }

    void OnDisable()
    {
        if (middleButton?.action != null)
        {
            middleButton.action.performed -= OnMiddle;
            middleButton.action.canceled -= OnMiddle;
        }

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
        if (_cam == null) return;

        // Ler inputs
        Vector2 wasd = Read(move);
        float rot = ReadFloat(rotate);
        float zm = ReadFloat(zoom);
        Vector2 pos = Read(pointerPos);
        Vector2 del = _middleHeld ? Read(pointerDelta) : Vector2.zero;

        // Detectar se o ponteiro está sobre UI
        bool overUI = EventSystem.current != null
                      && EventSystem.current.IsPointerOverGameObject();

        // Repassar para o controlador
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

    // ==================== HELPERS ====================

    static void Enable(InputActionReference r)
    {
        if (r?.action != null)
            r.action.Enable();
    }

    static void Disable(InputActionReference r)
    {
        if (r?.action != null)
            r.action.Disable();
    }

    static Vector2 Read(InputActionReference r)
    {
        return (r?.action != null) ? r.action.ReadValue<Vector2>() : Vector2.zero;
    }

    static float ReadFloat(InputActionReference r)
    {
        return (r?.action != null) ? r.action.ReadValue<float>() : 0f;
    }
}