using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Gerencia comandos de movimentação para unidades selecionadas
/// Integra Input System, FormationCalculator, UnitMovement e FormationGroupManager
/// 
/// VERSÃO 3.0 - Sistema de Grupos de Formação Independentes
/// 
/// MUDANÇAS PRINCIPAIS:
/// - Formação agora é POR GRUPO, não global
/// - Grupos são criados/destruídos automaticamente
/// - Mescla de grupos: formação do maior prevalece
/// - Subdividir grupo: cria novo grupo
/// - Formação None: unidades soltas (sem grupo)
/// 
/// LÓGICA DE GRUPOS:
/// 1. Movimento: Detecta grupos na seleção → Determina formação → Cria/atualiza grupo
/// 2. Mudança de formação: Atualiza grupo existente ou cria novo
/// 3. Mescla automática: Seleção com múltiplos grupos → Grupo maior vence
/// 4. Subdivisão: Mover parte do grupo → Cria novo grupo com mesma formação
/// </summary>
public class MovementCommandHandler : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private Camera mainCamera;

    [Header("Configuração de Formação")]
    [Tooltip("Formação padrão para NOVOS grupos (sempre None - grupos começam soltos)")]
    [SerializeField] private FormationType defaultFormation = FormationType.None;
    [SerializeField] private float formationSpacing = 2f;
    [SerializeField] private LayerMask groundLayer = -1;

    [Header("Detecção de Gargalos")]
    [SerializeField] private bool enableNarrowPassageDetection = true;
    [SerializeField] private float narrowPassageWidth = 6f;
    [SerializeField] private float checkDistance = 10f;
    [SerializeField] private int checkRayCount = 5;

    [Header("Centro Inteligente")]
    [Tooltip("Usar sistema de centro inteligente do FormationCalculator (ignora outliers)")]
    [SerializeField] private bool useIntelligentCenter = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showDebugGizmos = true;

    // Estado interno
    private Vector3 lastCommandPosition;
    bool _overUIThisFrame;

    // Cache
    private List<Unit> selectedUnitsList = new List<Unit>();

    // ========== PROPRIEDADES ==========

    /// <summary>
    /// Formação padrão para novos grupos
    /// IMPORTANTE: Deve ser sempre None - grupos novos começam soltos
    /// Formação é alterada apenas quando jogador escolhe explicitamente
    /// </summary>
    public FormationType DefaultFormation
    {
        get => defaultFormation;
        set => defaultFormation = value;
    }

    public float FormationSpacing => formationSpacing;

    // ========== LIFECYCLE ==========

    void Awake()
    {
        if (!selectionManager) selectionManager = Object.FindFirstObjectByType<SelectionManager>();
        if (!mainCamera) mainCamera = Camera.main;
    }

    void OnEnable()
    {
        // ❌ REMOVIDO: Não escutar leftClickAction diretamente
        // if (leftClickAction?.action != null)
        //     leftClickAction.action.performed += OnLeftClickPerformed;

        // ❌ REMOVIDO: Não escutar rightClickAction diretamente
        // if (rightClickAction?.action != null)
        //     rightClickAction.action.performed += OnRightClickPerformed;

        // ✅ NOVO: Escutar apenas o evento de comando de movimento
        GameEvents.OnMoveCommand += OnMoveCommandReceived;

        // Subscrever evento de mudança de seleção
        GameEvents.OnSelectionChanged += OnSelectionChanged;
    }

    void OnDisable()
    {
        // ❌ REMOVIDO: Não escutar leftClickAction diretamente
        // if (leftClickAction?.action != null)
        //     leftClickAction.action.performed -= OnLeftClickPerformed;

        // ❌ REMOVIDO: Não escutar rightClickAction diretamente
        // if (rightClickAction?.action != null)
        //     rightClickAction.action.performed -= OnRightClickPerformed;

        // ✅ NOVO: Desinscrever evento de comando de movimento
        GameEvents.OnMoveCommand -= OnMoveCommandReceived;

        GameEvents.OnSelectionChanged -= OnSelectionChanged;
    }

    private void LateUpdate()
    {
        _overUIThisFrame = ComputePointerOverUI();
    }

    // ========== EVENT HANDLERS ==========

    /// <summary>
    /// Chamado quando a seleção muda
    /// </summary>
    void OnSelectionChanged(IEnumerable<Unit> newSelection)
    {
        if (showDebugLogs)
            Debug.Log("<color=yellow>[MovementCommandHandler] Seleção mudou</color>");
    }

    // ========== INPUT HANDLERS ==========

    /// <summary>
    /// Recebe comando de movimento do InputSelection via GameEvents
    /// Apenas disparado quando jogador clica no CHÃO (não em unidades)
    /// </summary>
    void OnMoveCommandReceived(Vector3 worldPosition)
    {
        if (selectionManager == null || selectionManager.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("<color=yellow>[MovementCommandHandler] Comando de movimento ignorado - nenhuma unidade selecionada</color>");
            return;
        }

        // Mover unidades para a posição
        MoveSelectedUnitsTo(worldPosition);
    }

    /// <summary>
    /// DEPRECATED: Não usar mais
    /// Mantido apenas para compatibilidade com Inspector (remova as referências!)
    /// </summary>
    void OnRightClickPerformed(InputAction.CallbackContext context)
    {
        // Não faz nada - RMB é tratado no InputSelection
        // Este método pode ser removido no futuro
    }

    // ========== COMANDO DE MOVIMENTO (COM SISTEMA DE GRUPOS) ==========

    /// <summary>
    /// Move todas as unidades selecionadas para uma posição
    /// NOVO: Usa sistema de grupos para determinar formação
    /// </summary>
    public void MoveSelectedUnitsTo(Vector3 targetPosition)
    {
        if (selectionManager == null || selectionManager.Count == 0)
        {
            Debug.LogWarning("[MovementCommandHandler] Nenhuma unidade selecionada!");
            return;
        }

        lastCommandPosition = targetPosition;

        // Coletar unidades selecionadas
        selectedUnitsList.Clear();
        foreach (var unit in selectionManager.Selection)
        {
            if (unit != null && unit.GetComponent<UnitMovement>() != null)
                selectedUnitsList.Add(unit);
        }

        if (selectedUnitsList.Count == 0)
        {
            Debug.LogWarning("[MovementCommandHandler] Nenhuma unidade selecionada tem UnitMovement!");
            return;
        }

        // ============================================================
        // SISTEMA DE GRUPOS: Detectar e determinar formação
        // ============================================================

        FormationType formationToUse;

        // Detectar grupos na seleção
        var groupsInSelection = FormationGroupManager.Instance.GetGroupsInSelection(selectedUnitsList, out List<Unit> unitsWithoutGroup);

        if (groupsInSelection.Count == 0 && unitsWithoutGroup.Count > 0)
        {
            // Apenas unidades soltas - usar formação padrão
            formationToUse = defaultFormation;

            if (showDebugLogs)
                Debug.Log($"<color=cyan>[MovementCommandHandler] Apenas unidades soltas - usando formação padrão: {formationToUse}</color>");
        }
        else if (groupsInSelection.Count == 0)
        {
            // Nenhuma unidade válida
            formationToUse = defaultFormation;

            if (showDebugLogs)
                Debug.Log($"<color=cyan>[MovementCommandHandler] Nenhum grupo detectado - usando formação padrão: {formationToUse}</color>");
        }
        else
        {
            // Há grupos envolvidos - determinar formação pela regra de mescla
            formationToUse = FormationGroupManager.Instance.DetermineFormationForSelection(selectedUnitsList);

            if (showDebugLogs)
            {
                int groupCount = groupsInSelection.Count;
                Debug.Log($"<color=cyan>[MovementCommandHandler] {groupCount} grupo(s) detectado(s) - formação determinada: {formationToUse}</color>");
            }
        }

        // ============================================================
        // DETECÇÃO DE GARGALOS (pode sobrescrever formação)
        // ============================================================

        if (enableNarrowPassageDetection && formationToUse != FormationType.None)
        {
            if (DetectNarrowPassage(targetPosition))
            {
                formationToUse = FormationType.Column; // Forçar coluna em gargalos
                if (showDebugLogs)
                    Debug.Log($"<color=orange>[MovementCommandHandler] Gargalo detectado! Forçando COLUNA</color>");
            }
        }

        // ============================================================
        // CRIAR/ATUALIZAR GRUPO
        // ============================================================

        FormationGroup group = null;

        if (formationToUse != FormationType.None)
        {
            // Criar ou atualizar grupo
            group = FormationGroupManager.Instance.CreateGroup(selectedUnitsList, formationToUse);

            if (group != null)
            {
                group.LastCenter = targetPosition;
                group.LastMoveTime = Time.time;
            }
        }
        else
        {
            // Formação None - remover unidades de grupos
            foreach (var unit in selectedUnitsList)
            {
                FormationGroupManager.Instance.RemoveUnitFromAnyGroup(unit);
            }
        }

        // ============================================================
        // CALCULAR E APLICAR FORMAÇÃO
        // ============================================================

        Dictionary<Unit, Vector3> formation = FormationCalculator.CalculateFormation(
            selectedUnitsList,
            formationToUse,
            formationSpacing
        );

        // Aplicar movimento
        foreach (var kvp in formation)
        {
            Unit unit = kvp.Key;
            Vector3 offset = kvp.Value;

            var movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.MoveToWithOffset(targetPosition, offset);
            }
        }

        // Log de feedback
        if (showDebugLogs)
        {
            string formationName = formationToUse == FormationType.None ? "Livre (None)" : formationToUse.ToString();
            string groupInfo = group != null ? $" [Grupo {group.GroupID}]" : " [Sem grupo]";
            Debug.Log($"<color=lime>[MovementCommandHandler] {selectedUnitsList.Count} unidades movendo para " +
                     $"{targetPosition} em formação {formationName}{groupInfo}</color>");
        }
    }

    // ========== MUDANÇA DE FORMAÇÃO (COM SISTEMA DE GRUPOS) ==========

    /// <summary>
    /// Muda a formação das unidades selecionadas
    /// NOVO: Atualiza ou cria grupos automaticamente
    /// IMPORTANTE: Não altera defaultFormation - novos grupos sempre começam com None
    /// </summary>
    public void SetFormation(FormationType newFormation)
    {
        if (showDebugLogs)
            Debug.Log($"<color=cyan>[MovementCommandHandler] Alterando formação para: {newFormation}</color>");

        // Se não há unidades selecionadas, não faz nada
        if (selectionManager == null || selectionManager.Count == 0)
        {
            if (showDebugLogs)
                Debug.Log("<color=yellow>[MovementCommandHandler] Nenhuma unidade selecionada. " +
                         "Nenhuma ação realizada.</color>");
            return;
        }

        // ============================================================
        // TRATAMENTO ESPECIAL PARA FORMAÇÃO NONE
        // ============================================================
        if (newFormation == FormationType.None)
        {
            // Remover unidades de grupos existentes
            foreach (var unit in selectionManager.Selection)
            {
                FormationGroupManager.Instance.RemoveUnitFromAnyGroup(unit);
            }

            if (showDebugLogs)
                Debug.Log("<color=yellow>[MovementCommandHandler] Formação NONE ativada. " +
                         "Unidades removidas de grupos.</color>");

            return; // ← IMPORTANTE: Retornar aqui para NÃO reorganizar
        }

        // ============================================================
        // REORGANIZAR UNIDADES NA NOVA FORMAÇÃO
        // ============================================================

        ReorganizeFormationInPlace(newFormation);
    }

    /// <summary>
    /// Reorganiza as unidades na formação especificada mantendo o centro do grupo
    /// NOVO: Usa sistema de grupos
    /// </summary>
    void ReorganizeFormationInPlace(FormationType newFormation)
    {
        // Coletar unidades selecionadas
        selectedUnitsList.Clear();
        foreach (var unit in selectionManager.Selection)
        {
            if (unit != null && unit.GetComponent<UnitMovement>() != null)
                selectedUnitsList.Add(unit);
        }

        if (selectedUnitsList.Count == 0)
        {
            Debug.LogWarning("[MovementCommandHandler] Nenhuma unidade válida para reorganizar!");
            return;
        }

        // ============================================================
        // DETECTAR GRUPOS E DETERMINAR CENTRO
        // ============================================================

        var groupsInSelection = FormationGroupManager.Instance.GetGroupsInSelection(selectedUnitsList, out List<Unit> unitsWithoutGroup);

        Vector3 center;

        if (groupsInSelection.Count == 1 && unitsWithoutGroup.Count == 0)
        {
            // Todas do mesmo grupo - usar centro do grupo
            var existingGroup = groupsInSelection.Keys.First();
            center = existingGroup.LastCenter;

            if (center == Vector3.zero)
            {
                // Calcular centro se não tem um definido
                center = useIntelligentCenter
                    ? FormationCalculator.CalculateIntelligentCenter(selectedUnitsList)
                    : GetAveragePosition(selectedUnitsList);
            }

            if (showDebugLogs)
                Debug.Log($"<color=cyan>[MovementCommandHandler] Grupo {existingGroup.GroupID} - mantendo centro: {center}</color>");
        }
        else
        {
            // Múltiplos grupos ou mistura - calcular novo centro
            center = useIntelligentCenter
                ? FormationCalculator.CalculateIntelligentCenter(selectedUnitsList)
                : GetAveragePosition(selectedUnitsList);

            if (showDebugLogs)
                Debug.Log($"<color=cyan>[MovementCommandHandler] Múltiplos grupos ou mistura - calculando novo centro: {center}</color>");
        }

        // ============================================================
        // CRIAR/ATUALIZAR GRUPO COM NOVA FORMAÇÃO
        // ============================================================

        FormationGroup group = FormationGroupManager.Instance.CreateGroup(selectedUnitsList, newFormation);

        if (group != null)
        {
            group.LastCenter = center;
            group.LastMoveTime = Time.time;
        }

        // ============================================================
        // CALCULAR E APLICAR NOVA FORMAÇÃO
        // ============================================================

        Dictionary<Unit, Vector3> formation = FormationCalculator.CalculateFormation(
            selectedUnitsList,
            newFormation,
            formationSpacing
        );

        // Aplicar movimento mantendo o centro fixo
        foreach (var kvp in formation)
        {
            Unit unit = kvp.Key;
            Vector3 offset = kvp.Value;

            var movement = unit.GetComponent<UnitMovement>();
            if (movement != null)
            {
                movement.MoveToWithOffset(center, offset);
            }
        }

        // Log de feedback
        if (showDebugLogs)
        {
            string groupInfo = group != null ? $"Grupo {group.GroupID}" : "Sem grupo";
            Debug.Log($"<color=lime>[MovementCommandHandler] {selectedUnitsList.Count} unidades reorganizadas " +
                     $"em {newFormation} no centro {center} [{groupInfo}]</color>");
        }
    }

    // ========== DETECÇÃO DE GARGALOS ==========

    /// <summary>
    /// Detecta se há um gargalo/passagem estreita no caminho até o destino
    /// </summary>
    bool DetectNarrowPassage(Vector3 targetPosition)
    {
        if (selectedUnitsList.Count == 0) return false;

        // Pegar posição média das unidades selecionadas
        Vector3 currentCenter = GetAveragePosition(selectedUnitsList);

        // Direção e distância até o alvo
        Vector3 direction = (targetPosition - currentCenter).normalized;
        float distanceToTarget = Vector3.Distance(currentCenter, targetPosition);

        // Fazer vários raycasts perpendiculares ao caminho
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;

        int narrowChecks = 0;

        for (int i = 0; i < checkRayCount; i++)
        {
            // Ponto ao longo do caminho
            float t = (i + 1f) / (checkRayCount + 1f);
            Vector3 checkPoint = currentCenter + direction * (checkDistance * t);

            // Raycast para ambos os lados
            if (Physics.Raycast(checkPoint + perpendicular * narrowPassageWidth / 2f, -perpendicular, narrowPassageWidth, groundLayer))
            {
                narrowChecks++;
            }

            // Debug
            if (showDebugGizmos)
            {
                Debug.DrawLine(checkPoint + perpendicular * narrowPassageWidth / 2f,
                              checkPoint - perpendicular * narrowPassageWidth / 2f,
                              narrowChecks > 0 ? Color.red : Color.green,
                              1f);
            }
        }

        // Se mais da metade dos checks detectaram estreitamento, é um gargalo
        return narrowChecks > checkRayCount / 2;
    }

    // ========== UTILITÁRIOS ==========

    // ========== UTILITÁRIOS ==========

    /// <summary>
    /// Calcula posição média de uma lista de unidades
    /// </summary>
    Vector3 GetAveragePosition(List<Unit> units)
    {
        if (units == null || units.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var unit in units)
        {
            if (unit != null)
                sum += unit.transform.position;
        }

        return sum / units.Count;
    }

    /// <summary>
    /// Calcula preview de formação para uma posição (usado pelo FormationVisualizer)
    /// </summary>
    public Dictionary<Unit, Vector3> CalculateFormationPreview(Vector3 position)
    {
        if (selectionManager == null || selectionManager.Count == 0)
            return new Dictionary<Unit, Vector3>();

        // Coletar unidades selecionadas
        List<Unit> units = new List<Unit>();
        foreach (var unit in selectionManager.Selection)
        {
            if (unit != null)
                units.Add(unit);
        }

        if (units.Count == 0)
            return new Dictionary<Unit, Vector3>();

        // Determinar formação a usar
        FormationType formationToUse = FormationGroupManager.Instance.DetermineFormationForSelection(units);

        if (formationToUse == FormationType.None)
            formationToUse = defaultFormation;

        // Calcular e retornar formação
        return FormationCalculator.CalculateFormation(units, formationToUse, formationSpacing);
    }

    // Método TryGetWorldPosition removido - não é mais necessário
    // Input agora vem via GameEvents.OnMoveCommand já com posição calculada

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

    // ========== DEBUG ==========

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || selectionManager == null || selectionManager.Count == 0)
            return;

        // Desenhar informações de grupos das unidades selecionadas
        selectedUnitsList.Clear();
        foreach (var unit in selectionManager.Selection)
        {
            if (unit != null)
                selectedUnitsList.Add(unit);
        }

        if (selectedUnitsList.Count == 0)
            return;

        // Detectar grupos na seleção
        List<Unit> unitsWithoutGroup = null;
        var groupsInSelection = FormationGroupManager.Instance?.GetGroupsInSelection(selectedUnitsList, out unitsWithoutGroup);

        if (groupsInSelection == null)
            return;

        // Desenhar cada grupo com cor diferente
        Color[] groupColors = { Color.cyan, Color.yellow, Color.magenta, Color.green, Color.red };
        int colorIndex = 0;

        foreach (var kvp in groupsInSelection)
        {
            FormationGroup group = kvp.Key;
            List<Unit> unitsInGroup = kvp.Value;

            // Grupo - desenhar com cor específica
            Color groupColor = groupColors[colorIndex % groupColors.Length];
            colorIndex++;

            Gizmos.color = groupColor;

            // Desenhar centro do grupo
            if (group.LastCenter != Vector3.zero)
            {
                Gizmos.DrawWireSphere(group.LastCenter, 1.5f);
                Gizmos.DrawLine(group.LastCenter, group.LastCenter + Vector3.up * 3f);
            }

            // Desenhar unidades do grupo
            foreach (var unit in unitsInGroup)
            {
                Gizmos.DrawWireSphere(unit.transform.position, 0.7f);
            }

            // Label do grupo
            UnityEditor.Handles.color = groupColor;
            Vector3 labelPos = group.LastCenter != Vector3.zero ? group.LastCenter : GetAveragePosition(unitsInGroup);
            UnityEditor.Handles.Label(labelPos + Vector3.up * 3.5f,
                $"GRUPO {group.GroupID}\n{group.Formation}\n{unitsInGroup.Count}/{group.UnitCount} unidades selecionadas");
        }

        // Desenhar unidades soltas (sem grupo)
        if (unitsWithoutGroup != null && unitsWithoutGroup.Count > 0)
        {
            Gizmos.color = Color.white;
            foreach (var unit in unitsWithoutGroup)
            {
                Gizmos.DrawWireSphere(unit.transform.position, 0.5f);
            }

            Vector3 center = GetAveragePosition(unitsWithoutGroup);
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(center + Vector3.up * 2,
                $"Unidades Soltas\n{unitsWithoutGroup.Count} unidades\nFormação: None");
        }

        // Desenhar último comando
        if (lastCommandPosition != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(lastCommandPosition, 0.5f);
            UnityEditor.Handles.color = Color.magenta;
            UnityEditor.Handles.Label(lastCommandPosition + Vector3.up,
                "Último Click");
        }
    }

    [ContextMenu("Debug: Show Groups In Selection")]
    void DebugShowGroupsInSelection()
    {
        if (selectionManager == null || selectionManager.Count == 0)
        {
            Debug.Log("Nenhuma unidade selecionada");
            return;
        }

        selectedUnitsList.Clear();
        foreach (var unit in selectionManager.Selection)
        {
            if (unit != null)
                selectedUnitsList.Add(unit);
        }

        FormationGroupManager.Instance.DebugLogSelectionGroups(selectedUnitsList);
    }

    [ContextMenu("Debug: Test Formation None")]
    void DebugTestFormationNone()
    {
        SetFormation(FormationType.None);
    }

    [ContextMenu("Debug: Test Formation Line")]
    void DebugTestFormationLine()
    {
        SetFormation(FormationType.Line);
    }

    [ContextMenu("Debug: Test Formation Column")]
    void DebugTestFormationColumn()
    {
        SetFormation(FormationType.Column);
    }
#endif
}