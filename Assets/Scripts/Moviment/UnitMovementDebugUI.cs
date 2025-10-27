using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Debug UI para visualizar estado das unidades selecionadas
/// Attach no mesmo GameObject que o MovementCommandHandler
/// </summary>
public class UnitMovementDebugUI : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private SelectionManager selectionManager;

    [Header("Configuração")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private Vector2 position = new Vector2(10, 200);
    [SerializeField] private int fontSize = 12;

    private GUIStyle labelStyle;
    private List<Unit> cachedUnits = new List<Unit>();

    void Awake()
    {
        if (!selectionManager)
            selectionManager = Object.FindFirstObjectByType<SelectionManager>();
    }

    void OnGUI()
    {
        if (!showDebugUI || selectionManager == null) return;

        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = fontSize;
            labelStyle.normal.textColor = Color.white;
        }

        // Coletar unidades selecionadas
        cachedUnits.Clear();
        foreach (var unit in selectionManager.Selection)
        {
            if (unit != null)
                cachedUnits.Add(unit);
        }

        if (cachedUnits.Count == 0)
        {
            GUI.Label(new Rect(position.x, position.y, 300, 30),
                      "Nenhuma unidade selecionada", labelStyle);
            return;
        }

        // Desenhar info de cada unidade
        float yOffset = position.y;

        GUI.Label(new Rect(position.x, yOffset, 300, 20),
                  $"=== {cachedUnits.Count} UNIDADES SELECIONADAS ===", labelStyle);
        yOffset += 25;

        foreach (var unit in cachedUnits)
        {
            var movement = unit.GetComponent<UnitMovement>();
            if (movement == null) continue;

            string status = movement.IsMoving ?
                           $"<color=lime>MOVENDO</color>" :
                           $"<color=yellow>PARADO</color>";

            float distance = movement.IsMoving ?
                           Vector3.Distance(movement.Position, movement.FinalDestination) :
                           0f;

            string info = $"<b>{unit.DisplayName}</b> - {status}\n" +
                         $"  Pos: {movement.Position.ToString("F1")}\n";

            if (movement.IsMoving)
            {
                info += $"  Dest: {movement.FinalDestination.ToString("F1")}\n" +
                       $"  Dist: {distance:F1}m";
            }

            // Remover tags HTML (OnGUI não suporta rich text no label normal)
            string cleanInfo = info.Replace("<color=lime>", "")
                                  .Replace("<color=yellow>", "")
                                  .Replace("</color>", "")
                                  .Replace("<b>", "")
                                  .Replace("</b>", "");

            GUI.Label(new Rect(position.x, yOffset, 400, 60), cleanInfo, labelStyle);
            yOffset += 65;
        }
    }

    /// <summary>
    /// Toggle debug UI (pode chamar de outro script ou botão)
    /// </summary>
    public void ToggleDebugUI()
    {
        showDebugUI = !showDebugUI;
    }
}