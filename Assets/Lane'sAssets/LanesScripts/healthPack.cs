using UnityEngine;

public class healthPack : MonoBehaviour
{
    [SerializeField] private GameObject heathpack;
    PlayerStats playerstats;
    
    private void Start()
    {
        playerstats = GetComponent<PlayerStats>();
        
    }

    private void OnTriggerEnter(Collider other)
    {

        
        if (playerstats.health < playerstats.maxHealth && other.gameObject.tag == "Player")
        {
            playerstats.health == playerstats.maxHealth;
            
            Destroy(this);
        
         }
        else
             return;
    }
}
