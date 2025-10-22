using UnityEngine;

public class healthPack : MonoBehaviour
{
    [SerializeField] private GameObject heathpack;
    private void OnTriggerEnter(Collider other)
    {

        /*
        if (curPlayerhealth < maxplayerhealth && other.gameObject.tag == "Player")
        {
            curPlayerhealth == maxplayerhealh;
            Destroy(this);
        
         }
        else
             return;*/
    }
}
