using System;
using UnityEngine;

/// <summary>
/// Classe base de unidade EXPANDIDA
/// VERSÃO 3.0 - Adicionado integração com FormationGroup
/// </summary>
[DisallowMultipleComponent]
public class Unit : MonoBehaviour
{
    [Header("Dados")]
    public UnitDefinition def;
    public FactionId owner = FactionId.Player1;

    [Header("Stats Atuais (Runtime)")]
    [Tooltip("HP atual (modificado em combate)")]
    public float hp = 100;

    [Tooltip("HP máximo (pode aumentar com upgrades)")]
    public float hpMax = 100;

    [Tooltip("Ataque atual (pode ter buffs/debuffs)")]
    public float attack = 10;

    [Tooltip("Defesa atual (pode ter buffs/debuffs)")]
    public float defense = 5;

    [Header("Seleção (visuais)")]
    [SerializeField] GameObject selectionHighlight;

    [Header("Progressão")]
    [SerializeField] int level = 1;
    [SerializeField] float xp = 0f;
    [SerializeField] float baseXpToLevel = 100f;
    [SerializeField] float xpGrowth = 1.35f;

    public bool IsSelected { get; private set; }

    // ========== PROPRIEDADES ==========

    public int Level => level;
    public float Xp => xp;
    public float XpToNext => baseXpToLevel * Mathf.Pow(xpGrowth, Mathf.Max(0, level - 1));
    public float Xp01 => XpToNext <= 0f ? 0f : Mathf.Clamp01(xp / XpToNext);
    public string DisplayName => def ? def.displayName : gameObject.name;

    /// <summary>
    /// Velocidade máxima da unidade (vem do UnitDefinition)
    /// </summary>
    public float MaxSpeed => def ? def.moveSpeed : 3.5f;

    /// <summary>
    /// Prioridade de formação da unidade
    /// </summary>
    public FormationPriority FormationPriority => def ? def.formationPriority : FormationPriority.Medium;

    /// <summary>
    /// HP normalizado (0-1) para barras de vida
    /// </summary>
    public float HpPercent => hpMax > 0 ? Mathf.Clamp01(hp / hpMax) : 0f;

    // ========== FORMAÇÃO (NOVO - Sistema de Grupos) ==========

    /// <summary>
    /// Grupo de formação ao qual esta unidade pertence
    /// Retorna null se a unidade não estiver em nenhum grupo (formação None/livre)
    /// Esta propriedade é gerenciada automaticamente pelo FormationGroupManager
    /// </summary>
    public FormationGroup CurrentFormationGroup
    {
        get
        {
            if (FormationGroupManager.Instance == null)
                return null;

            return FormationGroupManager.Instance.GetGroupForUnit(this);
        }
    }

    /// <summary>
    /// Verifica se esta unidade está em algum grupo de formação
    /// </summary>
    public bool IsInFormationGroup => CurrentFormationGroup != null;

    /// <summary>
    /// Retorna a formação atual desta unidade (do grupo ou None se estiver solta)
    /// </summary>
    public FormationType CurrentFormation
    {
        get
        {
            var group = CurrentFormationGroup;
            return group != null ? group.Formation : FormationType.None;
        }
    }

    // ========== LIFECYCLE ==========

    private void OnEnable()
    {
        UnitRegistry.Register(this);
        GameEvents.RaiseUnitSpawned(this);

        // Inicializar stats do Definition se existir
        if (def != null)
        {
            InitializeFromDefinition();
        }
    }

    private void OnDisable()
    {
        UnitRegistry.Unregister(this);
        GameEvents.RaiseUnitDespawned(this);
    }

    // ========== MÉTODOS PÚBLICOS ==========

    /// <summary>
    /// Inicializa stats baseado no UnitDefinition
    /// Chamado automaticamente no OnEnable, mas pode ser chamado manualmente
    /// </summary>
    public void InitializeFromDefinition()
    {
        if (def == null) return;

        hpMax = def.maxHp;
        hp = hpMax;  // Começa com HP cheio
        attack = def.attack;
        defense = def.defense;
    }

    public void SetSelected(bool value)
    {
        if (IsSelected == value) return;
        IsSelected = value;
        if (selectionHighlight) selectionHighlight.SetActive(value);
        GameEvents.RaiseUnitSelectionChanged(this, value);
    }

    public void AddXp(float amount)
    {
        if (amount <= 0f) return;

        xp += amount;
        var guard = 64;

        while (xp >= XpToNext && guard-- > 0)
        {
            xp -= XpToNext;
            level++;
        }

        GameEvents.RaiseUnitProgressChanged(this);
    }

    /// <summary>
    /// Aplica dano à unidade
    /// </summary>
    public void TakeDamage(float damage)
    {
        float actualDamage = Mathf.Max(1, damage - defense); // Mínimo 1 de dano
        hp = Mathf.Max(0, hp - actualDamage);

        if (hp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Cura a unidade
    /// </summary>
    public void Heal(float amount)
    {
        hp = Mathf.Min(hpMax, hp + amount);
    }

    /// <summary>
    /// Verifica se a unidade está viva
    /// </summary>
    public bool IsAlive => hp > 0;

    /// <summary>
    /// Morte da unidade (expandir no futuro com animações, drops, etc)
    /// MODIFICADO: Agora notifica o sistema de grupos
    /// </summary>
    void Die()
    {
        // Notificar sistema de grupos antes de destruir
        GameEvents.RaiseUnitDied(this);

        // TODO: Animação de morte, drop de itens, XP para quem matou
        Debug.Log($"{DisplayName} morreu!");
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    public void TestList()
    {
        foreach (var u in UnitRegistry.All)
            Debug.Log($" - {u.DisplayName} (dono {u.owner})");
    }

    /// <summary>
    /// Debug: Mostra informações de formação no Inspector
    /// </summary>
    [ContextMenu("Debug: Show Formation Info")]
    void DebugShowFormationInfo()
    {
        if (CurrentFormationGroup != null)
        {
            Debug.Log($"=== {DisplayName} ===\n" +
                     $"Formation Group ID: {CurrentFormationGroup.GroupID}\n" +
                     $"Formation: {CurrentFormationGroup.Formation}\n" +
                     $"Units in Group: {CurrentFormationGroup.UnitCount}\n" +
                     $"Group Center: {CurrentFormationGroup.LastCenter}");
        }
        else
        {
            Debug.Log($"=== {DisplayName} ===\n" +
                     $"Formation: None (unidade solta)");
        }
    }
#endif
}