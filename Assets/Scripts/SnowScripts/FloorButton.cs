using UnityEngine;

public class FloorButton : MonoBehaviour
{
    [SerializeField] private Animator _anim;
    [SerializeField] private GameObject _key;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            _anim.SetBool("steppedOn", true);
            _key.SetActive(true);
        }
    }
}
