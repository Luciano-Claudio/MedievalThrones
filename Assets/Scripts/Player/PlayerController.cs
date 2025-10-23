using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Quem sou eu")]
    public FactionId myFaction = FactionId.Player1;

    [Header("Referências")]
    public Camera mainCamera;

    private void Reset()
    {
        mainCamera = Camera.main;
    }
}
