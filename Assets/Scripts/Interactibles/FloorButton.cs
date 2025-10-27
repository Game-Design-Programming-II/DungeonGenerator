using UnityEngine;

namespace Interactibles
{
    public class FloorButton : MonoBehaviour
    {
        private bool _hasActivationed;
        [SerializeField] private Animator _anim;
        [SerializeField] private GameObject _key;
        [SerializeField] private Vector3 _keySpawnPoint;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_hasActivationed)
            {
                return;
            }
            if (collision.tag == "Player")
            {
                _keySpawnPoint = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
                _anim.SetBool("steppedOn", true);
                Instantiate(_key, _keySpawnPoint, Quaternion.identity);
                _hasActivationed = true;
            }
        }
    }
}

