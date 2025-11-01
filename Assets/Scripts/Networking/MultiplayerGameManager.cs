using System.Collections.Generic;
using MapGeneration;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Networking
{
    /// <summary>
    /// Central coordinator for multiplayer sessions.
    /// Responsible for instantiating the local player once Photon is connected
    /// and relaying spawn positions supplied by the dungeon generator.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class MultiplayerGameManager : MonoBehaviourPunCallbacks
    {
        public static MultiplayerGameManager Instance { get; private set; }

        [Header("Player Prefab")]
        [Tooltip("Resource path used with PhotonNetwork.Instantiate. Prefab must live under a Resources folder.")]
        [SerializeField] private string networkPlayerPrefabPath = "Characters/Player/BasePlayer";

        [Header("Spawn Sources")]
        [Tooltip("Optional reference so we can listen for procedural spawn updates from the dungeon generator.")]
        [SerializeField] private Generator generator;

        private readonly Dictionary<int, GameObject> playerInstances = new Dictionary<int, GameObject>();
        private Vector3? lastKnownSpawnPoint;
        private bool localPlayerSpawned;
        private bool lobbyStartedGame;
        private int matchSeed;
        private bool hasSeed;
        private bool healthUiInitialized;

        private PhotonView cachedView;

        // Raised after the local network player object is instantiated.
        public event System.Action<GameObject> LocalPlayerSpawned;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            cachedView = GetComponent<PhotonView>();
            if (cachedView == null)
            {
                cachedView = gameObject.AddComponent<PhotonView>();
            }

            if (cachedView.ViewID == 0)
            {
                cachedView.ViewID = 1001;
            }
            cachedView.OwnershipTransfer = OwnershipOption.Fixed;

            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            HookGeneratorIfNeeded();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
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

            // Notify listeners (camera, UI, etc.) that our local player exists.
            try
            {
                LocalPlayerSpawned?.Invoke(playerObject);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Multiplayer] Error notifying LocalPlayerSpawned: {ex.Message}");
            }

            // Initialize Health UI once the local PlayerStats singleton exists
            TryInitHealthUI();
        }

        private void TryInitHealthUI()
        {
            if (healthUiInitialized) return;
            var controller = FindAnyObjectByType<HealthBarController>();
            if (controller == null) return;
            try
            {
                controller.GenerateHealthDisplay();
                healthUiInitialized = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Multiplayer] Failed to initialize Health UI: {ex.Message}");
            }
        }

        [PunRPC]
        public void BeginMatch(int seed)
        {
            lobbyStartedGame = true;
            matchSeed = seed;
            hasSeed = true;
            Debug.Log("[Multiplayer] BeginMatch RPC received.");
            if (PhotonNetwork.IsMasterClient)
            {
                HookGeneratorIfNeeded();
            }

            // If we have a generator in the active scene, kick off generation with the shared seed.
            if (generator != null)
            {
                try
                {
                    generator.GenerateWithSeed(matchSeed);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Multiplayer] Error starting generation with seed: {ex.Message}");
                }
            }
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[Multiplayer] Scene loaded: {scene.name}. Hooking generator...");
            HookGeneratorIfNeeded();
            
            // If match already began and we have a seed, start generation now.
            if (lobbyStartedGame && hasSeed && generator != null)
            {
                try
                {
                    generator.GenerateWithSeed(matchSeed);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[Multiplayer] Error starting generation after scene load: {ex.Message}");
                }
            }
        }

        private void HookGeneratorIfNeeded()
        {
            if (generator != null)
            {
                generator.PlayerSpawnPointUpdated -= HandleSpawnPointUpdated;
            }

            if (generator == null)
            {
                generator = FindAnyObjectByType<Generator>();
            }

            if (generator != null)
            {
                generator.PlayerSpawnPointUpdated -= HandleSpawnPointUpdated;
                generator.PlayerSpawnPointUpdated += HandleSpawnPointUpdated;

                Debug.Log("[Multiplayer] Generator found and event hooked.");
                if (generator.HasPlayerSpawn)
                {
                    Debug.Log("[Multiplayer] Generator already has a spawn point; using cached value.");
                    HandleSpawnPointUpdated(generator.PlayerSpawnWorldPosition);
                }
            }
        }
    }
}
