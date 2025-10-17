using UnityEngine;
using UnityEngine.UI;

public class UnitContextMenuHandler : MonoBehaviour
{
    [Header("UI")]
    public Button followButton;

    [Header("Movimento da câmera")]
    public bool snap = false;
    public float moveDuration = 0.5f;

    UnitListItemContextMenu _owner;
    Transform _target;

    public void Setup(UnitListItemContextMenu owner, Transform target)
    {
        _owner = owner;
        _target = target;

        if (followButton)
        {
            followButton.onClick.RemoveAllListeners();
            followButton.onClick.AddListener(OnFollow);
        }
    }

    void OnFollow()
    {
        if (_target == null) return;

        var cam = Object.FindFirstObjectByType<RTSCameraCinemachineV3Controller>();
        if (cam != null)
        {
            // vai até a posição XZ da unit (mantém heading/zoom)
            cam.GoTo(_target.position, snap, moveDuration);
        }

        _owner?.CloseMenu();
    }
}
