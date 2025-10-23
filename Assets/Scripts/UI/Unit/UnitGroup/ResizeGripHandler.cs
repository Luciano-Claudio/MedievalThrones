using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ResizeGripHandler : MonoBehaviour,
    IPointerDownHandler, IDragHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [Header("Target (Group)")]
    [Tooltip("O LayoutElement do item de lista (Group 1) que será redimensionado.")]
    [SerializeField]
    private LayoutElement targetLayoutElement;

    [Tooltip("O RectTransform do Group 1.")]
    [SerializeField]
    private RectTransform targetRect;

    [Header("Main List Scroll Refs")]
    [SerializeField]
    private ScrollRect mainScrollRect;
    [SerializeField]
    private RectTransform mainViewport;

    [Header("Cursor Settings")]
    [Tooltip("Arraste seu PNG do cursor para cá (ou será preenchido via código).")]
    [SerializeField]
    private Texture2D resizeCursorTexture;
    [Tooltip("O ponto de ancoragem do cursor (ajuste aqui a correção da posição).")]
    [SerializeField]
    private Vector2 hotSpot = new Vector2(16, 16);

    [Header("Auto-Scroll Tuning")]
    [SerializeField] float edgeHotZonePx = 80f;
    [SerializeField] float maxScrollSpeedPxPerSec = 900f;

    private Vector2 _dragStartMousePos;
    private float _dragStartPreferredHeight;
    private bool _isDragging = false;

    // Opção: tornar editáveis no Inspector para ajuste fino
    [Header("Constraints")]
    [SerializeField] float minHeight = 200f;
    [SerializeField] float maxHeight = 1000f;

    // --- API de Setup para uso dinâmico ---
    public void Setup(ScrollRect mainScroll, RectTransform mainVp, Texture2D cursorTexture)
    {
        mainScrollRect = mainScroll;
        mainViewport = mainVp;
        resizeCursorTexture = cursorTexture;

        // Auto-descobrimento defensivo (se o prefab não estiver ligado)
        if (targetLayoutElement == null)
        {
            targetLayoutElement = GetComponentInParent<LayoutElement>();
        }
        if (targetRect == null && targetLayoutElement != null)
        {
            targetRect = targetLayoutElement.GetComponent<RectTransform>();
        }
    }


    // --- Cursor Handling ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // NOVO: Verifica se há um drag em andamento que NÃO SEJA o drag deste grip.
        bool isOtherDragActive = eventData.pointerDrag != null && eventData.pointerDrag != gameObject;

        if (!isOtherDragActive && resizeCursorTexture != null)
        {
            Cursor.SetCursor(resizeCursorTexture, hotSpot, CursorMode.Auto);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Se o mouse saiu e não estamos arrastando *este* grip, restaura o cursor.
        if (!_isDragging)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    // --- Drag Handling ---

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetLayoutElement == null) return;

        _isDragging = true;
        _dragStartMousePos = eventData.position;

        _dragStartPreferredHeight = targetLayoutElement.preferredHeight > 0
            ? targetLayoutElement.preferredHeight
            : targetRect.rect.height;

        var outline = targetRect.GetComponent<Outline>();
        if (outline) outline.enabled = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || targetLayoutElement == null) return;

        float deltaY = eventData.position.y - _dragStartMousePos.y;
        float newHeight = _dragStartPreferredHeight - deltaY;

        // Aplique o min E max com Mathf.Clamp
        newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

        targetLayoutElement.preferredHeight = newHeight;

        if (targetRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(targetRect);

        // Removemos a lógica allowUpScroll do OnDrag e a colocamos diretamente no DoAutoScroll
        DoAutoScroll(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        var outline = targetRect ? targetRect.GetComponent<Outline>() : null;
        if (outline) outline.enabled = false;

        // Persistir altura do grupo
        var groupUI = GetComponentInParent<GroupListItemUI>();
        if (groupUI != null)
        {
            var le = targetLayoutElement ? targetLayoutElement : groupUI.GetComponent<LayoutElement>();
            if (le != null && groupUI.GroupModel != null)
            {
                string key = "GroupHeight_" + groupUI.GroupModel.ID;
                PlayerPrefs.SetFloat(key, le.preferredHeight);
                PlayerPrefs.Save();
            }
        }
    }

    // --- Lógica de Auto-Scroll ---
    void DoAutoScroll(Vector2 screenPos)
    {
        if (!mainScrollRect || mainViewport == null)
        {
            Debug.LogError("Scroll Rect ou Viewport não está ligado no ResizeGripHandler.");
            return;
        }

        Vector3[] viewportWorldCorners = new Vector3[4];
        // Obtém as coordenadas de canto do Viewport
        mainViewport.GetWorldCorners(viewportWorldCorners);

        // Converte os cantos mundiais para coordenadas de tela
        Camera cam = null; // Assumindo Screen Space Overlay ou Camera correta
        if (mainViewport.GetComponentInParent<Canvas>()?.renderMode == RenderMode.ScreenSpaceCamera)
            cam = mainViewport.GetComponentInParent<Canvas>().worldCamera;

        float viewportBottomScreenY = RectTransformUtility.WorldToScreenPoint(cam, viewportWorldCorners[0]).y; // Canto Inferior
        float viewportTopScreenY = RectTransformUtility.WorldToScreenPoint(cam, viewportWorldCorners[2]).y;    // Canto Superior (ajustado de wc[1])


        // --- Lógica de Detecção de Borda ---

        // Scroll para BAIXO (mouse abaixo do limite inferior)
        // downAmount > 0 se screenPos.y < (viewportBottomScreenY + edgeHotZonePx)
        float downAmount = Mathf.Clamp01(((viewportBottomScreenY + edgeHotZonePx) - screenPos.y) / edgeHotZonePx);

        // Scroll para CIMA (mouse acima do limite superior)
        // upAmount > 0 se screenPos.y > (viewportTopScreenY - edgeHotZonePx)
        float upAmount = Mathf.Clamp01((screenPos.y - (viewportTopScreenY - edgeHotZonePx)) / edgeHotZonePx);


        float dir = 0f;
        float amt = 0f;

        // Se o mouse desceu para a zona quente inferior
        if (downAmount > 0f)
        {
            dir = -1f; // Scroll para baixo diminui a verticalNormalizedPosition
            amt = downAmount;
        }
        else if (upAmount > 0f)
        {
            // Bloqueia o scroll para cima, conforme seu requisito
            return;
        }
        else return;

        float speedPx = amt * maxScrollSpeedPxPerSec;
        float contentH = mainScrollRect.content.rect.height;
        float viewportH = mainViewport.rect.height;
        float scrollable = Mathf.Max(1f, contentH - viewportH);

        float deltaNorm = (speedPx * Time.unscaledDeltaTime) / scrollable;

        mainScrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            mainScrollRect.verticalNormalizedPosition + dir * deltaNorm
        );
    }
}