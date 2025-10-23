using UnityEngine;
public enum Animations : int
{
    Aparecer,
    Desaparecer
}
public class LeftBarAnimationController : MonoBehaviour
{
    Animator animator;
    bool isVisible = false;
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    public void LeftBarButton()
    {
        if (isVisible)
        {
            animator.Play(Animations.Desaparecer.ToString());
            isVisible = false;
            return;
        }
        animator.Play(Animations.Aparecer.ToString());
        isVisible = true;
    }
}
