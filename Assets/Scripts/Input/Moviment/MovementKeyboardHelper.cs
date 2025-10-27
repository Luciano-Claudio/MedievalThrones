using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Helper de teclado para comandos de unidades usando Input System moderno
/// Usa um Action Map separado para não conflitar com controles da câmera
/// </summary>
public class MovementKeyboardHelper : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private MovementCommandHandler commandHandler;
    [SerializeField] private SelectionManager selectionManager;

    [Header("Action References - Unit Commands")]
    [Tooltip("Parar unidades selecionadas")]
    public InputActionReference stopUnitsAction;

    [Tooltip("Ciclar formações")]
    public InputActionReference cycleFormationAction;

    [Tooltip("Toggle Debug UI")]
    public InputActionReference toggleDebugAction;

    [Tooltip("Formação: Linha")]
    public InputActionReference formationLineAction;

    [Tooltip("Formação: Coluna")]
    public InputActionReference formationColumnAction;

    [Tooltip("Formação: Triangular")]
    public InputActionReference formationTriangularAction;

    [Tooltip("Formação: Cunha")]
    public InputActionReference formationWedgeAction;

    [Tooltip("Formação: Circular")]
    public InputActionReference formationCircularAction;

    [Tooltip("Formação: Quadrada")]
    public InputActionReference formationSquareAction;

    private UnitMovementDebugUI debugUI;
    private int currentFormationIndex = 0;

    void Awake()
    {
        if (!commandHandler) commandHandler = Object.FindFirstObjectByType<MovementCommandHandler>();
        if (!selectionManager) selectionManager = Object.FindFirstObjectByType<SelectionManager>();
        debugUI = GetComponent<UnitMovementDebugUI>();
    }

    void OnEnable()
    {
        // Habilitar todas as ações
        EnableAction(stopUnitsAction);
        EnableAction(cycleFormationAction);
        EnableAction(toggleDebugAction);
        EnableAction(formationLineAction);
        EnableAction(formationColumnAction);
        EnableAction(formationTriangularAction);
        EnableAction(formationWedgeAction);
        EnableAction(formationCircularAction);
        EnableAction(formationSquareAction);

        // Registrar callbacks
        RegisterCallback(stopUnitsAction, OnStopUnits);
        RegisterCallback(cycleFormationAction, OnCycleFormation);
        RegisterCallback(toggleDebugAction, OnToggleDebug);
        RegisterCallback(formationLineAction, ctx => SetFormation(FormationType.Line));
        RegisterCallback(formationColumnAction, ctx => SetFormation(FormationType.Column));
        RegisterCallback(formationTriangularAction, ctx => SetFormation(FormationType.Triangular));
        RegisterCallback(formationWedgeAction, ctx => SetFormation(FormationType.Wedge));
        RegisterCallback(formationCircularAction, ctx => SetFormation(FormationType.Circular));
        RegisterCallback(formationSquareAction, ctx => SetFormation(FormationType.Square));
    }

    void OnDisable()
    {
        // Desregistrar callbacks
        UnregisterCallback(stopUnitsAction, OnStopUnits);
        UnregisterCallback(cycleFormationAction, OnCycleFormation);
        UnregisterCallback(toggleDebugAction, OnToggleDebug);
        UnregisterCallback(formationLineAction, ctx => SetFormation(FormationType.Line));
        UnregisterCallback(formationColumnAction, ctx => SetFormation(FormationType.Column));
        UnregisterCallback(formationTriangularAction, ctx => SetFormation(FormationType.Triangular));
        UnregisterCallback(formationWedgeAction, ctx => SetFormation(FormationType.Wedge));
        UnregisterCallback(formationCircularAction, ctx => SetFormation(FormationType.Circular));
        UnregisterCallback(formationSquareAction, ctx => SetFormation(FormationType.Square));

        // Desabilitar todas as ações
        DisableAction(stopUnitsAction);
        DisableAction(cycleFormationAction);
        DisableAction(toggleDebugAction);
        DisableAction(formationLineAction);
        DisableAction(formationColumnAction);
        DisableAction(formationTriangularAction);
        DisableAction(formationWedgeAction);
        DisableAction(formationCircularAction);
        DisableAction(formationSquareAction);
    }

    // ========== CALLBACKS ==========

    void OnStopUnits(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (selectionManager == null || selectionManager.Count == 0) return;

        StopSelectedUnits();
    }

    void OnCycleFormation(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (commandHandler == null) return;

        CycleFormation();
    }

    void OnToggleDebug(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (debugUI == null) return;

        debugUI.ToggleDebugUI();
    }

    // ========== MÉTODOS ==========

    /// <summary>
    /// Para todas as unidades selecionadas
    /// </summary>
    void StopSelectedUnits()
    {
        int count = 0;
        foreach (var unit in selectionManager.Selection)
        {
            if (unit == null) continue;

            var movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.Stop();
                count++;
            }
        }

        Debug.Log($"<color=yellow>[KeyboardHelper] {count} unidades paradas</color>");
    }

    /// <summary>
    /// Cicla entre as formações disponíveis
    /// </summary>
    void CycleFormation()
    {
        currentFormationIndex = (currentFormationIndex + 1) % 6; // 6 formações (0-5)
        FormationType newFormation = (FormationType)currentFormationIndex;

        SetFormation(newFormation);
    }

    /// <summary>
    /// Define uma formação específica
    /// </summary>
    void SetFormation(FormationType formation)
    {
        if (commandHandler == null) return;

        // CORRIGIDO: Usar SetFormation ao invés de ChangeFormation
        commandHandler.SetFormation(formation);
        currentFormationIndex = (int)formation;

        Debug.Log($"<color=cyan>[KeyboardHelper] Formação mudada para: {formation}</color>");
    }

    // ========== HELPERS ==========

    static void EnableAction(InputActionReference actionRef)
    {
        if (actionRef?.action != null)
            actionRef.action.Enable();
    }

    static void DisableAction(InputActionReference actionRef)
    {
        if (actionRef?.action != null)
            actionRef.action.Disable();
    }

    static void RegisterCallback(InputActionReference actionRef, System.Action<InputAction.CallbackContext> callback)
    {
        if (actionRef?.action != null)
            actionRef.action.performed += callback;
    }

    static void UnregisterCallback(InputActionReference actionRef, System.Action<InputAction.CallbackContext> callback)
    {
        if (actionRef?.action != null)
            actionRef.action.performed -= callback;
    }

    /// <summary>
    /// Exibe ajuda de teclas no console
    /// </summary>
    [ContextMenu("Show Keyboard Help")]
    public void ShowHelp()
    {
        Debug.Log($"=== TECLAS DE ATALHO (Configure no Input Actions) ===\n" +
                  $"<b>Stop Units</b> - Parar unidades selecionadas\n" +
                  $"<b>Cycle Formation</b> - Ciclar formações\n" +
                  $"<b>Toggle Debug</b> - Toggle Debug UI\n" +
                  $"<b>Formation 1-6</b> - Formações diretas\n" +
                  $"\n" +
                  $"<color=yellow>Configure as teclas no Input Actions Asset!</color>");
    }
}