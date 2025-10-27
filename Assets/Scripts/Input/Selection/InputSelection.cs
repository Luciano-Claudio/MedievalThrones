using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Gerencia input de seleção e comandos de unidades
/// VERSÃO 3.1 - Corrigido sistema de prioridade de ações
/// 
/// PRIORIDADE DE AÇÕES (maior → menor):
/// 1. UI (EventSystem)
/// 2. Drag Selection (retângulo de seleção)
/// 3. Unit Click (selecionar unidade)
/// 4. Movement Command (mover unidades)
/// 5. Ground Click (desselecionar com Ctrl)
/// 
/// PROTEÇÕES:
/// - Click em unidade NÃO dispara movimento
/// - Drag NÃO dispara movimento no início
/// - Click sobre UI é ignorado completamente
/// </summary>
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
            // Disparar evento via GameEvents
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
            // Disparar evento via GameEvents
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
            // Disparar evento via GameEvents
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
            // DRAG: Disparar evento de fim de drag
            // NÃO chama HandleClick(), evitando movimento no fim do drag
            GameEvents.RaiseDragEnd(upPos);
        }
        else
        {
            // CLICK: Processar clique normal
            HandleClick(upPos);
        }

        // Disparar evento via GameEvents
        GameEvents.RaisePointerUp(upPos);
        _lmbDown = false;
        _dragging = false;  // Reset do drag
    }

    void OnRmbPerformed(InputAction.CallbackContext ctx)
    {
        // Ignora RMB iniciado sobre UI
        if (IsPointerOverUI()) return;

        if (picker != null && picker.TryPickGroundAt(_pointer, out var p, out _))
        {
            // Disparar evento via GameEvents
            GameEvents.RaiseGroundClick(p, false);
        }
    }

    /// <summary>
    /// Processa clique do mouse com SISTEMA DE PRIORIDADE
    /// CRÍTICO: A ordem dos IFs define a prioridade!
    /// </summary>
    void HandleClick(Vector2 screenPos)
    {
        if (picker == null) return;

        bool isCtrl = IsCtrlPressed;
        bool isShift = IsShiftPressed;

        // ============================================================
        // PRIORIDADE 1: CLICAR EM UNIDADE (Seleção)
        // ============================================================
        // Se clicou em uma unidade, APENAS seleciona, NÃO move
        if (picker.TryPickUnitAt(screenPos, out var unit))
        {
            // Verificar double click
            if (unit == _lastClickedUnit &&
                (Time.unscaledTime - _lastClickTime) <= doubleClickWindow)
            {
                GameEvents.RaiseUnitDoubleClick(unit);
                _lastClickedUnit = null;
                _lastClickTime = 0f;
                return;  // ← CRÍTICO: Sai aqui, NÃO dispara movimento
            }

            // Click simples em unidade
            GameEvents.RaiseUnitClick(unit, isCtrl);
            _lastClickedUnit = unit;
            _lastClickTime = Time.unscaledTime;

            // ← CRÍTICO: RETURN aqui impede que execute o código abaixo
            // Isso evita que clicar em unidade dispare comando de movimento
            return;
        }

        // ============================================================
        // PRIORIDADE 2: CLICAR NO CHÃO
        // ============================================================
        // Se chegou aqui, NÃO clicou em unidade
        // Pode ser: movimento, desselecionar, ou interação futura (minerar, etc)

        if (picker.TryPickGroundAt(screenPos, out var point, out _))
        {
            // REGRA: Click no chão SEM modificadores = Comando de Movimento
            if (!isCtrl && !isShift)
            {
                // Disparar comando de movimento
                GameEvents.RaiseMoveCommand(point);
            }
            else
            {
                // Click no chão COM Ctrl/Shift = Desselecionar (comportamento antigo)
                GameEvents.RaiseGroundClick(point, isCtrl);
            }

            // Limpar estado de double click
            _lastClickedUnit = null;
            _lastClickTime = 0f;
        }

        // ============================================================
        // PRIORIDADE 3 (FUTURO): INTERAÇÕES COM RECURSOS
        // ============================================================
        // Aqui você pode adicionar no futuro:
        // - if (picker.TryPickTree(...)) → Cortar árvore
        // - if (picker.TryPickRock(...)) → Minerar pedra
        // - if (picker.TryPickEnemyUnit(...)) → Atacar
        // Etc.
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