using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Visualizador de formações na Scene View
/// Mostra preview da formação antes de mover
/// Agora usa MovementCommandHandler ao invés de FormationManager
/// </summary>
public class FormationVisualizer : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private MovementCommandHandler movementCommandHandler;

    [Header("Configuração de Visualização")]
    [SerializeField] private bool showPreview = true;
    [SerializeField] private Color previewColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private float previewRadius = 0.4f;

    [Header("Debug")]
    [SerializeField] private Vector3 previewPosition = Vector3.zero;

    private Dictionary<Unit, Vector3> cachedFormation;
    private List<Unit> cachedUnits = new List<Unit>();
    private Camera mainCamera;

    void Awake()
    {
        if (!selectionManager)
            selectionManager = Object.FindFirstObjectByType<SelectionManager>();

        if (!movementCommandHandler)
            movementCommandHandler = Object.FindFirstObjectByType<MovementCommandHandler>();

        mainCamera = Camera.main;
    }

    void Update()
    {
        // Atualizar preview position baseado no mouse
        if (showPreview && selectionManager != null && selectionManager.Count > 0)
        {
            // Usar Mouse.current do Input System (API de baixo nível)
            if (Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                UpdatePreviewFromScreenPosition(mousePosition);
            }
        }
    }

    /// <summary>
    /// Atualiza o preview baseado em uma posição de tela
    /// </summary>
    void UpdatePreviewFromScreenPosition(Vector2 screenPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            previewPosition = hit.point;
            UpdateFormationPreview();
        }
    }

    void UpdateFormationPreview()
    {
        // Coletar unidades selecionadas
        cachedUnits.Clear();
        foreach (var unit in selectionManager.Selection)
        {
            if (unit != null)
                cachedUnits.Add(unit);
        }

        if (cachedUnits.Count == 0)
        {
            cachedFormation = null;
            return;
        }

        // Usar MovementCommandHandler para calcular o preview
        if (movementCommandHandler != null)
        {
            cachedFormation = movementCommandHandler.CalculateFormationPreview(previewPosition);
        }
        else
        {
            // Fallback: calcular diretamente se MovementCommandHandler não estiver disponível
            cachedFormation = FormationCalculator.CalculateFormation(
                cachedUnits,
                FormationType.Line,
                2f
            );
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showPreview || cachedFormation == null || cachedFormation.Count == 0)
            return;

        // Desenhar preview da formação
        Gizmos.color = previewColor;

        foreach (var kvp in cachedFormation)
        {
            Vector3 worldPos = previewPosition + kvp.Value;

            // Esfera para cada posição
            Gizmos.DrawSphere(worldPos, previewRadius);

            // Linha conectando ao centro
            Gizmos.DrawLine(previewPosition, worldPos);
        }

        // Desenhar centro da formação
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(previewPosition, 0.8f);

        // Label com info
        FormationType currentFormation = movementCommandHandler != null
            ? movementCommandHandler.DefaultFormation
            : FormationType.Line;

        UnityEditor.Handles.Label(
            previewPosition + Vector3.up * 2f,
            $"{currentFormation}\n{cachedFormation.Count} unidades"
        );

        // Desenhar raio da formação
        float radius = FormationCalculator.GetFormationRadius(cachedFormation);
        UnityEditor.Handles.color = new Color(0f, 1f, 0f, 0.1f);
        UnityEditor.Handles.DrawWireDisc(previewPosition, Vector3.up, radius);
    }
#endif

    /// <summary>
    /// Toggle visualização (pode ser chamado de botão ou tecla)
    /// </summary>
    public void TogglePreview()
    {
        showPreview = !showPreview;
    }

    /// <summary>
    /// Força atualização do preview
    /// </summary>
    [ContextMenu("Force Update Preview")]
    public void ForceUpdatePreview()
    {
        UpdateFormationPreview();
    }
}