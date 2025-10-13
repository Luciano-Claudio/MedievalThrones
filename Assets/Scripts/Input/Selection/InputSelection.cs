using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // << precisa disso para IsPointerOverGameObject()

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

    public event Action<Vector2> OnPointerDown;
    public event Action<Vector2> OnPointerUp;

    public event Action<Vector2> OnBeginDrag;
    public event Action<Vector2> OnDragging;
    public event Action<Vector2> OnEndDrag;

    public event Action<Unit, bool, bool> OnClickUnit;
    public event Action<Unit> OnDoubleClickUnit;
    public event Action<Vector3, bool, bool> OnClickGround;

    public bool IsCtrlPressed => ctrl != null && ctrl.action.IsPressed();
    public bool IsShiftPressed => shift != null && shift.action.IsPressed();

    Vector2 _pointer;
    bool _lmbDown;
    Vector2 _downPos;
    bool _dragging;

    bool _pressedOverUI;           // << NOVO: começou sobre UI?
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
            OnBeginDrag?.Invoke(_downPos);
        }
    }

    void OnPointPerformed(InputAction.CallbackContext ctx)
    {
        // Ignora LMB iniciado sobre UI
        if (IsPointerOverUI()) return;      // usa o cache -> some o warning
        _pointer = ctx.ReadValue<Vector2>();
        if (_lmbDown && _dragging && !_pressedOverUI)
            OnDragging?.Invoke(_pointer);
    }
    void OnLmbStarted(InputAction.CallbackContext _)
    {
        _lmbDown = true;
        _downPos = _pointer;

        // <<< NOVO: trava tudo se o clique começou sobre UI
        _pressedOverUI = IsPointerOverUI();
        _dragging = false;

        if (!_pressedOverUI)
            OnPointerDown?.Invoke(_downPos);
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

        if (_dragging) OnEndDrag?.Invoke(upPos);
        else HandleClick(upPos);

        OnPointerUp?.Invoke(upPos);
        _lmbDown = false;
    }

    void OnRmbPerformed(InputAction.CallbackContext ctx)
    {
        // Ignora RMB iniciado sobre UI
        if (IsPointerOverUI()) return;      // usa o cache -> some o warning

        if (picker != null && picker.TryPickGroundAt(_pointer, out var p, out _))
            OnClickGround?.Invoke(p, false, false);
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
                OnDoubleClickUnit?.Invoke(unit);
                _lastClickedUnit = null;
                _lastClickTime = 0f;
                return;
            }

            OnClickUnit?.Invoke(unit, isCtrl, isShift);
            _lastClickedUnit = unit;
            _lastClickTime = Time.unscaledTime;
        }
        else if (picker.TryPickGroundAt(screenPos, out var point, out _))
        {
            OnClickGround?.Invoke(point, isCtrl, isShift);
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
