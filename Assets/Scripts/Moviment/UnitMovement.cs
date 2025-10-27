using UnityEngine;
using Pathfinding;

/// <summary>
/// Controla a movimentação de uma unidade individual usando A* Pathfinding RichAI
/// Integra com o sistema de formações e comandos
/// </summary>
[RequireComponent(typeof(Unit))]
[RequireComponent(typeof(Seeker))]
public class UnitMovement : MonoBehaviour
{
    [Header("Componentes")]
    [SerializeField] private Unit unit;
    [SerializeField] private Seeker seeker;
    [SerializeField] private RichAI richAI;

    [Header("Estado de Movimento")]
    [SerializeField] private bool isMoving = false;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private Vector3 formationOffset = Vector3.zero;

    [Header("Configuração")]
    [Tooltip("Distância para considerar que chegou ao destino")]
    [SerializeField] private float arrivalDistance = 0.5f;

    // Estado interno
    private bool hasReachedFormationPosition = false;

    // ========== PROPRIEDADES ==========

    /// <summary>
    /// Posição atual no mundo
    /// </summary>
    public Vector3 Position => transform.position;

    /// <summary>
    /// Está atualmente se movendo?
    /// Verifica tanto a flag interna quanto o estado do RichAI
    /// </summary>
    public bool IsMoving
    {
        get
        {
            if (!isMoving) return false;
            if (richAI == null) return false;

            // Se não tem path, não está se movendo
            if (!richAI.hasPath) return false;

            // Se já chegou ao destino, não está mais se movendo
            if (richAI.reachedDestination) return false;

            return true;
        }
    }

    /// <summary>
    /// A unidade está parada? (oposto de IsMoving)
    /// </summary>
    public bool IsIdle => !IsMoving;

    /// <summary>
    /// Destino final (sem offset de formação)
    /// </summary>
    public Vector3 TargetPosition => targetPosition;

    /// <summary>
    /// Offset de formação atribuído a esta unidade
    /// </summary>
    public Vector3 FormationOffset
    {
        get => formationOffset;
        set => formationOffset = value;
    }

    /// <summary>
    /// Destino real considerando formação (targetPosition + formationOffset)
    /// </summary>
    public Vector3 FinalDestination => targetPosition + formationOffset;

    // ========== LIFECYCLE ==========

    void Reset()
    {
        unit = GetComponent<Unit>();
        seeker = GetComponent<Seeker>();
        richAI = GetComponent<RichAI>();
    }

    void Awake()
    {
        // Garantir que componentes estão atribuídos
        if (!unit) unit = GetComponent<Unit>();
        if (!seeker) seeker = GetComponent<Seeker>();
        if (!richAI) richAI = GetComponent<RichAI>();

        // Se RichAI não existe, adicionar e configurar
        if (richAI == null)
        {
            richAI = gameObject.AddComponent<RichAI>();
            ConfigureRichAI();
        }
    }

    void Start()
    {
        // Aplicar velocidade do UnitDefinition ao RichAI
        if (unit != null && unit.def != null && richAI != null)
        {
            richAI.maxSpeed = unit.MaxSpeed;
            richAI.acceleration = unit.def.acceleration;
            richAI.rotationSpeed = unit.def.rotationSpeed;
        }
    }

    void Update()
    {
        // Verificar se chegou ao destino
        if (isMoving && richAI != null)
        {
            // Usar a propriedade do RichAI para verificar se chegou
            if (richAI.reachedDestination || richAI.reachedEndOfPath)
            {
                OnReachedDestination();
            }
            else
            {
                // Fallback: verificar distância manual (mais preciso)
                float distanceToGoal = Vector3.Distance(Position, FinalDestination);

                if (distanceToGoal <= arrivalDistance)
                {
                    OnReachedDestination();
                }
            }
        }
    }

    /// <summary>
    /// Chamado quando a unidade chega ao destino
    /// </summary>
    void OnReachedDestination()
    {
        if (!hasReachedFormationPosition) // Só dispara uma vez
        {
            hasReachedFormationPosition = true;
            isMoving = false;

            Debug.Log($"[{unit.DisplayName}] Chegou ao destino {FinalDestination}!");

            // Opcional: Disparar evento via GameEvents (para UI, etc)
            // GameEvents.RaiseUnitArrivedAtDestination(unit);
        }
    }

    // ========== COMANDOS DE MOVIMENTO ==========

    /// <summary>
    /// Move a unidade para uma posição específica (sem formação)
    /// </summary>
    /// <param name="destination">Posição no mundo para onde ir</param>
    public void MoveTo(Vector3 destination)
    {
        MoveToWithOffset(destination, Vector3.zero);
    }

    /// <summary>
    /// Move a unidade para uma posição considerando offset de formação
    /// </summary>
    /// <param name="destination">Posição base do grupo</param>
    /// <param name="offset">Offset de formação relativo ao centro</param>
    public void MoveToWithOffset(Vector3 destination, Vector3 offset)
    {
        if (richAI == null)
        {
            Debug.LogWarning($"[{unit.DisplayName}] RichAI não encontrado!");
            return;
        }

        targetPosition = destination;
        formationOffset = offset;
        hasReachedFormationPosition = false;
        isMoving = true;

        // Definir destino no RichAI
        richAI.destination = FinalDestination;
        richAI.isStopped = false;

        Debug.Log($"<color=cyan>[{unit.DisplayName}] Movendo para {FinalDestination} (base: {destination}, offset: {offset})</color>");
    }

