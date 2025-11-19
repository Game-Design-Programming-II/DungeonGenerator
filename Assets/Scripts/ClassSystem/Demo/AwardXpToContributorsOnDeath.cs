using System.Collections.Generic;
using UnityEngine;

namespace ClassSystem.Demo
{
    // Tracks damage contributors and awards XP when this character dies.
    [RequireComponent(typeof(Runtime.CharacterStats))]
    public class AwardXpToContributorsOnDeath : MonoBehaviour
    {
        public int totalXp = 100;
        public bool proportionalToDamage = true; // if false, split evenly among contributors
        [Tooltip("Minimum damage a contributor must deal to earn XP (after resistances). 0 = any contribution counts.")]
        public float minContribution = 0f;

        Runtime.CharacterStats _self;
        readonly Dictionary<Runtime.CharacterStats, float> _contrib = new Dictionary<Runtime.CharacterStats, float>();
        bool _awarded;

        void Awake()
        {
            _self = GetComponent<Runtime.CharacterStats>();
        }

        void OnEnable()
        {
            if (_self == null) return;
            _self.OnDamagedBy += OnDamagedBy;
            _self.OnDied += OnDied;
        }

        void OnDisable()
        {
            if (_self == null) return;
            _self.OnDamagedBy -= OnDamagedBy;
            _self.OnDied -= OnDied;
        }

        void OnDamagedBy(float amount, Object source)
        {
            if (amount <= 0f || source == null) return;
            var attackerStats = ResolveStatsFromSource(source);
            if (attackerStats == null) return;
            if (attackerStats == _self) return; // ignore self-damage for XP

            if (_contrib.TryGetValue(attackerStats, out var cur))
                _contrib[attackerStats] = cur + amount;
            else
                _contrib[attackerStats] = amount;
        }

        void OnDied()
        {
            if (_awarded) return;
            _awarded = true;

            if (_contrib.Count == 0 || totalXp <= 0)
                return;

            // Filter by threshold
            var eligible = new List<KeyValuePair<Runtime.CharacterStats, float>>();
            float totalDamage = 0f;
            foreach (var kv in _contrib)
            {
                if (kv.Value >= minContribution)
                {
                    eligible.Add(kv);
                    totalDamage += kv.Value;
                }
            }
            if (eligible.Count == 0) return;

            if (proportionalToDamage && totalDamage > 0f)
            {
                foreach (var kv in eligible)
                {
                    int xp = Mathf.RoundToInt(totalXp * (kv.Value / totalDamage));
                    if (xp > 0) kv.Key.AddExperience(xp);
                }
            }
            else
            {
                int per = Mathf.Max(1, totalXp / eligible.Count);
                foreach (var kv in eligible)
                    kv.Key.AddExperience(per);
            }
        }

        static Runtime.CharacterStats ResolveStatsFromSource(Object source)
        {
            if (source == null) return null;
            // Common cases: Attacker component, Transform of caster, any Component
            if (source is Component c)
            {
                var s = c.GetComponentInParent<Runtime.CharacterStats>();
                if (s != null) return s;
            }
            if (source is GameObject go)
            {
                var s = go.GetComponentInParent<Runtime.CharacterStats>();
                if (s != null) return s;
            }
            if (source is Transform t)
            {
                var s = t.GetComponentInParent<Runtime.CharacterStats>();
                if (s != null) return s;
            }
            return null;
        }
    }
}

