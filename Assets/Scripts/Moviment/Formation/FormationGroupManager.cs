using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Gerenciador central de grupos de formação
/// Responsável por criar, mesclar, atualizar e destruir grupos automaticamente
/// 
/// SINGLETON PATTERN - Acesso global via FormationGroupManager.Instance
/// 
/// LÓGICA PRINCIPAL:
/// 1. Unidades sem grupo = Formação None (movimento livre)
/// 2. Ao mover unidades com formação → cria/atualiza grupo
/// 3. Ao mover subconjunto de grupo → cria novo grupo
/// 4. Ao mesclar seleção com múltiplos grupos → grupo maior prevalece
/// 5. Grupos vazios são destruídos automaticamente
/// 
/// VERSÃO: 1.0
/// DATA: 26/10/2025
/// </summary>
public class FormationGroupManager : MonoBehaviour
{
    // ========== SINGLETON ==========

    private static FormationGroupManager instance;

    public static FormationGroupManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<FormationGroupManager>();

                if (instance == null)
                {
                    GameObject go = new GameObject("FormationGroupManager");
                    instance = go.AddComponent<FormationGroupManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    // ========== ESTADO ==========

    /// <summary>
    /// Todos os grupos ativos, indexados por ID
    /// </summary>
    private Dictionary<int, FormationGroup> groups = new Dictionary<int, FormationGroup>();

    /// <summary>
    /// Mapeamento rápido: Unidade → Grupo
    /// Permite saber instantaneamente a qual grupo uma unidade pertence
    /// </summary>
    private Dictionary<Unit, FormationGroup> unitToGroup = new Dictionary<Unit, FormationGroup>();

    /// <summary>
    /// Contador para gerar IDs únicos de grupos
    /// </summary>
    private int nextGroupID = 1;

    /// <summary>
    /// Total de grupos ativos
    /// </summary>
    public int ActiveGroupCount => groups.Count;

    /// <summary>
    /// Total de unidades em grupos (não inclui unidades soltas)
    /// </summary>
    public int TotalUnitsInGroups => unitToGroup.Count;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // ========== LIFECYCLE ==========

    void Awake()
    {
        // Garantir singleton
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (showDebugLogs)
            Debug.Log("<color=lime>[FormationGroupManager] Inicializado</color>");
    }

    void OnEnable()
    {
        // Subscrever eventos de morte de unidades
        GameEvents.OnUnitDied += OnUnitDied;
    }

    void OnDisable()
    {
        // Desinscrever eventos
        GameEvents.OnUnitDied -= OnUnitDied;
    }

    void Update()
    {
        // Limpeza periódica de grupos vazios ou inválidos
        // Executar a cada segundo para não impactar performance
        if (Time.frameCount % 60 == 0) // ~1 segundo a 60 FPS
        {
            CleanupInvalidGroups();
        }
    }

    // ========== CRIAÇÃO DE GRUPOS ==========

    /// <summary>
    /// Cria um novo grupo com as unidades especificadas
    /// Remove unidades de grupos anteriores automaticamente
    /// </summary>
    /// <param name="units">Unidades para o novo grupo</param>
    /// <param name="formation">Formação do grupo</param>
    /// <returns>Grupo criado ou null se formação for None</returns>
    public FormationGroup CreateGroup(IEnumerable<Unit> units, FormationType formation)
    {
        // None não cria grupos
        if (formation == FormationType.None)
        {
            // Remover unidades de grupos existentes
            foreach (var unit in units)
            {
                RemoveUnitFromAnyGroup(unit);
            }

            if (showDebugLogs)
                Debug.Log("<color=yellow>[FormationGroupManager] Formação None - nenhum grupo criado</color>");

            return null;
        }

        // Criar novo grupo
        int groupID = nextGroupID++;
        FormationGroup newGroup = new FormationGroup(groupID, formation);

        // Adicionar unidades ao grupo
        foreach (var unit in units)
        {
            if (unit != null)
            {
                // Remover de grupo anterior se existir
                RemoveUnitFromAnyGroup(unit);

                // Adicionar ao novo grupo
                newGroup.AddUnit(unit);
                unitToGroup[unit] = newGroup;
            }
        }

        // Registrar grupo
        groups[groupID] = newGroup;

        if (showDebugLogs)
            Debug.Log($"<color=lime>[FormationGroupManager] Grupo {groupID} criado: {formation}, {newGroup.UnitCount} unidades</color>");

        return newGroup;
    }

    // ========== CONSULTAS ==========

    /// <summary>
    /// Retorna o grupo ao qual a unidade pertence (ou null se não estiver em nenhum)
    /// </summary>
    public FormationGroup GetGroupForUnit(Unit unit)
    {
        if (unit == null)
            return null;

        unitToGroup.TryGetValue(unit, out FormationGroup group);
        return group;
    }

    /// <summary>
    /// Verifica se uma unidade está em algum grupo
    /// </summary>
    public bool IsUnitInGroup(Unit unit)
    {
        return unit != null && unitToGroup.ContainsKey(unit);
    }

    /// <summary>
    /// Retorna a formação da unidade (do grupo ou None se não estiver em grupo)
    /// </summary>
    public FormationType GetFormationForUnit(Unit unit)
    {
        var group = GetGroupForUnit(unit);
        return group != null ? group.Formation : FormationType.None;
    }

    /// <summary>
    /// Retorna todos os grupos ativos
    /// </summary>
    public IEnumerable<FormationGroup> GetAllGroups()
    {
        return groups.Values;
    }

    /// <summary>
    /// Retorna grupo por ID
    /// </summary>
    public FormationGroup GetGroupByID(int groupID)
    {
        groups.TryGetValue(groupID, out FormationGroup group);
        return group;
    }

    // ========== DETECÇÃO DE GRUPOS NA SELEÇÃO ==========

    /// <summary>
    /// Analisa uma seleção de unidades e retorna os grupos envolvidos
    /// </summary>
    /// <param name="selectedUnits">Unidades selecionadas</param>
    /// <param name="unitsWithoutGroup">OUT: Lista de unidades sem grupo (se houver)</param>
    /// <returns>Dictionary: Grupo → Lista de unidades desse grupo na seleção</returns>
    public Dictionary<FormationGroup, List<Unit>> GetGroupsInSelection(IEnumerable<Unit> selectedUnits, out List<Unit> unitsWithoutGroup)
    {
        var result = new Dictionary<FormationGroup, List<Unit>>();
        unitsWithoutGroup = new List<Unit>();

        foreach (var unit in selectedUnits)
        {
            if (unit == null) continue;

            var group = GetGroupForUnit(unit);

            if (group != null)
            {
                // Unidade tem grupo
                if (!result.ContainsKey(group))
                    result[group] = new List<Unit>();

                result[group].Add(unit);
            }
            else
            {
                // Unidade sem grupo
                unitsWithoutGroup.Add(unit);
            }
        }

        return result;
    }

    /// <summary>
    /// Determina qual formação usar quando há múltiplos grupos na seleção
    /// REGRA: Grupo com mais unidades vence. Empate: menor valor no enum.
    /// </summary>
    public FormationType DetermineFormationForSelection(IEnumerable<Unit> selectedUnits)
    {
        var groupsInSelection = GetGroupsInSelection(selectedUnits, out List<Unit> unitsWithoutGroup);

        if (groupsInSelection.Count == 0)
            return FormationType.None;

        // Encontrar grupo com mais unidades
        FormationGroup largestGroup = null;
        int maxUnitsCount = 0;

        foreach (var kvp in groupsInSelection)
        {
            FormationGroup group = kvp.Key;
            int unitsInSelection = kvp.Value.Count;

            if (unitsInSelection > maxUnitsCount)
            {
                maxUnitsCount = unitsInSelection;
                largestGroup = group;
            }
            else if (unitsInSelection == maxUnitsCount && largestGroup != null)
            {
                // Empate: escolher formação com menor valor no enum
                if ((int)group.Formation < (int)largestGroup.Formation)
                {
                    largestGroup = group;
                }
            }
        }

        return largestGroup != null ? largestGroup.Formation : FormationType.None;
    }

    // ========== ATUALIZAÇÃO DE GRUPOS ==========

    /// <summary>
    /// Atualiza a formação de um grupo existente
    /// </summary>
    public void UpdateGroupFormation(FormationGroup group, FormationType newFormation)
    {
        if (group == null)
            return;

        FormationType oldFormation = group.Formation;
        group.Formation = newFormation;

        if (showDebugLogs)
            Debug.Log($"<color=cyan>[FormationGroupManager] Grupo {group.GroupID}: {oldFormation} → {newFormation}</color>");
    }

    /// <summary>
    /// Remove uma unidade de qualquer grupo que ela pertença
    /// </summary>
    public void RemoveUnitFromAnyGroup(Unit unit)
    {
        if (unit == null)
            return;

        var group = GetGroupForUnit(unit);
        if (group != null)
        {
            group.RemoveUnit(unit);
            unitToGroup.Remove(unit);

            // Destruir grupo se ficou vazio
            if (group.IsEmpty)
            {
                DestroyGroup(group.GroupID);
            }
        }
    }

    // ========== MESCLA DE GRUPOS ==========

    /// <summary>
    /// Mescla múltiplos grupos em um único grupo
    /// Formação resultante: grupo com mais unidades (empate: menor enum)
    /// </summary>
    public FormationGroup MergeGroups(IEnumerable<FormationGroup> groupsToMerge, FormationType? overrideFormation = null)
    {
        var groupsList = groupsToMerge.Where(g => g != null).ToList();

        if (groupsList.Count == 0)
            return null;

        if (groupsList.Count == 1)
            return groupsList[0]; // Nada a mesclar

        // Determinar formação resultante
        FormationType resultFormation;

        if (overrideFormation.HasValue)
        {
            resultFormation = overrideFormation.Value;
        }
        else
        {
            // Grupo com mais unidades vence
            var largestGroup = groupsList.OrderByDescending(g => g.UnitCount)
                                         .ThenBy(g => (int)g.Formation)
                                         .First();
            resultFormation = largestGroup.Formation;
        }

        // Coletar todas as unidades
        var allUnits = new List<Unit>();
        foreach (var group in groupsList)
        {
            allUnits.AddRange(group.Units);
        }

        // Destruir grupos antigos
        foreach (var group in groupsList)
        {
            DestroyGroup(group.GroupID, silent: true);
        }

        // Criar novo grupo mesclado
        var mergedGroup = CreateGroup(allUnits, resultFormation);

        if (showDebugLogs)
            Debug.Log($"<color=lime>[FormationGroupManager] {groupsList.Count} grupos mesclados → Grupo {mergedGroup.GroupID} ({resultFormation}, {allUnits.Count} unidades)</color>");

        return mergedGroup;
    }

    // ========== DESTRUIÇÃO DE GRUPOS ==========

    /// <summary>
    /// Destrói um grupo e remove todas as unidades dele
    /// </summary>
    public void DestroyGroup(int groupID, bool silent = false)
    {
        if (!groups.TryGetValue(groupID, out FormationGroup group))
            return;

        // Remover todas as unidades do mapeamento
        foreach (var unit in group.Units.ToList()) // ToList para evitar modificação durante iteração
        {
            unitToGroup.Remove(unit);
        }

        // Remover grupo
        groups.Remove(groupID);

        if (!silent && showDebugLogs)
            Debug.Log($"<color=orange>[FormationGroupManager] Grupo {groupID} destruído</color>");
    }

    /// <summary>
    /// Limpa grupos vazios ou com unidades inválidas
    /// </summary>
    void CleanupInvalidGroups()
    {
        var groupsToDestroy = new List<int>();

        foreach (var group in groups.Values)
        {
            // Limpar unidades inválidas
            group.ValidateAndCleanup();

            // Marcar grupos vazios para destruição
            if (group.IsEmpty)
            {
                groupsToDestroy.Add(group.GroupID);
            }
        }

        // Destruir grupos vazios
        foreach (var groupID in groupsToDestroy)
        {
            DestroyGroup(groupID);
        }
    }

    // ========== CALLBACKS DE EVENTOS ==========

    /// <summary>
    /// Chamado quando uma unidade morre
    /// Remove a unidade de seu grupo e destrói o grupo se ficou vazio
    /// </summary>
    void OnUnitDied(Unit unit)
    {
        RemoveUnitFromAnyGroup(unit);
    }

    // ========== DEBUG ==========

    [ContextMenu("Debug: Show All Groups")]
    void DebugShowAllGroups()
    {
        Debug.Log($"=== FORMATION GROUPS ({ActiveGroupCount} ativos) ===");

        foreach (var group in groups.Values)
        {
            group.DebugLog();
        }

        Debug.Log($"Total de unidades em grupos: {TotalUnitsInGroups}");
    }

    [ContextMenu("Debug: Clear All Groups")]
    void DebugClearAllGroups()
    {
        int count = groups.Count;

        foreach (var group in groups.Values.ToList())
        {
            DestroyGroup(group.GroupID);
        }

        Debug.Log($"<color=orange>[FormationGroupManager] Todos os {count} grupos foram destruídos</color>");
    }

    public void DebugLogSelectionGroups(IEnumerable<Unit> selectedUnits)
    {
        var groupsInSelection = GetGroupsInSelection(selectedUnits, out List<Unit> unitsWithoutGroup);

        Debug.Log($"=== GRUPOS NA SELEÇÃO ===");
        Debug.Log($"Total de unidades selecionadas: {selectedUnits.Count()}");
        Debug.Log($"Grupos diferentes envolvidos: {groupsInSelection.Count}");

        foreach (var kvp in groupsInSelection)
        {
            var group = kvp.Key;
            var unitsInSelection = kvp.Value;

            Debug.Log($"  • Grupo {group.GroupID} ({group.Formation}): {unitsInSelection.Count}/{group.UnitCount} unidades");
        }

        if (unitsWithoutGroup.Count > 0)
        {
            Debug.Log($"  • Sem grupo: {unitsWithoutGroup.Count} unidades");
        }

        FormationType determinedFormation = DetermineFormationForSelection(selectedUnits);
        Debug.Log($"Formação determinada para esta seleção: {determinedFormation}");
    }
}