using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

[RequireComponent(typeof(RectTransform))]
public class ReorderableListItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //mudança
    [Header("Refs (preenchidos pelo UnitListPanel)")]
    [SerializeField] RectTransform content;
    [SerializeField] RectTransform dragRoot;
    [SerializeField] Canvas canvasUI;
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform viewport;

    [Header("Auto-Scroll")]
    [SerializeField] float edgeHotZonePx = 80f;
    [SerializeField] float maxScrollSpeedPxPerSec = 900f;

    [Header("Drag Tuning")]
    [SerializeField] float dragDeadZonePx = 8f;
    [Range(0f, 0.5f)][SerializeField] float swapDeadZonePercent = 0.30f;
    [SerializeField] Color placeholderColor = new(1, 1, 1, 0.15f);
    // Anti-flip (estado)
    Vector2 _lastIndexChangePointer;
    float _lastStableDirY = 0f;


    RectTransform _rt;
    CanvasGroup _cg;
    GameObject _placeholderGO;
    RectTransform _placeholderRT;

    bool _dragging;
    bool _wasReparented;
    int _startIndex = -1;
    int _targetIndex = -1;
    Vector2 _dragStartScreen;
    Vector2 _lastScreen;

    UnitListPanel _panel;
    UnitListItemHandle _handle;

    // aninhamento
    RectTransform _currentContent;
    GroupListItemUI _hoveredGroupUI = null;

    // NOVO: canvas temporário para ficar por cima de tudo durante o drag
    Canvas _tempCanvas;
    GraphicRaycaster _tempRaycaster;
    int _oldSortingOrder;
    bool _oldOverride;

    public UnitListItemHandle OwnerHandle => _handle;
    public Canvas Canvas => canvasUI;

    // Estado adicional (adicione no topo da classe)
    struct ItemRectInfo
    {
        public RectTransform rt;
        public float top;
        public float bottom;
        public float center;
    }

    readonly System.Collections.Generic.List<ItemRectInfo> _rectCache = new();
    int _lockedNeighborIndex = -1;       // vizinho “grudado” durante a aproximação
    float _lockReleaseThresholdPx = 12f; // histerese para soltar o lock
    float _minSwapTravelPx = 6f;         // distância mínima desde a última troca

    public void Inject(RectTransform contentRt,
                        RectTransform dragRootRt,
                        Canvas canvas,
                        ScrollRect sr,
                        RectTransform vp,
                        UnitListPanel owner)
    {
        content = contentRt;
        dragRoot = dragRootRt;
        canvasUI = canvas;
        scrollRect = sr;
        viewport = vp;
        _panel = owner;
    }

    void Awake()
    {
        _rt = (RectTransform)transform;
        if (!canvasUI) canvasUI = GetComponentInParent<Canvas>();
        if (!viewport && scrollRect) viewport = scrollRect.viewport;
        if (!content && scrollRect) content = scrollRect.content;
        if (!_panel) _panel = GetComponentInParent<UnitListPanel>();
        _handle = GetComponent<UnitListItemHandle>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _dragStartScreen = e.position;
        _lastScreen = e.position;

        CreatePlaceholder();

        _currentContent = content;
        _startIndex = _rt.GetSiblingIndex();
        _targetIndex = _startIndex;

        var root = dragRoot ? dragRoot : (RectTransform)canvasUI.transform;
        _wasReparented = true;
        _rt.SetParent(root, true);
        _rt.SetAsLastSibling();

        // CanvasGroup
        _cg = gameObject.GetComponent<CanvasGroup>();
        if (!_cg) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.blocksRaycasts = false;
        _cg.alpha = 0.95f;

        // NOVO: canvas temporário para ficar acima de nested canvases / masks
        _tempCanvas = gameObject.GetComponent<Canvas>();
        if (_tempCanvas == null) _tempCanvas = gameObject.AddComponent<Canvas>();
        _tempRaycaster = gameObject.GetComponent<GraphicRaycaster>();
        if (_tempRaycaster == null) _tempRaycaster = gameObject.AddComponent<GraphicRaycaster>();

        _oldOverride = _tempCanvas.overrideSorting;
        _oldSortingOrder = _tempCanvas.sortingOrder;

        _tempCanvas.overrideSorting = true;
        _tempCanvas.sortingOrder = 5000;

        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
        _rt.pivot = new Vector2(0.5f, 0.5f);
        FollowPointer(e);

        _dragging = true;
        _lastIndexChangePointer = _dragStartScreen;
        _lastStableDirY = 0f;

    }

    public void OnDrag(PointerEventData e)
    {
        if (!_dragging) return;
        if ((e.position - _dragStartScreen).sqrMagnitude < dragDeadZonePx * dragDeadZonePx)
            return;

        _lastScreen = e.position;
        FollowPointer(e);

        if (TryHandleNesting(e))
        {
            UpdatePlaceholderIndex(e);
        }
        else
        {
            // ⚠️ “Raiz” precisa ser o content da LISTA PRINCIPAL, não o content injetado (que pode ser subContent)
            var rootContent = _panel ? _panel.Content : content;
            if (_currentContent != rootContent && _placeholderRT != null)
            {
                _placeholderRT.SetParent(rootContent, false);
                _currentContent = rootContent;
            }
            UpdatePlaceholderIndex(e);
        }

        DoAutoScroll(e.position);
    }


    public void OnEndDrag(PointerEventData e)
    {
        if (!_dragging) return;
        _dragging = false;

        // 1) Quem está sob o cursor agora?
        GroupListItemUI hitGroup = null;
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(e, results);
            foreach (var r in results)
            {
                var dz = r.gameObject.GetComponent<GroupDropZone>();
                if (dz != null) { hitGroup = dz.GetComponentInParent<GroupListItemUI>(); break; }
            }
        }

        // 2) Decisão final: grupo pelo hit; fallback: pai do placeholder
        GroupListItemUI targetGroupUI = hitGroup;
        if (targetGroupUI == null && _placeholderRT != null)
        {
            var maybe = _placeholderRT.GetComponentInParent<GroupListItemUI>();
            if (maybe != null && maybe.subContent == _placeholderRT.parent)
                targetGroupUI = maybe;
        }

        // ---------- DROP EM GRUPO ----------
        if (targetGroupUI != null)
        {
            var u = _handle ? _handle.CurrentUnit : null;
            var g = targetGroupUI.GroupModel;

            if (u != null && _panel != null && g != null)
            {
                int toIndex = _placeholderRT ? _placeholderRT.GetSiblingIndex() : g.Units.Count;

                // De onde o item veio?
                var sourceGroup = _panel.FindGroupOfUnit(u);

                // ===== NO-OP: mesmo grupo e índice não mudou =====
                if (sourceGroup != null && sourceGroup.ID == g.ID)
                {
                    int fromIndex = sourceGroup.Units.IndexOf(u);

                    if (fromIndex == toIndex || fromIndex < 0)
                    {
                        // Nada a fazer no modelo nem rebuild.
                        // Apenas restaura o visual no lugar original.
                        if (_wasReparented)
                        {
                            // volta para o subContent do próprio grupo
                            _rt.SetParent(targetGroupUI.subContent, false);
                            _wasReparented = false;
                        }
                        _rt.SetSiblingIndex(Mathf.Max(0, fromIndex));

                        DestroyPlaceholder();
                        RestoreVisualsAfterDrag();
                        RestoreTempCanvas();
                        return;
                    }
                }

                // ===== MOVE/REORDENA DE VERDADE =====
                // Agora sim podemos desativar o GO porque haverá rebuild do subContent
                _rt.gameObject.SetActive(false);

                if (sourceGroup != null && sourceGroup.ID == g.ID)
                {
                    _panel.CommitReorderInsideGroup(u, g.ID, toIndex);
                }
                else
                {
                    _panel.CommitMoveToGroupAt(u, g.ID, toIndex);
                }
            }

            DestroyPlaceholder();
            RestoreTempCanvas();
            return;
        }

        // ---------- DROP NA RAIZ ----------
        var unit = _handle ? _handle.CurrentUnit : null;

        // Se a unit estava em um grupo → sair do grupo e inserir na raiz
        if (unit != null && _panel != null && _panel.FindGroupOfUnit(unit) != null)
        {
            _rt.gameObject.SetActive(false);                 // o Build() vai recriar tudo
            _panel.CommitMoveOutOfGroup(unit, _targetIndex); // chama Build()
            DestroyPlaceholder();
            RestoreTempCanvas();
            return;                                          // importante: não tentar “restaurar” este GO
        }

        // Caso raiz→raiz (reordenação visual simples)
        if (_wasReparented)
        {
            // volta para a hierarquia da raiz correta
            var root = _panel ? _panel.Content : content;
            _rt.SetParent(root, false);
            _wasReparented = false;
        }

        int childCount = (_panel ? _panel.Content : content).childCount;
        _targetIndex = Mathf.Clamp(_targetIndex, 0, Mathf.Max(0, childCount - 1));
        _rt.SetSiblingIndex(_targetIndex);

        DestroyPlaceholder();
        RestoreVisualsAfterDrag();
        RestoreTempCanvas();

        if (unit != null && _panel != null)
        {
            _panel.CommitReorderByUnit(unit, _targetIndex); // raiz→raiz não chama Build()
            _panel.RefreshItemIndices();
        }
    }


    void RestoreTempCanvas()
    {
        if (_tempCanvas != null)
        {
            _tempCanvas.overrideSorting = _oldOverride;
            _tempCanvas.sortingOrder = _oldSortingOrder;
        }
        // manter os componentes no GO para evitar GC excessivo
    }

    void RestoreVisualsAfterDrag()
    {
        if (_cg == null) _cg = GetComponent<CanvasGroup>();
        if (_cg != null)
        {
            _cg.blocksRaycasts = true;
            _cg.alpha = 1f;
        }

        if (_handle != null) _handle.IgnoreNextClickOnce();
    }

    // ReorderableListItem.cs (SUBSTITUIR TryHandleNesting)

    bool TryHandleNesting(PointerEventData e)
    {
        if (_handle?.CurrentUnit == null) return false;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(e, results);

        GroupListItemUI newHoveredGroup = null;
        foreach (var r in results)
        {
            var drop = r.gameObject.GetComponent<GroupDropZone>();
            if (drop != null)
            {
                newHoveredGroup = drop.GetComponentInParent<GroupListItemUI>();
                break;
            }
        }

        // ----- Sobre um grupo -----
        if (newHoveredGroup != null)
        {
            // Ignora drop no próprio content/grupo
            if (newHoveredGroup.subContent == _hoveredGroupUI)
            {
                // Trata como se não houvesse hover, continuando o reorder interno
                return true;
            }

            // CRÍTICO: Não precisamos mais do delay de hover (_hoverStartTime)

            if (newHoveredGroup.GroupModel != null && !newHoveredGroup.GroupModel.IsExpanded)
            {
                newHoveredGroup.GroupModel.IsExpanded = true;
                newHoveredGroup.RefreshVisuals();
                LayoutRebuilder.ForceRebuildLayoutImmediate(newHoveredGroup.subContent);
            }

            if (_placeholderRT != null && newHoveredGroup.subContent != null && _currentContent != newHoveredGroup.subContent)
            {
                _placeholderRT.SetParent(newHoveredGroup.subContent, false);
                _currentContent = newHoveredGroup.subContent;
            }
            return true; // operando dentro de grupo
        }

        // ----- fora de qualquer grupo: VOLTA PARA A RAIZ -----
        _hoveredGroupUI = null;

        var root = _panel ? _panel.Content : content;
        if (_placeholderRT != null && _currentContent != root)
        {
            _placeholderRT.SetParent(root, false);
            _currentContent = root;
        }
        return false; // operando na raiz
    }


    void CreatePlaceholder()
    {
        if (_placeholderGO) return;

        _placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
        _placeholderRT = (RectTransform)_placeholderGO.transform;
        _placeholderRT.SetParent(content, false);
        _placeholderRT.localScale = Vector3.one;

        var img = _placeholderGO.GetComponent<Image>();
        img.color = placeholderColor;
        img.raycastTarget = false;

        var le = _placeholderGO.GetComponent<LayoutElement>();
        le.preferredHeight = _rt.rect.height;
        le.preferredWidth = _rt.rect.width;

        int idx = _rt.GetSiblingIndex();
        _placeholderRT.SetSiblingIndex(idx);
    }

    void DestroyPlaceholder()
    {
        if (_placeholderGO)
        {
            Destroy(_placeholderGO);
            _placeholderGO = null;
            _placeholderRT = null;
        }
    }

    void FollowPointer(PointerEventData e)
    {
        if (!canvasUI) return;

        var root = (RectTransform)(dragRoot ? dragRoot : canvasUI.transform);
        Camera cam = canvasUI.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvasUI.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, e.position, cam, out var local))
        {
            _rt.anchoredPosition = local;
        }
    }

    // ReorderableListItem.cs (SUBSTITUIR UpdatePlaceholderIndex)

    void UpdatePlaceholderIndex(PointerEventData e)
    {
        if (_placeholderRT == null || _currentContent == null) return;

        // 1) Snapshot estável dos retângulos (ignora placeholder e item arrastado)
        BuildItemRectCache();

        // 2) Posição do ponteiro (Y) em coordenadas de TELA para comparação com os corners
        float pointerY = e.position.y;

        // 3) Direção atual do arrasto
        float deltaY = e.position.y - _lastScreen.y;
        float dirY = Mathf.Sign(deltaY);
        if (Mathf.Abs(deltaY) >= 1f && dirY != 0f)
            _lastStableDirY = dirY;

        // 4) Índice proposto usando zonas de troca com zona morta central
        int proposedIndex = FindInsertionIndexByPointerY(pointerY, swapDeadZonePercent);

        // 5) Vizinhança “grudada” para evitar flip entre dois itens adjacentes
        proposedIndex = ApplyNeighborLock(proposedIndex, pointerY);

        // 6) Histerese: só troca se se afastou o suficiente da última decisão
        int currentIndex = _placeholderRT.GetSiblingIndex();
        if (currentIndex != proposedIndex)
        {
            float movedSinceChange = Mathf.Abs(e.position.y - _lastIndexChangePointer.y);
            if (movedSinceChange < _minSwapTravelPx)
            {
                // Não troque ainda; mantenha índice atual
                _targetIndex = currentIndex;
                return;
            }
        }

        // 7) Direção estável: respeita intenção (para cima só antes; para baixo só depois)
        // Empurra a decisão na direção do movimento para eliminar ambiguidade na zona morta
        if (_lastStableDirY > 0f && proposedIndex < currentIndex)
        {
            // arrastando para cima e decisão coloca antes -> OK
        }
        else if (_lastStableDirY < 0f && proposedIndex > currentIndex)
        {
            // arrastando para baixo e decisão coloca depois -> OK
        }
        else if (_lastStableDirY != 0f)
        {
            // Se a proposta contradiz a direção, suavize mantendo o atual
            _targetIndex = currentIndex;
            return;
        }

        // 8) Aplica mudança
        int realCount = _rectCache.Count;
        int finalIndex = Mathf.Clamp(proposedIndex, 0, realCount);

        if (finalIndex != currentIndex)
        {
            _placeholderRT.SetSiblingIndex(finalIndex);
            _lastIndexChangePointer = e.position;

            // Atualiza/ativa lock de vizinho quando cruzamos um limiar
            TryUpdateNeighborLock(finalIndex, pointerY);
        }

        _targetIndex = _placeholderRT.GetSiblingIndex();
    }





    void DoAutoScroll(Vector2 screenPos)
    {
        if (!scrollRect || !viewport) return;

        Vector3[] wc = new Vector3[4];
        viewport.GetWorldCorners(wc);
        float bottom = RectTransformUtility.WorldToScreenPoint(null, wc[0]).y;
        float top = RectTransformUtility.WorldToScreenPoint(null, wc[1]).y;

        float upAmount = Mathf.Clamp01((screenPos.y - (top - edgeHotZonePx)) / edgeHotZonePx);
        float downAmount = Mathf.Clamp01(((bottom + edgeHotZonePx) - screenPos.y) / edgeHotZonePx);

        float dir = 0f;
        float amt = 0f;

        if (upAmount > 0f) { dir = +1f; amt = upAmount; }
        else if (downAmount > 0f) { dir = -1f; amt = downAmount; }
        else return;

        float speedPx = amt * maxScrollSpeedPxPerSec;
        float contentH = scrollRect.content.rect.height;
        float viewportH = viewport.rect.height;
        float scrollable = Mathf.Max(1f, contentH - viewportH);

        float deltaNorm = (speedPx * Time.unscaledDeltaTime) / scrollable;

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(
            scrollRect.verticalNormalizedPosition + dir * deltaNorm
        );
    }
    void BuildItemRectCache()
    {
        _rectCache.Clear();
        if (_currentContent == null) return;

        int n = _currentContent.childCount;
        Vector3[] corners = new Vector3[4];

        for (int i = 0; i < n; i++)
        {
            var child = _currentContent.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (child == _placeholderRT || child == _rt) continue; // ignora arrastado e placeholder

            child.GetWorldCorners(corners);
            float bottom = corners[0].y;
            float top = corners[1].y;
            float center = (top + bottom) * 0.5f;

            _rectCache.Add(new ItemRectInfo
            {
                rt = child,
                top = top,
                bottom = bottom,
                center = center
            });
        }

        // Ordena top→bottom para decisão consistente (lista vertical padrão)
        _rectCache.Sort((a, b) => b.top.CompareTo(a.top));
    }

    int FindInsertionIndexByPointerY(float pointerY, float deadZonePercent)
    {
        // Decide índice considerando zonas superior/inferior com zona morta central
        int countReal = _rectCache.Count;
        if (countReal == 0) return 0;

        int bestIndex = countReal; // default: fim

        for (int i = 0; i < countReal; i++)
        {
            float top = _rectCache[i].top;
            float bottom = _rectCache[i].bottom;
            float height = Mathf.Max(1f, top - bottom);

            float upperZone = top - height * deadZonePercent;
            float lowerZone = bottom + height * deadZonePercent;

            if (pointerY > upperZone)
            {
                bestIndex = i;        // antes do i
                break;
            }
            else if (pointerY < lowerZone)
            {
                bestIndex = i + 1;    // depois do i
            }
            // Se estiver na zona morta, não muda bestIndex aqui.
        }
        return Mathf.Clamp(bestIndex, 0, countReal);
    }

    int ApplyNeighborLock(int proposedIndex, float pointerY)
    {
        if (_lockedNeighborIndex < 0 || _lockedNeighborIndex >= _rectCache.Count)
            return proposedIndex;

        var neighbor = _rectCache[_lockedNeighborIndex];
        float center = neighbor.center;

        // Enquanto o ponteiro ficar dentro de uma banda ao redor do centro do vizinho, segura o índice
        if (Mathf.Abs(pointerY - center) <= _lockReleaseThresholdPx)
            return proposedIndex < _lockedNeighborIndex ? _lockedNeighborIndex : _lockedNeighborIndex + 1;

        // Saiu da banda: libera lock
        _lockedNeighborIndex = -1;
        return proposedIndex;
    }

    void TryUpdateNeighborLock(int newIndex, float pointerY)
    {
        // Decide qual vizinho “grudar” quando estamos oscilando entre dois índices adjacentes
        // Lock apenas se realmente estamos entre dois itens (janela estreita)
        int neighborA = newIndex - 1;
        int neighborB = newIndex;

        // Escolhe o mais próximo do pointer
        float bestDist = float.PositiveInfinity;
        int bestNeighbor = -1;

        if (neighborA >= 0 && neighborA < _rectCache.Count)
        {
            float d = Mathf.Abs(pointerY - _rectCache[neighborA].center);
            if (d < bestDist) { bestDist = d; bestNeighbor = neighborA; }
        }
        if (neighborB >= 0 && neighborB < _rectCache.Count)
        {
            float d = Mathf.Abs(pointerY - _rectCache[neighborB].center);
            if (d < bestDist) { bestDist = d; bestNeighbor = neighborB; }
        }

        // Ativa lock se estiver suficientemente próximo
        if (bestNeighbor >= 0 && bestDist <= _lockReleaseThresholdPx)
            _lockedNeighborIndex = bestNeighbor;
    }


    // setters
    public void SetContent(RectTransform rt) => content = rt;
    public void SetDragRoot(RectTransform rt) => dragRoot = rt;
    public void SetCanvas(Canvas c) => canvasUI = c;
    public void SetScrollRect(ScrollRect sr) => scrollRect = sr;
    public void SetViewport(RectTransform rt) => viewport = rt;
    public void SetOwner(UnitListPanel p) => _panel = p;
}
