using UnityEngine;
using System.Collections;

namespace Interactibles
{
    public class FloorButton : MonoBehaviour
    {
        private bool _hasActivationed;
        [SerializeField] private Animator _anim;
        [SerializeField] private AnimationClip _animation;
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
                //StartCoroutine("Wait");
            }
        }

        public void Spawn()
        {
            Debug.Log("Event");
            Instantiate(_key, _keySpawnPoint, Quaternion.identity);
            _hasActivationed = true;
        }

        /*
        public IEnumerator Wait()
        {
            
            yield return new WaitForSeconds(_animation.length);
            //AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
            //Debug.Log(stateInfo.IsTag("FloorButton"));
            //yield return new WaitUntil(() => stateInfo.normalizedTime >= 1f && stateInfo.IsTag("FloorButton"));
            
        }*/
    }
}

