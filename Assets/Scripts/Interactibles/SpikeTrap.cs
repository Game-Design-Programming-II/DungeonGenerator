using UnityEngine;
using System.Collections.Generic;

namespace Interactibles
{
    public class SpikeTrap : MonoBehaviour
    {
        [Tooltip("The damage that the spike trap deals when activated")]
        [SerializeField] private float _damage;
        [SerializeField] private Animator _anim;
        private List<PlayerStats> _playersToDamage = new List<PlayerStats>();
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.tag == "Player")
            {
                _anim.SetBool("active", true);
                _playersToDamage.Add(collision.GetComponent<PlayerStats>());
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if(collision.tag == "Player")
            {
                _anim.SetBool("active", false);
                _playersToDamage.Remove(collision.GetComponent<PlayerStats>());
            }
        }

        public void Damage()
        {
            if(_playersToDamage.Count <= 0f)
            {
                return;
            }
            foreach (PlayerStats item in _playersToDamage)
            {
                item.TakeDamage(_damage);
            }
        }
    }
}

