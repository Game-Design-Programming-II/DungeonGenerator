using UnityEngine;

namespace Interactibles
{
    public class Chest : MonoBehaviour
    {
        private bool _hasActivationed;
        [SerializeField] private Sprite _openSprite;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_hasActivationed)
            {
                return;
            }
            if (collision.tag == "Player" && collision.GetComponent<PlayerStats>().HasKey)
            {
                Debug.Log("Get Loot Idiot");
                this.GetComponent<SpriteRenderer>().sprite = _openSprite;
                collision.GetComponent<PlayerStats>().Key(false);
                _hasActivationed = true;
            }
        }
    }
}

