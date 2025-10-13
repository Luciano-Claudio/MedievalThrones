using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Coloque este componente no GameObject "Name" do seu Group.
/// Ao clicar com o botão direito, instancia o prefab do menu na posição do mouse.
/// Fecha ao clicar fora ou ao desabilitar o objeto.
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
        if (eventData.button != PointerEventData.InputButton.Right) return;
        ShowMenuAt(eventData.position, eventData.pressEventCamera);
    }

    void ShowMenuAt(Vector2 screenPos, Camera eventCam)
    {
        CloseMenu();

        if (contextMenuPrefab == null) return;

        var parent = uiRoot;
        if (parent == null)
        {
            var cv = Rei.Canvas;
            if (cv != null) parent = cv.transform as RectTransform;
        }
        if (parent == null) return;

        // 1) Cria blocker
        _blocker = new GameObject("ContextMenuBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        var brt = (RectTransform)_blocker.transform;
        brt.SetParent(parent, false);
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = Vector2.zero;
        brt.offsetMax = Vector2.zero;
        brt.SetAsLastSibling();

        var bImg = _blocker.GetComponent<Image>();
        bImg.color = blockerColor;

        var bBtn = _blocker.GetComponent<Button>();
        bBtn.transition = Selectable.Transition.None;
        bBtn.onClick.AddListener(CloseMenu);

        // 2) Cria o menu por cima do blocker
        _menu = Instantiate(contextMenuPrefab, parent);
        _menu.gameObject.SetActive(true);
        _menu.SetAsLastSibling();

        var groupUI = GetComponentInParent<GroupListItemUI>();
        if (groupUI == null) { CloseMenu(); return; }

        var rootPanel = groupUI.GetComponentInParent<UnitListPanel>();
        if (rootPanel == null) { CloseMenu(); return; }

        // 3) Posição: converte screen → local no parent
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPos, eventCam, out var local);
        _menu.anchoredPosition = local;

        // 4) Rebuild para obter o tamanho final do menu
        LayoutRebuilder.ForceRebuildLayoutImmediate(_menu);

        // 5) CORREÇÃO CRÍTICA: Ajusta a posição para que o VÉRTICE SUPERIOR ESQUERDO fique no mouse.
        // O pivot do seu menu (assumimos 0.5, 0.5) é o ponto de ancoragem.
        // Se queremos o canto superior esquerdo (pivot 0, 1) na posição do mouse,
        // precisamos compensar pela distância do pivot ao canto superior esquerdo (em coordenadas de pivot).

        var size = _menu.rect.size;
        var pivot = _menu.pivot;

        // Deslocamento necessário:
        // X: move o menu para a direita pela distância entre a âncora (pos.x) e a borda esquerda (pos.x - size.x * pivot.x)
        // Y: move o menu para baixo pela distância entre a âncora (pos.y) e a borda superior (pos.y + size.y * (1 - pivot.y))

        // Se o pivot do menu for (0.5, 0.5):
        // X compensa por + size.x * 0.5
        // Y compensa por - size.y * 0.5

        var compensatedPosition = _menu.anchoredPosition;

        // O ponto de ancoragem é o pivot.
        // Queremos que o ponto (0, 1) do menu esteja em 'local'.

        // Deslocamento X: Distância do PIVOT (ex: 0.5) até a borda esquerda (0)
        compensatedPosition.x -= size.x * pivot.x;
        // Deslocamento Y: Distância do PIVOT (ex: 0.5) até a borda superior (1)
        compensatedPosition.y += size.y * (1f - pivot.y);

        _menu.anchoredPosition = compensatedPosition;

        var handler = _menu.GetComponent<GroupContextMenuHandler>();
        if (handler)
        {
            // O GroupListItemUI deve ter a referência ao TextMeshPro do Name
            TMP_Text headerText = groupUI.nameText;

            handler.Setup(rootPanel, groupUI.GroupModel, headerText, this);
        }

        // 6) Rebuild e clamp dentro do parent (usa o ClampToParent ajustado)
        ClampToParent(parent, _menu);
    }

    void ClampToParent(RectTransform parent, RectTransform menu)
    {
        var pr = parent.rect;
        var m = menu.rect;

        // O Clamp agora usa o ponto EXATO de canto do menu (que já foi ajustado)
        // O canto superior esquerdo é o ponto (X_local, Y_local) do menu,
        // E precisamos garantir que o canto superior esquerdo esteja DENTRO do pr.

        var pos = menu.anchoredPosition;
        var pivot = menu.pivot;

        // Borda esquerda do menu no espaço local (X da âncora - offset do pivot)
        float menuLeft = pos.x - m.width * pivot.x;
        // Borda superior do menu no espaço local (Y da âncora + offset do pivot)
        float menuTop = pos.y + m.height * (1f - pivot.y);

        // Clamping X: Se a borda esquerda (menuLeft) for menor que o limite (pr.xMin + padding)
        if (menuLeft < pr.xMin + screenPadding.x)
        {
            // Move o menu para a direita
            pos.x += (pr.xMin + screenPadding.x) - menuLeft;
        }
        else if (menuLeft + m.width > pr.xMax - screenPadding.x)
        {
            // Se a borda direita (menuLeft + width) for maior que o limite
            // Move o menu para a esquerda
            pos.x -= (menuLeft + m.width) - (pr.xMax - screenPadding.x);
        }

        // Clamping Y: Se a borda superior (menuTop) for maior que o limite (pr.yMax - padding)
        if (menuTop > pr.yMax - screenPadding.y)
        {
            // Move o menu para baixo
            pos.y -= menuTop - (pr.yMax - screenPadding.y);
        }
        else if (menuTop - m.height < pr.yMin + screenPadding.y)
        {
            // Se a borda inferior (menuTop - height) for menor que o limite
            // Move o menu para cima
            pos.y += (pr.yMin + screenPadding.y) - (menuTop - m.height);
        }

        menu.anchoredPosition = pos;
    }

    public void CloseMenu()
    {
        if (_menu) Destroy(_menu.gameObject);
        if (_blocker) Destroy(_blocker);
        _menu = null;
        _blocker = null;
    }

    void OnDisable() => CloseMenu();
}
