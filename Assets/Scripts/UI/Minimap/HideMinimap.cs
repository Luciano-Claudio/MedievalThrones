using UnityEngine;
using UnityEngine.UI;

public class HideMinimap : MonoBehaviour
{
    public Toggle toggle;
    public Animator minimapAnimator;
    bool isHide = false;
    bool isShow = true;

    private void Update()
    {
        if(toggle.isOn && isShow)
        {
            minimapAnimator.Play("Hide");
            isShow = false;
            isHide = true;
        }
        if (!toggle.isOn && isHide)
        {
            minimapAnimator.Play("Show");
            isHide = false;
            isShow = true;
        }
    }
}
