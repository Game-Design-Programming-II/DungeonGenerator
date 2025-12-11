using UnityEngine;

namespace ClassSystem.Demo
{
    // Runtime-only helper: destroys the GameObject when its CharacterStats dies (health reaches zero).
    [DefaultExecutionOrder(1001)]
    public class DespawnOnDeath : MonoBehaviour
    {
        Runtime.CharacterStats _stats;
        bool _despawned;

        void Awake()
        {
            _stats = GetComponent<Runtime.CharacterStats>();
        }

        void OnEnable()
        {
            if (_stats != null)
                _stats.OnDied += HandleDied;
        }

        void OnDisable()
        {
            if (_stats != null)
                _stats.OnDied -= HandleDied;
        }

        void HandleDied()
        {
            Despawn();
        }

        void Despawn()
        {
            if (_despawned) return;
            _despawned = true;
            // Destroy at end of frame; other OnDied subscribers can finish.
            Destroy(gameObject);
        }
    }
}
