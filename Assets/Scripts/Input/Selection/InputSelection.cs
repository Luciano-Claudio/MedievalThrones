using System;
using UnityEngine;
using UnityEngine.InputSystem;

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

    float _lastClickTime;
    Unit _lastClickedUnit;

    void OnEnable()
    {
        // habilita actions
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

    void OnPointPerformed(InputAction.CallbackContext ctx)
    {
        _pointer = ctx.ReadValue<Vector2>();
        if (_lmbDown && _dragging)
            OnDragging?.Invoke(_pointer);
    }

    void Update()
    {
        if (_lmbDown && !_dragging &&
            Vector2.Distance(_downPos, _pointer) >= dragThresholdPx)
        {
            _dragging = true;
            OnBeginDrag?.Invoke(_downPos);
        }
    }

    void OnLmbStarted(InputAction.CallbackContext _)
    {
        _lmbDown = true;
        _downPos = _pointer;
        _dragging = false;
        OnPointerDown?.Invoke(_downPos);
    }

    void OnLmbCanceled(InputAction.CallbackContext _)
    {
        var upPos = _pointer;

        if (_dragging) OnEndDrag?.Invoke(upPos);
        else HandleClick(upPos);

        OnPointerUp?.Invoke(upPos);
        _lmbDown = false;
    }

    void OnRmbPerformed(InputAction.CallbackContext ctx)
    {
        if (picker.TryPickGroundAt(_pointer, out var p, out _))   // agora _ é discard de Vector3
            OnClickGround?.Invoke(p, false, false);
    }


    void HandleClick(Vector2 screenPos)
    {
        bool isCtrl = ctrl != null && ctrl.action.IsPressed();
        bool isShift = shift != null && shift.action.IsPressed();

        if (picker.TryPickUnitAt(screenPos, out var unit))
        {
            if (unit == _lastClickedUnit &&
                (Time.unscaledTime - _lastClickTime) <= doubleClickWindow)
            {
                OnDoubleClickUnit?.Invoke(unit);
                _lastClickTime = 0f; _lastClickedUnit = null;
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
}
