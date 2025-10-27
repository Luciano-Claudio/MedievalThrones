using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// Coloque este componente no GameObject "Name" do seu Group.
/// Comportamento:
/// - Right click abre o popup no header.
/// - Right click em outro header fecha o atual e abre no novo.
/// - Left click fecha o popup.
public class GroupHeaderContextMenu : MonoBehaviour, IPointerClickHandler
{
    [Header("Prefab do menu (com os botões Rename/Delete)")]
    public RectTransform contextMenuPrefab;

    [Header("Onde instanciar (se vazio, usa o Canvas do ReorderableListItem)")]
    public RectTransform uiRoot;
    public ReorderableListItem Rei;

    [Header("Ajustes de layout")]
    [Tooltip("Padding (em px) para não colar o menu nas bordas do Canvas")]
    public Vector2 screenPadding = new Vector2(8f, 8f);

    [Tooltip("Cor do blocker (0 alpha já recebe raycasts); use algo >0 se quiser ver um leve overlay")]
    public Color blockerColor = new Color(0, 0, 0, 0.001f);

    RectTransform _menu;
    GameObject _blocker;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ShowMenuAt(eventData.position, eventData.pressEventCamera);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            CloseMenu();
        }
    }

    void ShowMenuAt(Vector2 screenPos, Camera eventCam)
    {
        CloseMenu();

        if (contextMenuPrefab == null) return;

        var parent = uiRoot;
        if (parent == null)
        {
            var cv = Rei != null ? Rei.Canvas : null;
            if (cv != null) parent = cv.transform as RectTransform;
        }
        if (parent == null)
        {
            var any = GetComponentInParent<Canvas>();
            if (any) parent = any.transform as RectTransform;
        }
        if (parent == null) return;

        // 1) Blocker customizado (diferencia left/right e reenvia o right)
        _blocker = new GameObject("GroupContextMenuBlocker",
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

        var catcher = _blocker.GetComponent<BlockerInputCatcher>();
        catcher.onLeftClick = CloseMenu;
        catcher.onRightClick = () =>
        {
            CloseMenu();
            ForwardRightClickToAnyContextTarget();
        };

        // 2) Menu
        _menu = Instantiate(contextMenuPrefab, parent);
        _menu.gameObject.SetActive(true);
        _menu.SetAsLastSibling();

        var groupUI = GetComponentInParent<GroupListItemUI>();
        var rootPanel = groupUI != null ? groupUI.GetComponentInParent<UnitListPanel>() : null;
        if (groupUI == null || rootPanel == null) { CloseMenu(); return; }

        // 3) Posiciona canto superior esquerdo no mouse
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, eventCam, out var local);
        _menu.anchoredPosition = local;
        LayoutRebuilder.ForceRebuildLayoutImmediate(_menu);

        var size = _menu.rect.size;
        var pivot = _menu.pivot;
        var pos = _menu.anchoredPosition;
        pos.x -= size.x * pivot.x;          // esquerda no mouse
        pos.y += size.y * (1f - pivot.y);   // topo    no mouse
        _menu.anchoredPosition = pos;

        // 4) Setup do handler do menu
        var handler = _menu.GetComponent<GroupContextMenuHandler>();
        if (handler)
        {
            TMP_Text headerText = groupUI.nameText;
            handler.Setup(rootPanel, groupUI.GroupModel, headerText, this);
        }

        // 5) Clamp
        ClampToParent(parent, _menu);
    }

    void ClampToParent(RectTransform parent, RectTransform menu)
    {
        var pr = parent.rect;
        var m = menu.rect;
        var pos = menu.anchoredPosition;
        var pv = menu.pivot;

        float left = pos.x - m.width * pv.x;
        float top = pos.y + m.height * (1f - pv.y);

        if (left < pr.xMin + screenPadding.x)
            pos.x += (pr.xMin + screenPadding.x) - left;
        else if (left + m.width > pr.xMax - screenPadding.x)
            pos.x -= (left + m.width) - (pr.xMax - screenPadding.x);

        if (top > pr.yMax - screenPadding.y)
            pos.y -= top - (pr.yMax - screenPadding.y);
        else if (top - m.height < pr.yMin + screenPadding.y)
            pos.y += (pr.yMin + screenPadding.y) - (top - m.height);

        menu.anchoredPosition = pos;
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
            var unitCtx = r.gameObject.GetComponent<UnitListItemHandle>();
            if (unitCtx != null) { unitCtx.OnPointerClick(pointer); return; }

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
