using UnityEngine;

namespace Interactibles 
{
    public class HealthPack : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.tag == "Player" && collision.GetComponent<PlayerStats>().Health < collision.GetComponent<PlayerStats>().MaxHealth)
            {
                collision.GetComponent<PlayerStats>().Heal(1000);
                Destroy(gameObject);
            }
        }
    }
}

