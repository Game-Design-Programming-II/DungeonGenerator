using UnityEngine;

public class PlayerDataContainer : MonoBehaviour
{
    void OnEnable()
    {
        GameManager.instance.SubscribePlayer(this);
    }

    void OnDisable()
    {
        GameManager.instance.UnsubscribePlayer(this);
    }
}