    /// <summary>
    /// Para o movimento imediatamente
    /// </summary>
    public void Stop()
    {
        if (richAI != null)
        {
            richAI.isStopped = true;
        }

        isMoving = false;
        hasReachedFormationPosition = false;

        Debug.Log($"<color=yellow>[{unit.DisplayName}] Parado manualmente</color>");
    }

    /// <summary>
    /// Atualiza apenas o offset de formação (mantém o mesmo targetPosition)
    /// Útil quando a formação muda mas o destino final é o mesmo
    /// </summary>
    /// <param name="newOffset">Novo offset de formação</param>
    public void UpdateFormationOffset(Vector3 newOffset)
    {
        if (!isMoving) return;

        formationOffset = newOffset;
        hasReachedFormationPosition = false;

        // Atualizar destino do RichAI
        if (richAI != null)
        {
            richAI.destination = FinalDestination;
        }
    }

    // ========== CONFIGURAÇÃO ==========

    /// <summary>
    /// Configura o RichAI com valores padrão para RTS
    /// </summary>
    void ConfigureRichAI()
    {
        if (richAI == null) return;

        // Configurações básicas de velocidade e aceleração
        richAI.maxSpeed = 3.5f;  // Será sobrescrito pelo UnitDefinition no Start
        richAI.acceleration = 8f;
        richAI.rotationSpeed = 120f;

        // Configuração de chegada ao destino
        richAI.endReachedDistance = arrivalDistance;
        richAI.slowdownTime = 0.5f;  // Tempo para desacelerar antes de chegar

        // Gravidade - usar configurações do projeto Unity
        richAI.gravity = Vector3.down * 9.81f;

        // Rotação habilitada (rotaciona para a direção do movimento)
        richAI.enableRotation = true;

        // Configurações de parede (útil para evitar colisões)
        richAI.wallDist = 1f;
        richAI.wallForce = 3f;

        // Usar simplificação de funil (melhora os caminhos)
        richAI.funnelSimplification = true;

        Debug.Log($"[UnitMovement] RichAI configurado para {gameObject.name}");
    }

    /// <summary>
    /// Atualiza a velocidade da unidade (útil para buffs/debuffs)
    /// </summary>
    /// <param name="newSpeed">Nova velocidade máxima</param>
    public void SetSpeed(float newSpeed)
    {
        if (richAI != null)
        {
            richAI.maxSpeed = newSpeed;
        }
    }

    /// <summary>
    /// Retorna a velocidade atual da unidade
    /// </summary>
    public float GetCurrentSpeed()
    {
        return richAI != null ? richAI.velocity.magnitude : 0f;
    }

    // ========== DEBUG ==========

    /// <summary>
    /// Método de debug para forçar movimento (útil para testar no Inspector)
    /// </summary>
    [ContextMenu("Debug: Move Forward 10 units")]
    void DebugMoveForward()
    {
        Vector3 forwardPos = transform.position + transform.forward * 10f;
        MoveTo(forwardPos);
    }

    /// <summary>
    /// Mostra status atual no console
    /// </summary>
    [ContextMenu("Debug: Show Status")]
    void DebugShowStatus()
    {
        Debug.Log($"=== [{unit.DisplayName}] STATUS ===\n" +
                  $"IsMoving: {IsMoving}\n" +
                  $"IsIdle: {IsIdle}\n" +
                  $"Position: {Position}\n" +
                  $"Target: {TargetPosition}\n" +
                  $"Final Dest: {FinalDestination}\n" +
                  $"Distance: {Vector3.Distance(Position, FinalDestination):F2}\n" +
                  $"RichAI.hasPath: {(richAI ? richAI.hasPath.ToString() : "null")}\n" +
                  $"RichAI.reachedDestination: {(richAI ? richAI.reachedDestination.ToString() : "null")}\n" +
                  $"RichAI.isStopped: {(richAI ? richAI.isStopped.ToString() : "null")}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!isMoving) return;

        // Desenhar linha para o destino base
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.5f);

        // Desenhar linha para o destino com formação
        if (formationOffset != Vector3.zero)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, FinalDestination);
            Gizmos.DrawWireSphere(FinalDestination, arrivalDistance);

            // Linha do offset
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetPosition, FinalDestination);
        }

        // Desenhar círculo de chegada
        Gizmos.color = hasReachedFormationPosition ? Color.green : Color.red;
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.DrawWireDisc(FinalDestination, Vector3.up, arrivalDistance);

        // Texto de status
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2,
            $"{unit.DisplayName}\n" +
            $"Moving: {IsMoving}\n" +
            $"Dist: {Vector3.Distance(Position, FinalDestination):F1}m"
        );
    }
#endif
}