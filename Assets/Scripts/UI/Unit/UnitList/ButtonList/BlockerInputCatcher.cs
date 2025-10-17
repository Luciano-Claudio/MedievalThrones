using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class BlockerInputCatcher : MonoBehaviour, IPointerClickHandler
{
    public UnityAction onLeftClick;
    public UnityAction onRightClick;

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left)
            onLeftClick?.Invoke();
        else if (e.button == PointerEventData.InputButton.Right)
            onRightClick?.Invoke();
    }
}
