using System;
using System.Collections.Generic;
using MapGeneration;
using UnityEngine;
using UnityEngine.Serialization;

namespace DungeonGenerator.Character
{
    /// <summary>
    /// Spawns the primary player prefab at the generator's designated spawn point
    /// and exposes an API for spawning additional players when needed.
    /// </summary>
    public class PlayerSpawnController : MonoBehaviour
    {
        [SerializeField] private Generator mapGenerator;
        [SerializeField] private Transform playerParent;
        [SerializeField, Tooltip("Initial player prefab to spawn when the map finishes generating.")]
        private GameObject primaryPlayerPrefab;

        private readonly List<GameObject> spawnedPlayers = new();
        private Vector3 lastSpawnPoint;
        private bool hasSpawnPoint;

        public event Action<GameObject> PlayerSpawned;
        public IReadOnlyList<GameObject> SpawnedPlayers => spawnedPlayers;

        private void OnEnable()
        {
            if (mapGenerator == null)
            {
                Debug.LogWarning($"{nameof(PlayerSpawnController)} requires a reference to the dungeon generator.", this);
                return;
            }

            mapGenerator.PlayerSpawnPointUpdated += HandleSpawnPointUpdated;
            if (mapGenerator.HasPlayerSpawn)
            {
                HandleSpawnPointUpdated(mapGenerator.PlayerSpawnWorldPosition);
            }
        }

        private void OnDisable()
        {
            if (mapGenerator != null)
            {
                mapGenerator.PlayerSpawnPointUpdated -= HandleSpawnPointUpdated;
            }
        }

        private void HandleSpawnPointUpdated(Vector3 spawnPoint)
        {
            hasSpawnPoint = true;
            lastSpawnPoint = spawnPoint;
            SpawnPlayers();
        }

        private void SpawnPlayers()
        {
            ClearSpawnedPlayers();

            if (primaryPlayerPrefab == null)
            {
                Debug.LogWarning("Primary player prefab is not assigned.", this);
                return;
            }

            GameObject instance = SpawnPlayerInstance(primaryPlayerPrefab, lastSpawnPoint);
            if (instance != null)
            {
                PlayerSpawned?.Invoke(instance);
            }
        }

        public GameObject SpawnAdditionalPlayer(GameObject prefab)
        {
            if (!hasSpawnPoint)
            {
                Debug.LogWarning("Cannot spawn additional players before a spawn point is available.", this);
                return null;
            }

            if (prefab == null)
            {
                Debug.LogWarning("Attempted to spawn a null prefab.", this);
                return null;
            }

            GameObject instance = SpawnPlayerInstance(prefab, lastSpawnPoint);
            if (instance != null)
            {
                PlayerSpawned?.Invoke(instance);
            }

            return instance;
        }

        private GameObject SpawnPlayerInstance(GameObject prefab, Vector3 spawnPoint)
        {
            GameObject instance = Instantiate(prefab, spawnPoint, Quaternion.identity, playerParent);
            spawnedPlayers.Add(instance);
            return instance;
        }

        private void ClearSpawnedPlayers()
        {
            foreach (GameObject player in spawnedPlayers)
            {
                if (player != null)
                {
                    Destroy(player);
                }
            }

            spawnedPlayers.Clear();
        }
    }
}
