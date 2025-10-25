using UnityEngine;

public class DragRectRenderer : MonoBehaviour
{
    // REFATORAÇÃO: Não precisa mais de referência ao InputSelection
    // public InputSelection input; // REMOVIDO

    public Camera cam;
    public GameObject quadPrefab;

    GameObject _activeQuad;
    Vector3 _startWorld;

    void OnEnable()
    {
        // REFATORAÇÃO: Subscrever eventos via GameEvents
        GameEvents.OnDragBegin += BeginRect;
        GameEvents.OnDragging += UpdateRect;
        GameEvents.OnDragEnd += EndRect;
    }

    void OnDisable()
    {
        // REFATORAÇÃO: Desinscrever eventos via GameEvents
        GameEvents.OnDragBegin -= BeginRect;
        GameEvents.OnDragging -= UpdateRect;
        GameEvents.OnDragEnd -= EndRect;
    }

    void BeginRect(Vector2 screenStart)
    {
        if (!cam || !quadPrefab) return;

        if (Physics.Raycast(cam.ScreenPointToRay(screenStart), out var hit))
        {
            _startWorld = hit.point;
            _activeQuad = Instantiate(quadPrefab);
            _activeQuad.SetActive(true);
            UpdateRect(screenStart); // desenha um frame inicial
        }
    }

    void UpdateRect(Vector2 screenPos)
    {
        if (_activeQuad == null || !cam) return;

        if (Physics.Raycast(cam.ScreenPointToRay(screenPos), out var hit))
        {
            Vector3 endWorld = hit.point;

            Vector3 center = (_startWorld + endWorld) * 0.5f;
            Vector3 size = new Vector3(
                Mathf.Abs(endWorld.x - _startWorld.x),
                Mathf.Abs(endWorld.z - _startWorld.z),
                1f
            );
            center.y = 1f;
            _activeQuad.transform.position = center;
            _activeQuad.transform.localScale = size;
        }
    }

    void EndRect(Vector2 _)
    {
        if (_activeQuad != null)
        {
            Destroy(_activeQuad);
            _activeQuad = null;
        }
    }
}