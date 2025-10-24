using UnityEngine;
using UnityEngine.Events;

namespace DungeonGenerator.Character
{
    /// <summary>
    /// Basic pickup component that fires an event when a player walks over it.
    /// Intended to be paired with sprite-only GameObjects spawned by the dungeon generator.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PickupItem : MonoBehaviour
    {
        [Tooltip("Invoked when a player collects this pickup. The player GameObject is passed as the argument.")]
        public UnityEvent<GameObject> OnCollected;

        [Tooltip("Should the pickup destroy itself after firing the collected event.")]
        [SerializeField] private bool destroyOnCollect = true;

        private Collider2D cachedCollider;

        private void Awake()
        {
            cachedCollider = GetComponent<Collider2D>();
            if (cachedCollider != null)
            {
                cachedCollider.isTrigger = true;
            }
        }

        private void OnValidate()
        {
            if (cachedCollider == null)
            {
                cachedCollider = GetComponent<Collider2D>();
            }

            if (cachedCollider != null)
            {
                cachedCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            OnCollected?.Invoke(other.gameObject);

            if (destroyOnCollect)
            {
                Destroy(gameObject);
            }
        }
    }
}
