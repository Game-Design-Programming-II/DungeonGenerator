using UnityEngine;

namespace Interactibles
{
    public class Key : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.tag == "Player")
            {
                //collision.GetComponent<PlayerStats>().Key(true);
                GameObject chest = GameObject.FindGameObjectWithTag("Chest");
                chest.GetComponent<Chest>().AddKey(-1);
                Destroy(this.gameObject);
            }
        }
    }
}

