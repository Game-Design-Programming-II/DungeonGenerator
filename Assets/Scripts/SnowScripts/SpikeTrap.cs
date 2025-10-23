using UnityEngine;
using System.Collections;

public class SpikeTrap : MonoBehaviour
{
    [Tooltip("The damage that the spike trap deals when activated")]
    [SerializeField] private float _damage;
    [SerializeField] private Animator _anim;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            _anim.Play("SpikeTrap");
            collision.TakeDamage(_damage);
        }
    }
}
