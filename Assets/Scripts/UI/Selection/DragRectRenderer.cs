using UnityEngine;
using UnityEngine.UI;

public class DragRectRenderer : MonoBehaviour
{
    public InputSelection input;
    public Image rectImage;        // Image dentro do Canvas
    public Canvas canvas;          // Root canvas do retângulo

    RectTransform _canvasRect;     // cache
    Vector2 _startLocal;

    void Awake()
    {
        if (!canvas) canvas = rectImage.canvas;
        _canvasRect = canvas.GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        input.OnBeginDrag += BeginRect;
        input.OnDragging += UpdateRect;
        input.OnEndDrag += EndRect;
    }
    void OnDisable()
    {
        input.OnBeginDrag -= BeginRect;
        input.OnDragging -= UpdateRect;
        input.OnEndDrag -= EndRect;
    }

    Camera UICamera =>
        canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

    void BeginRect(Vector2 screenStart)
    {
        // Converte ponto de tela para coordenada local do canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenStart, UICamera, out _startLocal);

        rectImage.gameObject.SetActive(true);
        UpdateRect(screenStart); // desenha um frame inicial
    }

    void UpdateRect(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect, screenPos, UICamera, out var currentLocal);

        // calcula min/max em espaço local do canvas
        var min = Vector2.Min(_startLocal, currentLocal);
        var max = Vector2.Max(_startLocal, currentLocal);

        var center = (min + max) * 0.5f;
        var size = (max - min);

        var rt = rectImage.rectTransform;
        rt.anchoredPosition = center; // com pivot 0.5/0.5, essa é a posição do centro
        rt.sizeDelta = size;   // largura/altura
    }

    void EndRect(Vector2 _)
    {
        rectImage.gameObject.SetActive(false);
    }
}
