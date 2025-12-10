using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;   
    private Dictionary<PlayerDataContainer, Transform> players = new Dictionary<PlayerDataContainer, Transform>();
    public event Action<Dictionary<PlayerDataContainer, Transform>> updatePlayers;
    public Dictionary<PlayerDataContainer, Transform> GetPlayers { get{return players;}}

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SubscribePlayer(PlayerDataContainer player)
    {
        if (!players.ContainsKey(player))
        {
            players.Add(player, player.transform);
            updatePlayers?.Invoke(players);
        }
    }

    public void UnsubscribePlayer(PlayerDataContainer player)
    {
        players.Remove(player);
        updatePlayers?.Invoke(players);
    }

    public void SelectClass(uint classID)
    {

    }
}
