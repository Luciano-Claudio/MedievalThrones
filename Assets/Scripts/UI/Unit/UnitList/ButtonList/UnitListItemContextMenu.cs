using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(ReorderableListItem))]
[RequireComponent(typeof(UnitListItemUI))]
public class UnitListItemContextMenu : MonoBehaviour
{
    [Header("Prefab do menu (root tem UnitContextMenuHandler)")]
    public RectTransform contextMenuPrefab;

    [Header("Layout")]
    public Vector2 screenPadding = new Vector2(8f, 8f);
    public Color blockerColor = new Color(0, 0, 0, 0.001f);

    // refs locais (auto)
    ReorderableListItem _reorder;
    UnitListItemUI _ui;

    RectTransform _menu;
    GameObject _blocker;

    void Awake()
    {
        _reorder = GetComponent<ReorderableListItem>();
        _ui = GetComponent<UnitListItemUI>();
    }

    // ===== alvo para Follow =====
    Transform ResolveTargetTransform()
    {
        // UnitListItemUI expõe Unit; normalmente é um MonoBehaviour com Transform
        if (_ui != null && _ui.Unit != null)
            return _ui.Unit.transform;

        // fallback: o próprio item (não deve acontecer)
        return transform;
    }

    // ===== instancia popup =====
    public void ShowMenuAt(Vector2 screenPos, Camera eventCam)
    {
        CloseMenu();
        if (!contextMenuPrefab) return;

        // Canvas do item vem do ReorderableListItem (já está preenchido nele)
        RectTransform parent = null;
        var canvas = _reorder != null ? _reorder.Canvas : null;
        if (canvas) parent = canvas.transform as RectTransform;
        if (parent == null)
        {
            var any = GetComponentInParent<Canvas>();
            if (any) parent = any.transform as RectTransform;
        }
        if (parent == null) return;

        // 1) blocker “fecha ao clicar fora”
        // --- Blocker customizado (detecta left/right) ---
        _blocker = new GameObject("UnitContextMenuBlocker",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(BlockerInputCatcher));
        var brt = (RectTransform)_blocker.transform;
        brt.SetParent(parent, false);
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        brt.SetAsLastSibling();

        var bImg = _blocker.GetComponent<Image>();
        bImg.color = blockerColor;

        // Passa a lógica para o script auxiliar
        var catcher = _blocker.GetComponent<BlockerInputCatcher>();
        catcher.onLeftClick = CloseMenu;
        catcher.onRightClick = () =>
        {
            CloseMenu();
            ForwardRightClickToAnyContextTarget();
        };

        // 2) menu
        _menu = Instantiate(contextMenuPrefab, parent);
        _menu.gameObject.SetActive(true);
        _menu.SetAsLastSibling();

        // 3) posiciona com vértice superior esquerdo no mouse
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, eventCam, out var local);
        _menu.anchoredPosition = local;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_menu);

        var size = _menu.rect.size;
        var pivot = _menu.pivot;
        var pos = _menu.anchoredPosition;
        pos.x -= size.x * pivot.x;          // esquerda no mouse
        pos.y += size.y * (1f - pivot.y);   // topo    no mouse
        _menu.anchoredPosition = pos;

        // 4) configura handler
        var handler = _menu.GetComponent<UnitContextMenuHandler>();
        if (handler == null) { CloseMenu(); return; }
        handler.Setup(this, ResolveTargetTransform());

        // 5) clamp
        ClampToParent(parent, _menu);
    }

    void ClampToParent(RectTransform parent, RectTransform menu)
    {
        var pr = parent.rect;
        var mr = menu.rect;
        var pos = menu.anchoredPosition;
        var pv = menu.pivot;

        float left = pos.x - mr.width * pv.x;
        float top = pos.y + mr.height * (1f - pv.y);

        if (left < pr.xMin + screenPadding.x)
            pos.x += (pr.xMin + screenPadding.x) - left;
        else if (left + mr.width > pr.xMax - screenPadding.x)
            pos.x -= (left + mr.width) - (pr.xMax - screenPadding.x);

        if (top > pr.yMax - screenPadding.y)
            pos.y -= top - (pr.yMax - screenPadding.y);
        else if (top - mr.height < pr.yMin + screenPadding.y)
            pos.y += (pr.yMin + screenPadding.y) - (top - mr.height);

        menu.anchoredPosition = pos;
    }
    void SelectSelf()
    {
        var panel = GetComponentInParent<UnitListPanel>();
        var unit = _ui != null ? _ui.Unit : null;
        if (panel == null || unit == null) return;

        // Seleção simples, sem Ctrl/Shift/double
        panel.SelectOnlyUnit(unit);
    }
    void ForwardRightClickToAnyContextTarget()
    {
        var es = EventSystem.current;
        if (es == null) return;

        var pointer = new PointerEventData(es)
        {
            position = UnityEngine.InputSystem.Mouse.current.position.ReadValue(),
            button = PointerEventData.InputButton.Right
        };

        var results = new System.Collections.Generic.List<RaycastResult>();
        es.RaycastAll(pointer, results);

        foreach (var r in results)
        {
            // 1) tenta item de unidade
            var unitCtx = r.gameObject.GetComponent<UnitListItemHandle>();
            if (unitCtx != null) { unitCtx.OnPointerClick(pointer); return; }

            // 2) tenta header de grupo
            var groupCtx = r.gameObject.GetComponent<GroupHeaderContextMenu>();
            if (groupCtx != null) { groupCtx.OnPointerClick(pointer); return; }
        }
    }

    public void CloseMenu()
    {
        if (_menu) Destroy(_menu.gameObject);
        if (_blocker) Destroy(_blocker);
        _menu = null; _blocker = null;
    }

    void OnDisable() => CloseMenu();
}
