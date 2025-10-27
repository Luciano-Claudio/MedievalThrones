using UnityEngine;

/// <summary>
/// Definição de unidade expandida com stats de movimento, combate e prioridade
/// VERSÃO 2.0 - Suporte para sistema de movimentação e formações
/// </summary>
[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Identificação")]
    public string displayName;
    public UnitType type;
    public Sprite icon;

    [Header("Stats de Combate")]
    [Tooltip("Vida máxima da unidade")]
    public float maxHp = 100f;

    [Tooltip("Ataque base da unidade")]
    public float attack = 10f;

    [Tooltip("Defesa base da unidade")]
    public float defense = 5f;

    [Header("Stats de Movimento")]
    [Tooltip("Velocidade máxima em unidades/segundo")]
    public float moveSpeed = 3.5f;

    [Tooltip("Aceleração da unidade")]
    public float acceleration = 8f;

    [Tooltip("Velocidade de rotação em graus/segundo")]
    public float rotationSpeed = 120f;

    [Header("Formação")]
    [Tooltip("Prioridade na formação (maior = mais à frente)")]
    public FormationPriority formationPriority = FormationPriority.Medium;

    [Tooltip("Espaçamento preferido entre unidades do mesmo tipo")]
    public float spacing = 2f;

    [Header("Visão e Detecção")]
    [Tooltip("Alcance de visão da unidade")]
    public float visionRange = 10f;

    /// <summary>
    /// Retorna o valor numérico da prioridade para cálculos de ordenação
    /// </summary>
    public int GetPriorityValue() => (int)formationPriority;

    /// <summary>
    /// Helper para configuração rápida de stats baseado no tipo
    /// (Pode ser usado no Inspector via botão customizado)
    /// </summary>
    [ContextMenu("AutoConfigureByType")]
    public void AutoConfigureByType()
    {
        switch (type)
        {
            case UnitType.Hero:
                formationPriority = FormationPriority.Maximum;
                moveSpeed = 4.5f;
                attack = 30f;
                defense = 20f;
                maxHp = 200f;
                break;

            case UnitType.Warrior:
                formationPriority = FormationPriority.VeryHigh;
                moveSpeed = 3.5f;
                attack = 20f;
                defense = 15f;
                maxHp = 150f;
                break;

            case UnitType.Spearman:
                formationPriority = FormationPriority.High;
                moveSpeed = 3.2f;
                attack = 15f;
                defense = 12f;
                maxHp = 120f;
                break;

            case UnitType.Archer:
                formationPriority = FormationPriority.Medium;
                moveSpeed = 3.0f;
                attack = 12f;
                defense = 5f;
                maxHp = 80f;
                break;

            case UnitType.CavalryLight:
                formationPriority = FormationPriority.Medium;
                moveSpeed = 6.0f;  // Cavalry é mais rápida
                attack = 18f;
                defense = 8f;
                maxHp = 100f;
                break;

            case UnitType.Ram:
            case UnitType.Catapult:
                formationPriority = FormationPriority.Low;
                moveSpeed = 2.0f;  // Siege é lenta
                attack = 40f;  // Mas ataque alto
                defense = 10f;
                maxHp = 200f;
                break;

            case UnitType.Worker:
                formationPriority = FormationPriority.VeryLow;
                moveSpeed = 3.0f;
                attack = 5f;
                defense = 3f;
                maxHp = 60f;
                break;
        }
    }
}