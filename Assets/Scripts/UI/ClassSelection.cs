using UnityEngine;

public class ClassSelection : MonoBehaviour
{
    [SerializeField] private Animator _anim;
    private void OnMouseEnter()
    {
        _anim.SetBool("hovered", true);
        
    }

    private void OnMouseExit()
    {
        _anim.SetBool("hovered", false);
    }
}
