using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LOCALIZAÇÃO: Scripts/Core/FormationGroup.cs
/// 
/// Representa um grupo de unidades que compartilham a mesma formação
/// Grupos são criados automaticamente quando unidades se movem juntas
/// ou quando uma formação é atribuída a múltiplas unidades
/// 
/// REGRAS:
/// - Cada unidade pode estar em apenas 1 grupo por vez (ou em nenhum)
/// - Unidades sem grupo têm formação None (movimento livre)
/// - Grupos são destruídos automaticamente quando ficam vazios
/// - Mescla de grupos: formação do grupo maior prevalece
/// - Empate na mescla: menor valor no enum FormationType
/// 
/// VERSÃO: 1.0
/// DATA: 26/10/2025
/// </summary>
public class FormationGroup
{
    // ========== IDENTIFICAÇÃO ==========

    /// <summary>
    /// ID único do grupo (auto-incrementado)
    /// </summary>
    public int GroupID { get; private set; }

    /// <summary>
    /// Formação atual do grupo
    /// </summary>
    public FormationType Formation { get; set; }

    // ========== UNIDADES ==========

    /// <summary>
    /// Unidades que pertencem a este grupo
    /// Usa HashSet para busca O(1) e garantir unicidade
    /// </summary>
    private HashSet<Unit> units = new HashSet<Unit>();

    /// <summary>
    /// Quantidade de unidades no grupo
    /// </summary>
    public int UnitCount => units.Count;

    /// <summary>
    /// Verifica se o grupo está vazio
    /// </summary>
    public bool IsEmpty => units.Count == 0;

    /// <summary>
    /// Acesso read-only às unidades (para iteração)
    /// </summary>
    public IReadOnlyCollection<Unit> Units => units;

    // ========== ESTADO ==========

    /// <summary>
    /// Centro atual da formação (última posição de movimento)
    /// Usado para manter coesão do grupo
    /// </summary>
    public Vector3 LastCenter { get; set; }

    /// <summary>
    /// Timestamp de quando o grupo foi criado (para debug/analytics)
    /// </summary>
    public float CreationTime { get; private set; }

    /// <summary>
    /// Timestamp da última movimentação do grupo
    /// </summary>
    public float LastMoveTime { get; set; }

    // ========== CONSTRUTOR ==========

    /// <summary>
    /// Cria um novo grupo de formação
    /// </summary>
    /// <param name="groupID">ID único do grupo</param>
    /// <param name="formation">Formação inicial</param>
    /// <param name="initialUnits">Unidades iniciais (opcional)</param>
    public FormationGroup(int groupID, FormationType formation, IEnumerable<Unit> initialUnits = null)
    {
        GroupID = groupID;
        Formation = formation;
        CreationTime = Time.time;
        LastMoveTime = Time.time;
        LastCenter = Vector3.zero;

        if (initialUnits != null)
        {
            foreach (var unit in initialUnits)
            {
                AddUnit(unit);
            }
        }
    }

    // ========== GERENCIAMENTO DE UNIDADES ==========

    /// <summary>
    /// Adiciona uma unidade ao grupo
    /// </summary>
    /// <returns>True se adicionou com sucesso, False se já estava no grupo</returns>
    public bool AddUnit(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogWarning("[FormationGroup] Tentativa de adicionar unidade nula");
            return false;
        }

        bool added = units.Add(unit);

        if (added)
        {
            Debug.Log($"<color=cyan>[FormationGroup {GroupID}] Unidade {unit.DisplayName} adicionada. Total: {UnitCount}</color>");
        }

