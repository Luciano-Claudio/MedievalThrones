using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;

// Implementa IPointerClickHandler para clique/seleção e IDropHandler para aninhamento.
public class GroupDropZone : MonoBehaviour, IDropHandler
{
    [Header("Refs")]
    [SerializeField] GroupListItemUI groupUI;
    [SerializeField] UnitListPanel panel; // Referência ao painel principal

    // Duplo clique: o mesmo que UnitListItemHandle
    [SerializeField] float doubleClickMaxDelay = 0.30f;
    float _lastClickTime = -10f;

    private const int SUPPRESS_DRAG_FRAME = 2; // Tempo (em frames) para suprimir clique após drop
    public int LastDropFrame = -100000; // frame em que ocorreu o último drop (inicial valor bem negativo)

    void Awake()
    {
        // Garante que o painel seja encontrado se o GroupUI falhar em ligar
        if (!panel) panel = GetComponentInParent<UnitListPanel>();
        if (!groupUI) groupUI = GetComponentInParent<GroupListItemUI>();
    }

    // --- API de Setup (NOVO) ---
    public void Setup(UnitListPanel p)
    {
        panel = p;
    }

    /// <summary> Usado pelo UnitListPanel para injetar o GroupListItemUI (caso auto-discovery falhe).</summary>
    public void SetGroupUI(GroupListItemUI ui)
    {
        groupUI = ui;
    }


    // --- Lógica de Clique e Seleção de Grupo (Simples/Ctrl/Shift/Duplo) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // suprime cliques imediatamente após um drop: compara frames reais
        if (Time.frameCount - LastDropFrame <= SUPPRESS_DRAG_FRAME)
        {
            return;
        }

        // Garante que o GroupUI foi ligado, se não, tenta a auto-descoberta novamente
        if (groupUI == null) groupUI = GetComponentInParent<GroupListItemUI>();

        if (groupUI == null || groupUI.GroupModel == null || panel == null) return;

        bool ctrl = panel.InputSelection != null && panel.InputSelection.IsCtrlPressed;
        bool shift = panel.InputSelection != null && panel.InputSelection.IsShiftPressed;

        bool isDouble = (Time.unscaledTime - _lastClickTime) <= doubleClickMaxDelay;
        _lastClickTime = Time.unscaledTime;

        var selectionManager = panel.SelectionManager;
        var groupModel = groupUI.GroupModel;
        var unitsInGroup = groupModel.Units;


        if (isDouble)
        {
            var unitInGroup = groupModel.Units.FirstOrDefault();
            if (unitInGroup != null)
            {
                var sameTypeInGroup = groupModel.Units
                    .Where(u => u != null && u.def == unitInGroup.def);
                selectionManager.SelectExactly(sameTypeInGroup);
            }
            return;
        }

        // --- Lógica de Seleção ---
        if (shift)
        {
            if (ctrl)
            {
                selectionManager.AddToSelection(unitsInGroup);
            }
            else
            {
                selectionManager.SelectExactly(unitsInGroup);
            }
        }
        else if (ctrl)
        {
            selectionManager.ToggleSet(unitsInGroup);
        }
        else
        {
            selectionManager.SelectExactly(unitsInGroup);
        }

        // NOVO: Chama o método do painel para registrar a âncora de intervalo (_anchorIndex)
        panel.CommitSelectionForGroup(groupModel);
    }

    // --- Lógica de Drag-and-Drop (Aninhamento) ---
    public void OnDrop(PointerEventData eventData)
    {
        var draggedItem = eventData.pointerDrag;
        if (draggedItem == null) return;

        var rei = draggedItem.GetComponent<ReorderableListItem>();
        if (rei != null)
        {
            LastDropFrame = Time.frameCount; // Registra o frame do drop
            // O commit será tratado pelo OnEndDrag do ReorderableListItem, que será chamado em seguida.
            return;
        }

        // (Espaço para aceitar drops "externos" no futuro, se existir)
    }

}