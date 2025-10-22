using UnityEngine;

public class healthPack : MonoBehaviour
{
    [SerializeField] private GameObject heathpack;
    private void OnTriggerEnter(Collider other)
    {
        

        if (curPlayerhealth < maxplayerhealth)
        {
            curPlayerhealth == maxplayerhealh;

        }
        else
            return;
    }
}