        return added;
    }

    /// <summary>
    /// Remove uma unidade do grupo
    /// </summary>
    /// <returns>True se removeu com sucesso, False se não estava no grupo</returns>
    public bool RemoveUnit(Unit unit)
    {
        if (unit == null)
            return false;

        bool removed = units.Remove(unit);

        if (removed)
        {
            Debug.Log($"<color=yellow>[FormationGroup {GroupID}] Unidade {unit.DisplayName} removida. Total: {UnitCount}</color>");
        }

        return removed;
    }

    /// <summary>
    /// Verifica se uma unidade pertence a este grupo
    /// </summary>
    public bool ContainsUnit(Unit unit)
    {
        return unit != null && units.Contains(unit);
    }

    /// <summary>
    /// Remove todas as unidades do grupo
    /// </summary>
    public void Clear()
    {
        int count = units.Count;
        units.Clear();
        Debug.Log($"<color=orange>[FormationGroup {GroupID}] Todas as unidades removidas ({count} unidades)</color>");
    }

    /// <summary>
    /// Adiciona múltiplas unidades de uma vez
    /// </summary>
    public int AddUnits(IEnumerable<Unit> unitsToAdd)
    {
        int addedCount = 0;

        foreach (var unit in unitsToAdd)
        {
            if (AddUnit(unit))
                addedCount++;
        }

        return addedCount;
    }

    /// <summary>
    /// Remove múltiplas unidades de uma vez
    /// </summary>
    public int RemoveUnits(IEnumerable<Unit> unitsToRemove)
    {
        int removedCount = 0;

        foreach (var unit in unitsToRemove)
        {
            if (RemoveUnit(unit))
                removedCount++;
        }

        return removedCount;
    }

    // ========== UTILITÁRIOS ==========

    /// <summary>
    /// Copia as unidades para uma lista (para iteração segura)
    /// </summary>
    public List<Unit> GetUnitsList()
    {
        return new List<Unit>(units);
    }

    /// <summary>
    /// Calcula o centro geométrico das unidades do grupo
    /// </summary>
    public Vector3 CalculateCenter()
    {
        if (units.Count == 0)
            return Vector3.zero;

        Vector3 sum = Vector3.zero;
        int validCount = 0;

        foreach (var unit in units)
        {
            if (unit != null)
            {
                sum += unit.transform.position;
                validCount++;
            }
        }

        return validCount > 0 ? sum / validCount : Vector3.zero;
    }

    /// <summary>
    /// Atualiza o centro baseado nas posições atuais das unidades
    /// </summary>
    public void UpdateCenter()
    {
        LastCenter = CalculateCenter();
    }

    // ========== DEBUG ==========

    /// <summary>
    /// Retorna representação em string para debug
    /// </summary>
    public override string ToString()
    {
        return $"FormationGroup[ID={GroupID}, Formation={Formation}, Units={UnitCount}, Center={LastCenter}]";
    }

    /// <summary>
    /// Log detalhado do estado do grupo
    /// </summary>
    public void DebugLog()
    {
        Debug.Log($"=== FORMATION GROUP {GroupID} ===\n" +
                 $"Formation: {Formation}\n" +
                 $"Units: {UnitCount}\n" +
                 $"Last Center: {LastCenter}\n" +
                 $"Creation Time: {CreationTime}\n" +
                 $"Last Move: {LastMoveTime}\n" +
                 $"Age: {Time.time - CreationTime:F1}s");

        if (units.Count > 0)
        {
            Debug.Log($"Units in group: {string.Join(", ", System.Linq.Enumerable.Select(units, u => u.DisplayName))}");
        }
    }

    /// <summary>
    /// Valida a integridade do grupo (remove unidades nulas/mortas)
    /// </summary>
    public int ValidateAndCleanup()
    {
        var toRemove = new List<Unit>();

        foreach (var unit in units)
        {
            // Remover unidades nulas ou que foram destruídas
            if (unit == null || unit.gameObject == null)
            {
                toRemove.Add(unit);
            }
        }

        foreach (var unit in toRemove)
        {
            units.Remove(unit);
        }

        if (toRemove.Count > 0)
        {
            Debug.LogWarning($"<color=orange>[FormationGroup {GroupID}] Limpeza: {toRemove.Count} unidades inválidas removidas</color>");
        }

        return toRemove.Count;
    }
}