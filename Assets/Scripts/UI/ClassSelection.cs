using UnityEngine;

public class ClassSelection : MonoBehaviour
{
    [SerializeField] private GameManager _gM;
    [SerializeField] private Animator _anim;
    [SerializeField] private AudioSource _aS;
    [Tooltip("The ID of the class this button selects.")]
    [SerializeField, Range(0,2)] private uint _classID;
    private bool _selected;
    private void OnMouseEnter()
    {
        _anim.SetBool("hovered", true);
        
    }

    private void OnMouseExit()
    {
        if (!_selected)
        {
            _anim.SetBool("hovered", false);
        }
    }

    private void OnMouseDown()
    {
        _selected = !_selected;
        _gM.SelectClass(_classID);
        _aS.Play();
    }
}
