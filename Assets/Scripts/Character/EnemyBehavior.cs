using System.Collections.Generic;
using DungeonGenerator.Character;
using UnityEngine;

namespace DungeonGenerator.Character
{
    /// <summary>
    /// Simple chasing enemy that moves toward the nearest visible player.
    /// Requires a Rigidbody2D and optionally subscribes to PlayerSpawnController.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyBehavior : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private float acceleration = 8f;

        [Header("Detection")]
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private LayerMask lineOfSightObstacles = Physics2D.DefaultRaycastLayers;
        [SerializeField] private bool debugDrawLineOfSight;

        private readonly List<Transform> trackedPlayers = new List<Transform>();
        private Rigidbody2D body;
        private PlayerSpawnController spawnController;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
        }

        private void OnEnable()
        {
            spawnController = FindObjectOfType<PlayerSpawnController>();
            CacheExistingPlayers();

            if (spawnController != null)
            {
                spawnController.PlayerSpawned += HandlePlayerSpawned;
            }
        }

        private void OnDisable()
        {
            if (spawnController != null)
            {
                spawnController.PlayerSpawned -= HandlePlayerSpawned;
            }
        }

        private void FixedUpdate()
        {
            CleanupNullTargets();
            Transform target = AcquireVisibleTarget();

            if (target != null)
            {
                Vector2 toTarget = target.position - transform.position;
                Vector2 desiredVelocity = toTarget.normalized * moveSpeed;
                body.linearVelocity = Vector2.MoveTowards(
                    body.linearVelocity,
                    desiredVelocity,
                    acceleration * Time.fixedDeltaTime);
            }
            else
            {
                body.linearVelocity = Vector2.MoveTowards(
                    body.linearVelocity,
                    Vector2.zero,
                    acceleration * Time.fixedDeltaTime);
            }
        }

        private void CacheExistingPlayers()
        {
            PlayerCharacterController[] players = FindObjectsOfType<PlayerCharacterController>();
            foreach (PlayerCharacterController player in players)
            {
                RegisterPlayer(player.transform);
            }

            if (spawnController != null)
            {
                foreach (GameObject spawned in spawnController.SpawnedPlayers)
                {
                    if (spawned != null)
                    {
                        RegisterPlayer(spawned.transform);
                    }
                }
            }
        }

        private void HandlePlayerSpawned(GameObject player)
        {
            if (player != null)
            {
                RegisterPlayer(player.transform);
            }
        }

        private void RegisterPlayer(Transform playerTransform)
        {
            if (playerTransform == null) return;
            if (!trackedPlayers.Contains(playerTransform))
            {
                trackedPlayers.Add(playerTransform);
            }
        }

        private void CleanupNullTargets()
        {
            for (int i = trackedPlayers.Count - 1; i >= 0; i--)
            {
                if (trackedPlayers[i] == null)
                {
                    trackedPlayers.RemoveAt(i);
                }
            }
        }

        private Transform AcquireVisibleTarget()
        {
            float bestDistance = float.PositiveInfinity;
            Transform bestTarget = null;

            foreach (Transform candidate in trackedPlayers)
            {
                if (candidate == null) continue;

                Vector2 toTarget = candidate.position - transform.position;
                float distance = toTarget.magnitude;
                if (distance > detectionRange) continue;
                if (!HasLineOfSight(candidate.position, distance)) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = candidate;
                }
            }

            return bestTarget;
        }

        private bool HasLineOfSight(Vector3 targetPosition, float distance)
        {
            if (distance <= 0.001f)
            {
                return true;
            }

            Vector2 origin = transform.position;
            Vector2 direction = (Vector2)(targetPosition - transform.position);

            RaycastHit2D hit = Physics2D.Raycast(origin, direction.normalized, distance, lineOfSightObstacles);
            bool blocked = hit.collider != null;

            if (debugDrawLineOfSight)
            {
                Color color = blocked ? Color.red : Color.green;
                Debug.DrawLine(origin, origin + direction.normalized * Mathf.Min(distance, detectionRange), color);
            }

            return !blocked;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
#endif
    }
}
