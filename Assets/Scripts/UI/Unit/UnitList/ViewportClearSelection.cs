using UnityEngine;
using UnityEngine.EventSystems;

/// Clique no “vazio” do Viewport limpa seleção e âncora.
public class ViewportClearSelection : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] UnitListPanel panel; // pode ser arrastado, mas também auto-descobre

    void Awake()
    {
        if (!panel) panel = GetComponentInParent<UnitListPanel>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!panel) return;

        // Se o clique acerta um item, não limpa.
        var go = eventData.pointerPressRaycast.gameObject;
        if (go && go.GetComponentInParent<UnitListItemUI>()) return;

        // limpa seleção + âncora no SelectionManager
        var sel = panel.GetComponentInChildren<SelectionManager>(true);
        if (sel != null)
        {
            sel.SelectExactly(System.Array.Empty<Unit>()); // já existente
            sel.ClearAnchor();                             // âncora (método que você já tem na V2)
        }
    }
}
