using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputSelection : MonoBehaviour
{
    [Header("Refs")]
    public WorldPicker picker;

    [Header("Config")]
    public float dragThresholdPx = 6f;
    public float doubleClickWindow = 0.28f;

    [Header("Actions (arraste do seu asset)")]
    public InputActionReference point; // Vector2
    public InputActionReference lmb;   // Button
    public InputActionReference rmb;   // Button
    public InputActionReference ctrl;  // Button
    public InputActionReference shift; // Button

    // REFATORAÇÃO: Eventos locais removidos, agora usa GameEvents
    // public event Action<Vector2> OnPointerDown;           // REMOVIDO
    // public event Action<Vector2> OnPointerUp;             // REMOVIDO
    // public event Action<Vector2> OnBeginDrag;             // REMOVIDO
    // public event Action<Vector2> OnDragging;              // REMOVIDO
    // public event Action<Vector2> OnEndDrag;               // REMOVIDO
    // public event Action<Unit, bool> OnClickUnit;          // REMOVIDO
    // public event Action<Unit> OnDoubleClickUnit;          // REMOVIDO
    // public event Action<Vector3, bool> OnClickGround;     // REMOVIDO

    public bool IsCtrlPressed => ctrl != null && ctrl.action.IsPressed();
    public bool IsShiftPressed => shift != null && shift.action.IsPressed();

    Vector2 _pointer;
    bool _lmbDown;
    Vector2 _downPos;
    bool _dragging;

    bool _pressedOverUI;           // << começou sobre UI?
    float _lastClickTime;
    Unit _lastClickedUnit;
    bool _overUIThisFrame;

    void OnEnable()
    {
        point?.action.Enable();
        lmb?.action.Enable();
        rmb?.action.Enable();
        ctrl?.action.Enable();
        shift?.action.Enable();

        point.action.performed += OnPointPerformed;
        lmb.action.started += OnLmbStarted;
        lmb.action.canceled += OnLmbCanceled;
        rmb.action.performed += OnRmbPerformed;
    }

    void OnDisable()
    {
        point.action.performed -= OnPointPerformed;
        lmb.action.started -= OnLmbStarted;
        lmb.action.canceled -= OnLmbCanceled;
        rmb.action.performed -= OnRmbPerformed;

        point?.action.Disable();
        lmb?.action.Disable();
        rmb?.action.Disable();
        ctrl?.action.Disable();
        shift?.action.Disable();
    }

    private void LateUpdate()
    {
        _overUIThisFrame = ComputePointerOverUI();
    }

    void Update()
    {
        if (_lmbDown && !_dragging && !_pressedOverUI &&
            Vector2.Distance(_downPos, _pointer) >= dragThresholdPx)
        {
            _dragging = true;
            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaiseDragBegin(_downPos);
        }
    }

    void OnPointPerformed(InputAction.CallbackContext ctx)
    {
        // Ignora LMB iniciado sobre UI
        if (IsPointerOverUI()) return;
        _pointer = ctx.ReadValue<Vector2>();
        if (_lmbDown && _dragging && !_pressedOverUI)
        {
            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaiseDragging(_pointer);
        }
    }

    void OnLmbStarted(InputAction.CallbackContext _)
    {
        _lmbDown = true;
        _downPos = _pointer;

        // <<< trava tudo se o clique começou sobre UI
        _pressedOverUI = IsPointerOverUI();
        _dragging = false;

        if (!_pressedOverUI)
        {
            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaisePointerDown(_downPos);
        }
    }

    void OnLmbCanceled(InputAction.CallbackContext _)
    {
        var upPos = _pointer;

        if (_pressedOverUI)
        {
            // Clique começou em UI → não é seleção do mundo
            _pressedOverUI = false;
            _lmbDown = false;
            return;
        }

        if (_dragging)
        {
            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaiseDragEnd(upPos);
        }
        else
        {
            HandleClick(upPos);
        }

        // REFATORAÇÃO: Disparar evento via GameEvents
        GameEvents.RaisePointerUp(upPos);
        _lmbDown = false;
    }

    void OnRmbPerformed(InputAction.CallbackContext ctx)
    {
        // Ignora RMB iniciado sobre UI
        if (IsPointerOverUI()) return;

        if (picker != null && picker.TryPickGroundAt(_pointer, out var p, out _))
        {
            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaiseGroundClick(p, false);
        }
    }

    void HandleClick(Vector2 screenPos)
    {
        if (picker == null) return;

        bool isCtrl = IsCtrlPressed;
        bool isShift = IsShiftPressed;

        if (picker.TryPickUnitAt(screenPos, out var unit))
        {
            // double click
            if (unit == _lastClickedUnit &&
                (Time.unscaledTime - _lastClickTime) <= doubleClickWindow)
            {
                // REFATORAÇÃO: Disparar evento via GameEvents
                GameEvents.RaiseUnitDoubleClick(unit);
                _lastClickedUnit = null;
                _lastClickTime = 0f;
                return;
            }

            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaiseUnitClick(unit, isCtrl);
            _lastClickedUnit = unit;
            _lastClickTime = Time.unscaledTime;
        }
        else if (picker.TryPickGroundAt(screenPos, out var point, out _))
        {
            // REFATORAÇÃO: Disparar evento via GameEvents
            GameEvents.RaiseGroundClick(point, isCtrl);
            _lastClickedUnit = null;
            _lastClickTime = 0f;
        }
    }

    bool IsPointerOverUI() => _overUIThisFrame;

    bool ComputePointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // --- Input System novo: melhor passar um pointerId ---
#if ENABLE_INPUT_SYSTEM
        // Mouse
        if (Mouse.current != null)
            return EventSystem.current.IsPointerOverGameObject(Mouse.current.deviceId);

        // Toque (qualquer dedo ativo)
        if (Touchscreen.current != null)
        {
            foreach (var t in Touchscreen.current.touches)
                if (t.isInProgress && EventSystem.current.IsPointerOverGameObject(t.touchId.ReadValue()))
                    return true;
        }
#endif

        // Fallback (standalone/legacy)
        return EventSystem.current.IsPointerOverGameObject();
    }
}