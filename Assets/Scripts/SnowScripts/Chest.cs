using UnityEngine;

public class Chest : MonoBehaviour
{
    [SerializeField] private Sprite _openSprite;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player" && collision.GetComponent<PlayerStats>().HasKey)
        {
            Debug.Log("Get Loot Idiot");
            this.GetComponent<SpriteRenderer>().sprite = _openSprite;
            collision.GetComponent<PlayerStats>().Key(false);
        }
    }
}
