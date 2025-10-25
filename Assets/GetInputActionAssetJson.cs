using UnityEngine;
using UnityEngine.InputSystem;

public class GetInputActionAssetJson : MonoBehaviour
{
    public InputActionAsset myInputActionAsset; // Assign in Inspector

    void Start()
    {
        if (myInputActionAsset != null)
        {
            foreach (var map in myInputActionAsset.actionMaps)
            {
                Debug.Log("Action Map: " + map.name);
                string jsonString = map.ToJson();
                Debug.Log("Input Action Asset JSON:\n" + jsonString);
            }
        }
    }
}