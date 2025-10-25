using System.Collections.Generic;
using MapGeneration;
using Photon.Pun;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Central coordinator for multiplayer sessions.
    /// Responsible for instantiating the local player once Photon is connected
    /// and relaying spawn positions supplied by the dungeon generator.
    /// </summary>
    public class MultiplayerGameManager : MonoBehaviourPunCallbacks
    {
        public static MultiplayerGameManager Instance { get; private set; }

        [Header("Player Prefab")]
        [Tooltip("Resource path used with PhotonNetwork.Instantiate. Prefab must live under a Resources folder.")]
        [SerializeField] private string networkPlayerPrefabPath = "Characters/Player";

        [Header("Spawn Sources")]
        [Tooltip("Optional reference so we can listen for procedural spawn updates from the dungeon generator.")]
        [SerializeField] private Generator generator;

        private readonly Dictionary<int, GameObject> playerInstances = new Dictionary<int, GameObject>();
        private Vector3? lastKnownSpawnPoint;
        private bool localPlayerSpawned;
        private bool lobbyStartedGame;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void OnEnable()
        {
            if (generator != null)
            {
                generator.PlayerSpawnPointUpdated += HandleSpawnPointUpdated;
            }
        }

        private void OnDisable()
        {
            if (generator != null)
            {
                generator.PlayerSpawnPointUpdated -= HandleSpawnPointUpdated;
            }
        }

        private void HandleSpawnPointUpdated(Vector3 spawnPosition)
        {
            lastKnownSpawnPoint = spawnPosition;
            TrySpawnLocalPlayer();
        }

        private void TrySpawnLocalPlayer()
        {
            if (localPlayerSpawned) return;
            if (!PhotonNetwork.IsConnectedAndReady) return;
            if (!lobbyStartedGame)
            {
                Debug.LogWarning("[Multiplayer] Ignoring spawn — lobby has not signalled game start.");
                return;
            }
            if (string.IsNullOrWhiteSpace(networkPlayerPrefabPath))
            {
                Debug.LogError("[Multiplayer] Network player prefab path not assigned.");
                return;
            }

            if (!lastKnownSpawnPoint.HasValue)
            {
                Debug.LogWarning("[Multiplayer] Waiting for dungeon spawn point before spawning local player.");
                return;
            }

            Vector3 spawnPosition = lastKnownSpawnPoint.Value;
            GameObject playerObject = PhotonNetwork.Instantiate(networkPlayerPrefabPath, spawnPosition, Quaternion.identity);
            RegisterPlayerInstance(PhotonNetwork.LocalPlayer.ActorNumber, playerObject);
            localPlayerSpawned = true;
        }

        [PunRPC]
        public void BeginMatch()
        {
            lobbyStartedGame = true;
            TrySpawnLocalPlayer();
        }

        public void RegisterPlayerInstance(int actorNumber, GameObject playerObject)
        {
            if (playerInstances.TryGetValue(actorNumber, out GameObject existing) && existing != null && existing != playerObject)
            {
                Destroy(existing);
            }

            playerInstances[actorNumber] = playerObject;
        }

        public void UnregisterPlayerInstance(int actorNumber, GameObject playerObject)
        {
            if (playerInstances.TryGetValue(actorNumber, out GameObject existing) && existing == playerObject)
            {
                playerInstances.Remove(actorNumber);
            }
        }
    }
}
