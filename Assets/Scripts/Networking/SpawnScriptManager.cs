using UnityEngine;

public class SpawnScriptManager : MonoBehaviour
{
    public GameObject networkManager;

    void Awake()
    {
        if (FindAnyObjectByType<NetworkManager>() == null)
        {
            Instantiate(networkManager);
        }
    }
}
