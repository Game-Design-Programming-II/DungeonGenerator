using UnityEngine;

namespace Interactibles
{
    public class Chest : MonoBehaviour
    {
        private bool _hasActivationed;
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private GameObject _canvas;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_hasActivationed)
            {
                return;
            }
            if (collision.tag == "Player" && collision.GetComponent<PlayerStats>().HasKey)
            {
                Debug.Log("Get Loot Idiot");
                //_canvas.GetComponent<PopUpText>().PopUp("Get Loot Idiot");
                this.GetComponent<SpriteRenderer>().sprite = _openSprite;
                collision.GetComponent<PlayerStats>().Key(false);
                _hasActivationed = true;
            }
        }
    }
}

