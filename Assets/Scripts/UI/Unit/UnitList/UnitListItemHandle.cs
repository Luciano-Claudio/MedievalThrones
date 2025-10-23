using UnityEngine;
using UnityEngine.EventSystems;

public class UnitListItemHandle : MonoBehaviour, IPointerClickHandler
{
    UnitListPanel _panel;
    InputSelection _input;
    SelectionManager _selection;
    int _index;
    Unit _unit;

    [SerializeField] float doubleClickMaxDelay = 0.30f;
    float _lastClickTime = -10f;

    // --- SUPRESSÃO DE CLIQUE APÓS DRAG (por frame) ---
    int _suppressClickFrame = -1;

    public int Index => _index;
    public Unit CurrentUnit => _unit;

    public void Setup(UnitListPanel panel, InputSelection input, SelectionManager selection, int index, Unit unit)
    {
        _panel = panel;
        _input = input;
        _selection = selection;
        _index = index;
        _unit = unit;
    }

    public void SetIndex(int index) => _index = index;

    public void IgnoreNextClickOnce()
    {
        _suppressClickFrame = Time.frameCount;
        _lastClickTime = -10f;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Se este item representa um grupo, precisaremos de um tratamento de clique diferente.
        // Por enquanto, só processa se houver uma Unit.
        if (_panel == null || _unit == null) return;

        if (Time.frameCount == _suppressClickFrame)
        {
            _suppressClickFrame = -1;
            return;
        }

        bool ctrl = _input != null && _input.IsCtrlPressed;
        bool shift = _input != null && _input.IsShiftPressed;

        // O índice visual é o índice do filho no content
        // NOTA: unitNow no UnitListPanel está desnecessário se _unit for passado.
        Unit unitToPass = _unit;

        bool isDoubleSameItem = (Time.unscaledTime - _lastClickTime) <= doubleClickMaxDelay;
        _lastClickTime = Time.unscaledTime;

        // Passa a Unit ligada para o painel resolver a seleção.
        _panel.OnItemClicked(unitToPass, ctrl, shift, isDoubleSameItem);
    }
}