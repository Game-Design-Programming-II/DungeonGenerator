using UnityEngine;
using MapGeneration;

namespace Interactibles
{
    public class Chest : MonoBehaviour
    {
        private bool _hasActivationed;
        private bool _canBeOpened;
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private GameObject _canvas;
        private int _neededKeys;
        private GameObject _gen;

        public void SetNeededKeys()
        {
            _gen = GameObject.FindGameObjectWithTag("Generator");
            _neededKeys = _gen.GetComponent<Generator>().GetPressurePR;
            Debug.Log(_neededKeys);
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_hasActivationed)
            {
                return;
            }
            if (collision.tag == "Player" && _canBeOpened)
            {
                Debug.Log("Get Loot Idiot");
                Instantiate<GameObject>(_canvas);
                //_canvas.GetComponent<PopUpText>().PopUp("Get Loot Idiot");
                this.GetComponent<SpriteRenderer>().sprite = _openSprite;
                //collision.GetComponent<PlayerStats>().Key(false);
                _hasActivationed = true;
            }
        }
        public void AddKey(int key)
        {
            /*if(_neededKeys == 0 && !_canBeOpened)
            {
                _gen = GameObject.FindGameObjectWithTag("Generator");
                _neededKeys = _gen.GetComponent<Generator>().GetPressurePR;
                Debug.Log(_neededKeys);
            }*/

            _neededKeys += key;
            Debug.Log(_neededKeys);
            if (_neededKeys <= 0)
            {
                Debug.Log("Can open");
                _canBeOpened = true;
            }
        }
    }
}

