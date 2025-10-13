using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    [Header("Selection")]
    [Tooltip("Px extras no ret�ngulo para evitar perda por borda")]
    public float rectInflatePx = 1.5f;

    [Header("Refs")]
    public PlayerController player;   // define a fac��o local (Player1, etc.)
    public Camera cam;                // mesma c�mera usada no WorldPicker
    public InputSelection input;      // nosso input separado

    [Header("Filtro")]
    public bool onlyOwnUnits = true;  // nunca selecionar unidades de outra fac��o

    // seleção atual
    readonly HashSet<Unit> _selection = new();
    public IReadOnlyCollection<Unit> Selection => _selection;
    public int Count => _selection.Count;
    public event Action<IReadOnlyCollection<Unit>> OnSelectionChanged;

    // �ncora para SHIFT (intervalo)
    Unit _rangeAnchor;     // << era _anchorUnit

    // controle de drag (coordenadas de tela)
    Vector2 _dragStart;
    public void ClearAnchor() => _rangeAnchor = null;


    void Reset()
    {
        if (!cam) cam = Camera.main;
    }

    void OnEnable()
    {
        input.OnClickUnit += HandleClickUnit;
        input.OnClickGround += HandleClickGround;
        input.OnBeginDrag += OnBeginDragHandler;   // << usar m�todo
        input.OnEndDrag += HandleEndDrag;
        input.OnDoubleClickUnit += HandleDoubleClickUnit;
    }

    void OnDisable()
    {
        input.OnClickUnit -= HandleClickUnit;
        input.OnClickGround -= HandleClickGround;
        input.OnBeginDrag -= OnBeginDragHandler;   // << remover o mesmo m�todo
        input.OnEndDrag -= HandleEndDrag;
        input.OnDoubleClickUnit -= HandleDoubleClickUnit;
    }
    void OnBeginDragHandler(Vector2 startScreenPos)
    {
        _dragStart = startScreenPos;
    }

    // ======== Handlers de Input ========

    void HandleClickUnit(Unit unit, bool ctrl, bool shift)
    {
        if (onlyOwnUnits && unit.owner != player.myFaction) return;

        if (shift)
        {
            // intervalo no "mundo": ret�ngulo entre �ncora e alvo clicado
            // Se ainda n�o existe �ncora, trata como clique normal e define �ncora.
            if (_rangeAnchor == null)
            {
                if (ctrl) Toggle(unit);
                else { Clear(); Add(unit); }
                _rangeAnchor = unit;     // define �ncora
                FireChanged();
                return;
            }

            // SHIFT: seleciona intervalo entre �ncora fixa e o novo alvo.
            var a = cam.WorldToScreenPoint(_rangeAnchor.transform.position);
            var b = cam.WorldToScreenPoint(unit.transform.position);

            // Seleciona o intervalo
            var rect = Inflate(BuildRect(a, b), rectInflatePx);
            SelectByScreenRect(rect, additive: ctrl);

            // Garante os extremos dentro (�ncora e alvo)
            if (!onlyOwnUnits || _rangeAnchor.owner == player.myFaction) Add(_rangeAnchor);
            Add(unit);

            // IMPORTANTE: N�O muda a �ncora enquanto Shift estiver pressionado
            FireChanged();
            return;
        }

        // Sem shift: clique normal
        if (ctrl) Toggle(unit);
        else { Clear(); Add(unit); }

        _rangeAnchor = unit;
        FireChanged();
    }
    void HandleClickGround(Vector3 worldPoint, bool ctrl, bool shift)
    {
        if (shift && _rangeAnchor != null)
        {
            // SHIFT + clique no terreno: usa �ncora e o ponto clicado
            var a = cam.WorldToScreenPoint(_rangeAnchor.transform.position);
            var b = cam.WorldToScreenPoint(worldPoint);

            var rect = Inflate(BuildRect(a, b), rectInflatePx);
            SelectByScreenRect(rect, additive: ctrl);

            // garante a �ncora dentro
            if (!onlyOwnUnits || _rangeAnchor.owner == player.myFaction) Add(_rangeAnchor);

            // N�O muda a �ncora
            FireChanged();
            return;
        }

        // clique no ch�o (ou RMB no seu setup): limpa sele��o
        Clear();
        // opcional: n�o mexer na �ncora; ela permanece at� um clique normal substituir
        FireChanged();
    }
    void HandleEndDrag(Vector2 endScreenPos)
    {
        // arrasto retangular: substitui; com Ctrl, adiciona
        bool ctrl = input != null && input.IsCtrlPressed;
        SelectByScreenRect(BuildRect(_dragStart, endScreenPos), additive: ctrl);
        // N�O altera _rangeAnchor
        FireChanged();
    }
    void HandleDoubleClickUnit(Unit unit)
    {
        if (onlyOwnUnits && unit.owner != player.myFaction) return;

        Clear();
        // "mesmo tipo vis�vel": vou usar UnitDefinition (ou type)
        var mine = UnitRegistry.GetByFaction(player.myFaction);
        foreach (var u in mine)
        {
            if (!IsOnScreen(u.transform.position)) continue;
            if (u.def == unit.def) Add(u); // ou comparar u.def.type se preferir
        }
        _rangeAnchor = unit;  // duplo clique atualiza �ncora
        FireChanged();
    }

    // ======== Opera��es de Sele��o ========

    void Add(Unit u)
    {
        if (_selection.Add(u)) u.SetSelected(true);
    }

    void Remove(Unit u)
    {
        if (_selection.Remove(u)) u.SetSelected(false);
    }

    void Toggle(Unit u)
    {
        if (_selection.Contains(u)) Remove(u);
        else Add(u);
    }

    void Clear()
    {
        if (_selection.Count == 0) return;
        foreach (var u in _selection) u.SetSelected(false);
        _selection.Clear();
    }

    void SelectByScreenRect(Rect screenRect, bool additive)
    {
        if (!additive) Clear();

        var mine = UnitRegistry.GetByFaction(player.myFaction);
        for (int i = 0; i < mine.Count; i++)
        {
            var u = mine[i];
            var sp = cam.WorldToScreenPoint(u.transform.position);
            if (sp.z <= 0f) continue;                   // atr�s da c�mera
            if (!IsInViewport(sp)) continue;            // fora da tela
            if (screenRect.Contains(sp, true)) Add(u);  // centro-dentro
        }
    }

    // ======== Util ========

    Rect BuildRect(Vector2 a, Vector2 b)
    {
        var min = Vector2.Min(a, b);
        var max = Vector2.Max(a, b);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    bool IsInViewport(Vector3 screenPos)
    {
        // se quiser, pode usar viewport (0..1). Aqui j� basta checar limites da tela:
        return screenPos.x >= 0 && screenPos.x <= Screen.width &&
               screenPos.y >= 0 && screenPos.y <= Screen.height;
    }

    bool IsOnScreen(Vector3 worldPos)
    {
        var sp = cam.WorldToScreenPoint(worldPos);
        return sp.z > 0 && IsInViewport(sp);
    }

    void FireChanged() => OnSelectionChanged?.Invoke(_selection);
    Rect Inflate(Rect r, float px)
    {
        r.xMin -= px; r.yMin -= px;
        r.xMax += px; r.yMax += px;
        return r;
    }

    // ======== Bridge p/ UI da lista (métodos ADITIVOS) ========
    public bool IsSelected(Unit u) => u != null && _selection.Contains(u);

    public void SelectExactly(IEnumerable<Unit> units)
    {
        Clear();
        if (units != null)
        {
            foreach (var u in units) if (u != null) Add(u);
        }
        FireChanged();
    }

    public void SelectExactly(Unit u)
    {
        Clear();
        if (u != null) Add(u);
        FireChanged();
    }

    public void ToggleSet(IEnumerable<Unit> units)
    {
        if (units == null) return;
        foreach (var u in units) if (u != null) Toggle(u);
        FireChanged();
    }
    // ADIÇÃO: união (add) sem limpar o que já está selecionado
    public void AddToSelection(IEnumerable<Unit> units)
    {
        if (units == null) return;

        bool changed = false;
        foreach (var u in units)
        {
            if (u == null) continue;
            if (_selection.Add(u))
            {
                u.SetSelected(true);
                changed = true;
            }
        }

        if (changed) FireChanged();
    }

}