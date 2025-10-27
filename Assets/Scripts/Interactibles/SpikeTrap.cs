using UnityEngine;

namespace Interactibles
{
    public class SpikeTrap : MonoBehaviour
    {
        [Tooltip("The damage that the spike trap deals when activated")]
        [SerializeField] private float _damage;
        [SerializeField] private Animator _anim;
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.tag == "Player")
            {
                _anim.SetBool("active", true);
                collision.GetComponent<PlayerStats>().TakeDamage(_damage);
            }
        }
    }
}

