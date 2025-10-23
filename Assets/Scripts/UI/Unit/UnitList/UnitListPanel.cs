using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UnitListPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] PlayerController player;
    [SerializeField] SelectionManager selection;
    [SerializeField] InputSelection inputSel;

    [SerializeField] ScrollRect scroll;
    [SerializeField] RectTransform content;
    [SerializeField] UnitListItemUI itemPrefab;

    [SerializeField] GroupListItemUI groupPrefab; // Prefab do Grupo

    [Tooltip("Raiz para arrasto (fora da lista).")]
    [SerializeField] RectTransform dragRoot;

    [Header("Assets")]
    [SerializeField] Texture2D resizeCursorTexture;

    // ordem canônica da lista: mistura itens (Units) e grupos (UnitGroup)
    readonly List<ListItemWrapper> _order = new();
    readonly List<ReorderableListItem> _items = new();

    Canvas _canvas;
    int? _anchorIndex;

    public RectTransform Content => content;
    public Canvas CanvasUI => _canvas;
    public RectTransform DragRoot => dragRoot;
    public ScrollRect ScrollRect => scroll;
    public RectTransform Viewport => scroll ? scroll.viewport : null;

    public SelectionManager SelectionManager => selection;
    public InputSelection InputSelection => inputSel;

    void Awake()
    {
        if (!_canvas) _canvas = GetComponentInParent<Canvas>();
        if (!scroll) scroll = GetComponentInChildren<ScrollRect>(true);
        if (!content && scroll) content = scroll.content;

        if (!dragRoot)
        {
            var found = GameObject.Find("DragRoot");
            if (found) dragRoot = found.transform as RectTransform;
        }
    }

    void OnEnable()
    {
        InitializeOrderFromRegistry();

        Build();
        if (selection != null)
            selection.OnSelectionChanged += RefreshFromSelection;
    }

    void OnDisable()
    {
        if (selection != null)
            selection.OnSelectionChanged -= RefreshFromSelection;
    }

    /// <summary>Popula a ordem inicial com as Units da cena se a lista estiver vazia.</summary>
    void InitializeOrderFromRegistry()
    {
        if (_order.Any(w => w.IsGroup) || _order.Count > 0) return;

        var myUnits = UnitRegistry.GetByFaction(player.myFaction);
        foreach (var u in myUnits)
        {
            _order.Add(new ListItemWrapper(u));
        }
    }

    // ------------------------------------------------------------------
    // BUILD
    // ------------------------------------------------------------------
    public void Build()
    {
        ClearChildren();

        if (itemPrefab == null)
        {
            Debug.LogError("[UnitListPanel] 'Item Prefab' não está ligado. Lista de Units não será construída.");
            return;
        }
        if (groupPrefab == null)
        {
            Debug.LogWarning("[UnitListPanel] 'Group Prefab' não está ligado. Grupos não serão construídos.");
        }

        int total = _order.Count;
        bool reverse = false;
        var vlg = content ? content.GetComponent<VerticalLayoutGroup>() : null;
        if (vlg) reverse = vlg.reverseArrangement;

        for (int i = 0; i < _order.Count; i++)
        {
            int desiredSibling = reverse ? (total - 1 - i) : i;
            var wrapper = _order[i];

            if (wrapper.IsGroup)
            {
                var groupUI = Instantiate(groupPrefab, content);
                groupUI.transform.SetSiblingIndex(desiredSibling);
                // marca o GO do grupo com o wrapper correspondente
                var markerG = groupUI.gameObject.AddComponent<ListItemMarker>();
                markerG.Wrapper = wrapper;


                var groupModel = (UnitGroup)wrapper.Model;
                groupUI.Bind(groupModel);

                var dropZones = groupUI.GetComponentsInChildren<GroupDropZone>(true);
                foreach (var dz in dropZones)
                {
                    dz.Setup(this);
                    dz.SetGroupUI(groupUI);
                }

                var reiG = groupUI.GetComponent<ReorderableListItem>();
                if (reiG)
                {
                    reiG.Inject(content, dragRoot, _canvas, scroll, scroll.viewport, this);
                    _items.Add(reiG);
                }

                var resizeHandlerG = groupUI.GetComponentInChildren<ResizeGripHandler>(true);
                if (resizeHandlerG)
                {
                    resizeHandlerG.Setup(scroll, scroll.viewport, resizeCursorTexture);
                }

                BuildGroupContent(groupModel, groupUI.subContent);
            }
            else
            {
                var u = wrapper.GetUnit();
                if (u == null) continue;

                var ui = Instantiate(itemPrefab, content);
                ui.transform.SetSiblingIndex(desiredSibling);
                // marca o GO do item com o wrapper correspondente
                var markerU = ui.gameObject.AddComponent<ListItemMarker>();
                markerU.Wrapper = wrapper;


                ui.Bind(u);

                var handle = ui.GetComponent<UnitListItemHandle>();
                if (handle) handle.Setup(this, inputSel, selection, i, u);

                var rei = ui.GetComponent<ReorderableListItem>();
                if (rei)
                {
                    rei.Inject(content, dragRoot, _canvas, scroll, scroll.viewport, this);
                    _items.Add(rei);
                }
            }
        }
        // ... seu Build() após terminar de instanciar itens e grupos
        ApplyRootSiblingOrder();     // <--- NOVO: fixa a posição dos grupos/itens na raiz
        StartCoroutine(ApplyRootSiblingOrderNextFrame()); // <--- NOVO
        RefreshItemIndices();
        RefreshFromSelection(selection?.Selection);
    }

    void ClearChildren()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            DestroyImmediate(content.GetChild(i).gameObject);
        _items.Clear();
    }

    // ------------------------------------------------------------------
    // GRUPOS
    // ------------------------------------------------------------------
    /// <summary>Chamado pelo CreateGroupUI.</summary>
    public void CreateNewGroup(string groupName)
    {
        // 1) cria o modelo
        var newGroup = new UnitGroup { GroupName = groupName, IsExpanded = true };
        var newWrapper = new ListItemWrapper(newGroup);

        // 2) adiciona no fim do _order (ou ajuste para inserir onde quiser)
        _order.Add(newWrapper);
        int modelIndex = _order.Count - 1;

        // 3) calcula o índice visual respeitando ReverseArrangement
        bool reverse = false;
        var vlg = content ? content.GetComponent<VerticalLayoutGroup>() : null;
        if (vlg) reverse = vlg.reverseArrangement;
        int desiredSibling = reverse ? (_order.Count - 1 - modelIndex) : modelIndex;

        // 4) instancia SÓ o grupo novo
        var groupUI = Instantiate(groupPrefab, content);
        groupUI.transform.SetSiblingIndex(desiredSibling);

        // (opcional) marcador
        var marker = groupUI.gameObject.GetComponent<ListItemMarker>() ?? groupUI.gameObject.AddComponent<ListItemMarker>();
        marker.Wrapper = newWrapper;

        // bind + dropzones + drag
        groupUI.Bind(newGroup);

        var dropZones = groupUI.GetComponentsInChildren<GroupDropZone>(true);
        foreach (var dz in dropZones)
        {
            dz.Setup(this);
            dz.SetGroupUI(groupUI);
        }

        var resizeHandlerG = groupUI.GetComponentInChildren<ResizeGripHandler>(true);
        if (resizeHandlerG)
        {
            resizeHandlerG.Setup(scroll, scroll.viewport, resizeCursorTexture);
        }

        // conteúdo interno (vazio nesse momento, mas mantém a consistência)
        BuildGroupContent(newGroup, groupUI.subContent);

        var reiG = groupUI.GetComponent<ReorderableListItem>();
        if (reiG)
        {
            reiG.Inject(content, dragRoot, _canvas, scroll, scroll.viewport, this);
            _items.Add(reiG);
        }

        // 5) indices/seleção (sem rebuild da lista toda)
        RefreshItemIndices();
        RefreshFromSelection(selection?.Selection);
    }


    /// <summary>Instancia as Units de um grupo no seu subContent e configura drag/handle.</summary>
    public void BuildGroupContent(UnitGroup group, RectTransform parentContent)
    {
        if (group == null || parentContent == null) return;

        for (int i = parentContent.childCount - 1; i >= 0; i--)
            DestroyImmediate(parentContent.GetChild(i).gameObject);

        int visualIndex = 0;

        if (group.IsExpanded)
        {
            foreach (var unit in group.Units)
            {
                if (unit == null) continue;

                var ui = Instantiate(itemPrefab, parentContent);
                ui.Bind(unit);

                var handle = ui.GetComponent<UnitListItemHandle>();
                if (handle) handle.Setup(this, inputSel, selection, visualIndex, unit);

                var rei = ui.GetComponent<ReorderableListItem>();
                if (rei)
                {
                    // Drag configurado para operar DENTRO do subContent do grupo
                    rei.Inject(
                        parentContent,
                        dragRoot,
                        _canvas,
                        scroll,
                        scroll.viewport,
                        this
                    );
                }

                visualIndex++;
            }
        }
    }

    // ------------------------------------------------------------------
    // CLIQUES & SELEÇÃO
    // ------------------------------------------------------------------
    public void OnItemClicked(Unit unit, bool ctrl, bool shift, bool isDouble)
    {
        if (unit == null) return;

        var wrapper = _order.FirstOrDefault(w => w.GetUnit() == unit);
        if (wrapper == null)
        {
            // Trata Units aninhadas dentro de Grupo
            var group = _order.Where(w => w.IsGroup).Select(w => (UnitGroup)w.Model)
                .FirstOrDefault(g => g.Units.Contains(unit));

            if (group == null) return;

            if (isDouble)
            {
                var sameType = group.Units.Where(u => u != null && u.def == unit.def);
                selection.SelectExactly(sameType);
                return;
            }

            if (ctrl) selection.ToggleSet(new[] { unit });
            else selection.SelectExactly(unit);

            return;
        }

        // Item de topo
        int index = _order.IndexOf(wrapper);

        // Duplo clique: seleciona todos do mesmo tipo
        if (isDouble)
        {
            var sameType = _order
                .Select(w => w.GetUnit())
                .Where(u => u != null && u.def == unit.def);

            selection.SelectExactly(sameType);
            _anchorIndex = index;
            return;
        }

        // SHIFT (intervalo)
        if (shift)
        {
            int from = _anchorIndex ?? index;
            int a = Mathf.Min(from, index);
            int b = Mathf.Max(from, index);

            var rangeOfUnits = _order
                .Skip(a)
                .Take(b - a + 1)
                .SelectMany(w => w.GetUnitsInItem())
                .Where(u => u != null)
                .Distinct();

            if (ctrl) selection.AddToSelection(rangeOfUnits);
            else selection.SelectExactly(rangeOfUnits);

            return;
        }

        // CTRL (toggle)
        if (ctrl)
        {
            selection.ToggleSet(new[] { unit });
            _anchorIndex = index;
            return;
        }

        // Clique simples
        selection.SelectExactly(unit);
        _anchorIndex = index;
    }

    public void RefreshFromSelection(IReadOnlyCollection<Unit> units)
    {
        if (units == null || content == null) return;

        int visualCount = content.childCount;
        int n = Mathf.Min(_order.Count, visualCount);

        for (int i = 0; i < n; i++)
        {
            var wrapper = _order[i];

            var tr = content.GetChild(i);
            if (!tr) continue;

            var ui = tr.GetComponent<UnitListItemUI>();
            var groupUI = tr.GetComponent<GroupListItemUI>();

            // 1) item Unit
            if (ui != null)
            {
                bool isUnitSelected = units.Contains(ui.Unit);
                ui.SetSelected(isUnitSelected);
            }

            // 2) item Grupo
            if (groupUI != null)
            {
                bool isGroupSelected = groupUI.Units.Any(u => units.Contains(u));
                groupUI.SetSelected(isGroupSelected);

                // também atualiza seleção visual dos filhos
                if (groupUI.subContent != null)
                {
                    foreach (Transform childTr in groupUI.subContent)
                    {
                        var childUI = childTr.GetComponent<UnitListItemUI>();
                        if (childUI != null)
                        {
                            childUI.SetSelected(units.Contains(childUI.Unit));
                        }
                    }
                }
            }
        }
    }

    public void CommitSelectionForGroup(UnitGroup group)
    {
        if (group == null) return;

        var wrapper = _order.FirstOrDefault(w => w.Model == group);
        if (wrapper == null) return;

        _anchorIndex = _order.IndexOf(wrapper);
    }

    public void RefreshItemIndices()
    {
        if (!content) return;

        for (int i = 0; i < content.childCount; i++)
        {
            var h = content.GetChild(i).GetComponent<UnitListItemHandle>();
            if (h) h.SetIndex(i);
        }
    }

    public Unit GetUnitAt(int index)
    {
        if (index < 0 || index >= _order.Count) return null;
        return _order[index].GetUnit();
    }

    public ListItemWrapper GetItemAt(int index)
    {
        if (index < 0 || index >= _order.Count) return null;
        return _order[index];
    }

    // ------------------------------------------------------------------
    // COMMITS DE MOVIMENTO / REORDENAÇÃO
    // ------------------------------------------------------------------

    /// <summary>
    /// Reordenação pedida por um item Unit (pode vir da raiz OU de dentro de grupo).
    /// Se vier de grupo, tiramos do grupo e inserimos na raiz na posição 'to'.
    /// </summary>
    public void CommitReorderByUnit(Unit unit, int to)
    {
        if (unit == null) return;

        // (a) estava na RAIZ → reordena na raiz
        var wrapperOnRoot = _order.FirstOrDefault(w => w.GetUnit() == unit);
        if (wrapperOnRoot != null)
        {
            int from = _order.IndexOf(wrapperOnRoot);
            if (from < 0) return;

            CommitReorder(from, Mathf.Clamp(to, 0, _order.Count - 1));
            // sem Build aqui: o visual já foi movido; só mantemos o modelo.
            return;
        }

        // (b) estava em um GRUPO → drop aconteceu na RAIZ → tira do grupo e insere na RAIZ
        var sourceGroup = FindGroupOfUnit(unit);
        if (sourceGroup != null)
        {
            CommitMoveOutOfGroup(unit, Mathf.Clamp(to, 0, _order.Count));
            return;
        }
    }



    /// <summary>
    /// Move uma Unit que estava aninhada para a lista principal (_order) na posição 'toIndex'.
    /// </summary>
    public void CommitMoveOutOfGroup(Unit unit, int toIndex)
    {
        if (unit == null) return;

        // 1) remover do grupo de origem
        foreach (var gw in _order.Where(w => w.IsGroup))
        {
            var g = (UnitGroup)gw.Model;
            if (g.Units.Remove(unit))
                break;
        }

        // 2) inserir na RAIZ no índice alvo (modelo)
        var newWrapper = new ListItemWrapper(unit);
        toIndex = Mathf.Clamp(toIndex, 0, _order.Count);
        _order.Insert(toIndex, newWrapper);

        // 3) criar APENAS o visual desse item na raiz (sem rebuild global)
        bool reverse = false;
        var vlg = content ? content.GetComponent<VerticalLayoutGroup>() : null;
        if (vlg) reverse = vlg.reverseArrangement;
        int desiredSibling = reverse ? (_order.Count - 1 - toIndex) : toIndex;

        var ui = Instantiate(itemPrefab, content);
        ui.transform.SetSiblingIndex(desiredSibling);

        // (opcional) marcador
        var marker = ui.gameObject.GetComponent<ListItemMarker>() ?? ui.gameObject.AddComponent<ListItemMarker>();
        marker.Wrapper = newWrapper;

        ui.Bind(unit);

        var handle = ui.GetComponent<UnitListItemHandle>();
        if (handle) handle.Setup(this, inputSel, selection, toIndex, unit);

        var rei = ui.GetComponent<ReorderableListItem>();
        if (rei)
        {
            rei.Inject(content, dragRoot, _canvas, scroll, scroll.viewport, this);
            _items.Add(rei);
        }

        // 4) ajustar índices/seleção (sem mexer na posição dos grupos)
        RefreshItemIndices();
        RefreshFromSelection(selection?.Selection);
    }



    // OVERLOAD de conveniência: mantém compatibilidade e evita erro de assinatura
    // mantém compatibilidade com chamadas antigas
    public void CommitMoveToGroup(Unit unit, string targetGroupID)
    {
        // insere ao FIM do grupo (comportamento anterior), mas usando a API que respeita o índice
        var targetWrap = _order.FirstOrDefault(w => w.IsGroup && ((UnitGroup)w.Model).ID == targetGroupID);
        int toIndex = 0;
        if (targetWrap != null) toIndex = ((UnitGroup)targetWrap.Model).Units.Count;

        CommitMoveToGroupAt(unit, targetGroupID, toIndex);
    }

    // se você tiver a versão com 3 parâmetros, troque o corpo por:
    public void CommitMoveToGroup(Unit unit, string targetGroupID, ReorderableListItem draggedItem)
    {
        var targetWrap = _order.FirstOrDefault(w => w.IsGroup && ((UnitGroup)w.Model).ID == targetGroupID);
        int toIndex = 0;
        if (targetWrap != null) toIndex = ((UnitGroup)targetWrap.Model).Units.Count;

        CommitMoveToGroupAt(unit, targetGroupID, toIndex);

        if (draggedItem && draggedItem.gameObject) Destroy(draggedItem.gameObject);
    }

    public UnitGroup FindGroupOfUnit(Unit unit)
    {
        if (unit == null) return null;
        foreach (var gw in _order.Where(w => w.IsGroup))
        {
            var g = (UnitGroup)gw.Model;
            if (g.Units.Contains(unit)) return g;
        }
        return null;
    }

    public void CommitMoveToGroupAt(Unit unit, string targetGroupId, int toIndex)
    {
        if (unit == null || string.IsNullOrEmpty(targetGroupId)) return;

        // Remover da raiz (se estiver)
        _order.RemoveAll(w => !w.IsGroup && w.GetUnit() == unit);

        // Remover do grupo de origem (se estiver)
        UnitGroup sourceGroup = null;
        foreach (var gw in _order.Where(w => w.IsGroup))
        {
            var g = (UnitGroup)gw.Model;
            if (g.Units.Remove(unit))
            {
                sourceGroup = g;
                break;
            }
        }

        // Inserir no grupo alvo na posição solicitada
        var targetWrap = _order.FirstOrDefault(w => w.IsGroup && ((UnitGroup)w.Model).ID == targetGroupId);
        if (targetWrap == null) return;

        var targetGroup = (UnitGroup)targetWrap.Model;
        targetGroup.IsExpanded = true;

        toIndex = Mathf.Clamp(toIndex, 0, targetGroup.Units.Count);
        targetGroup.Units.Insert(toIndex, unit);

        // ✅ NÃO reconstruir a raiz; atualiza apenas os grupos impactados
        if (sourceGroup != null && !ReferenceEquals(sourceGroup, targetGroup))
            RebuildOnlyGroup(sourceGroup);
        RebuildOnlyGroup(targetGroup);

        RefreshFromSelection(selection?.Selection);
    }


    public void CommitReorderInsideGroup(Unit unit, string groupId, int toIndex)
    {
        if (unit == null || string.IsNullOrEmpty(groupId)) return;

        var wrap = _order.FirstOrDefault(w => w.IsGroup && ((UnitGroup)w.Model).ID == groupId);
        if (wrap == null) return;

        var g = (UnitGroup)wrap.Model;
        int from = g.Units.IndexOf(unit);
        if (from < 0) return;

        toIndex = Mathf.Clamp(toIndex, 0, g.Units.Count - 1);
        if (from == toIndex) return;

        g.Units.RemoveAt(from);
        if (toIndex > g.Units.Count) toIndex = g.Units.Count;
        g.Units.Insert(toIndex, unit);

        // ✅ NÃO reconstruir a raiz; atualiza só o grupo
        RebuildOnlyGroup(g);
        RefreshFromSelection(selection?.Selection);
    }


    /// <summary>
    /// Reordena itens na raiz (índices da lista principal).
    /// </summary>
    public void CommitReorder(int from, int to)
    {
        if (from < 0 || from >= _order.Count) return;
        if (to < 0 || to >= _order.Count) return;
        if (from == to) return;

        var wrapper = _order[from];
        _order.RemoveAt(from);
        _order.Insert(to, wrapper);

        // Ajusta âncora de seleção (intervalo SHIFT)
        if (_anchorIndex.HasValue)
        {
            int a = _anchorIndex.Value;
            if (a == from) _anchorIndex = to;
            else if (from < a && to >= a) _anchorIndex = a - 1;
            else if (from > a && to <= a) _anchorIndex = a + 1;
        }
    }

    // Exemplo de API para expandir/contrair via UI do grupo
    public void ToggleGroupExpanded(UnitGroup group, bool expanded)
    {
        if (group == null) return;

        group.IsExpanded = expanded;

        // Rebuild parcial: só o conteúdo do grupo
        RebuildOnlyGroup(group);

        // Atualiza seleção e índices sem mexer na raiz
        RefreshItemIndices();
        RefreshFromSelection(selection?.Selection);
    }


    // Helpers diversos
    public IEnumerable<Unit> EnumerateAllUnits()
    {
        foreach (var w in _order)
        {
            if (w.IsGroup)
            {
                var g = (UnitGroup)w.Model;
                foreach (var u in g.Units) yield return u;
            }
            else yield return w.GetUnit();
        }
    }
    // Encontra o Transform na raiz (content) correspondente a um wrapper do _order
    Transform FindRootChildForWrapper(ListItemWrapper w)
    {
        if (w == null || content == null) return null;

        foreach (Transform child in content)
        {
            var marker = child.GetComponent<ListItemMarker>();
            if (marker != null && object.ReferenceEquals(marker.Wrapper, w))
                return child;
        }
        return null;
    }


    // Aplica a ordem visual dos filhos em 'content' para bater com '_order' (respeitando reverseArrangement)
    void ApplyRootSiblingOrder()
    {
        if (content == null || _order == null) return;

        int n = _order.Count;
        bool reverse = false;
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        if (vlg) reverse = vlg.reverseArrangement;

        for (int i = 0; i < n; i++)
        {
            int desired = reverse ? (n - 1 - i) : i;
            var w = _order[i];
            var t = FindRootChildForWrapper(w);
            if (t != null && t.GetSiblingIndex() != desired)
                t.SetSiblingIndex(desired);
        }
    }
    System.Collections.IEnumerator ApplyRootSiblingOrderNextFrame()
    {
        // aguarda o layout atual “assentar”
        yield return null;                 // end-of-frame
        Canvas.ForceUpdateCanvases();
        ApplyRootSiblingOrder();           // reaplica a ordem canônica
    }
    void RebuildOnlyGroup(UnitGroup g)
    {
        if (g == null || content == null) return;

        // encontra o GroupListItemUI que está representando ESTE modelo na raiz
        GroupListItemUI targetUI = null;
        foreach (Transform child in content)
        {
            var ui = child.GetComponent<GroupListItemUI>();
            if (ui != null && ReferenceEquals(ui.GroupModel, g))
            {
                targetUI = ui;
                break;
            }
        }
        if (targetUI == null) return;

        // limpa e reconstrói SOMENTE o sub-content
        BuildGroupContent(g, targetUI.subContent);

        // mantém expandido conforme modelo e força layout só deste trecho
        targetUI.RefreshChildrenOnly();
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetUI.subContent);
    }
    public void DeleteGroupAndRestoreUnits(UnitGroup group, List<Unit> unitsToRestore)
    {
        if (group == null || unitsToRestore == null || _order == null) return;

        var wrapperToRemove = _order.FirstOrDefault(w => w.Model == group);
        if (wrapperToRemove == null) return;

        int groupIndex = _order.IndexOf(wrapperToRemove);
        if (groupIndex < 0) return;

        // 1. Remove o grupo da lista principal
        _order.RemoveAt(groupIndex);

        // 2. Insere cada unidade de volta na posição onde o grupo estava (na lista principal)
        // Insere na ordem inversa para manter a ordem original ao inserir no mesmo índice
        unitsToRestore.Reverse();

        foreach (var unit in unitsToRestore)
        {
            var newWrapper = new ListItemWrapper(unit);
            // Insere na posição do grupo (a lista _order está agora 1 item menor)
            _order.Insert(groupIndex, newWrapper);
        }

        // 3. Força o Rebuild da lista inteira
        Build();

        // Opcional: Limpar a seleção se o item deletado estava selecionado
        RefreshFromSelection(selection?.Selection);
    }

}
