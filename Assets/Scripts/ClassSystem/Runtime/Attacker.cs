using UnityEngine;
using ClassSystem.Combat;
using Character;
using System.Collections.Generic;
using CommonDamageType = Common.DamageType;

namespace ClassSystem.Runtime
{
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(EquipmentManager))]
    public class Attacker : MonoBehaviour
    {
        CharacterStats _stats;
        EquipmentManager _equip;
        float _lastAttackTime;
        [SerializeField] bool debugLogs = false;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _equip = GetComponent<EquipmentManager>();
        }

        public bool CanAttack()
        {
            if (_equip.weapon == null)
            {
                if (debugLogs) Debug.Log($"[Attacker] Cannot attack: no weapon on {name}.", this);
                return false;
            }
            if (Time.time < _lastAttackTime + _equip.weapon.attackCooldown)
            {
                if (debugLogs) Debug.Log($"[Attacker] Cannot attack: on cooldown for {_equip.weapon.attackCooldown - (Time.time - _lastAttackTime):0.00}s.", this);
                return false;
            }
            return true;
        }

        public bool TryAttack(IDamageable target)
        {
            if (!CanAttack() || target == null) return false;
            _lastAttackTime = Time.time;

            return ResolveAttack(target);
        }

        /// <summary>
        /// Attempts to hit all provided targets in a single swing (one cooldown gate).
        /// Each target rolls its own to-hit check using the current weapon.
        /// </summary>
        public bool TryAttackTargets(IEnumerable<IDamageable> targets)
        {
            if (!CanAttack() || targets == null) return false;

            bool attempted = false;
            bool hitSomething = false;
            foreach (var target in targets)
            {
                if (target == null) continue;
                attempted = true;
                if (ResolveAttack(target)) hitSomething = true;
            }

            if (attempted)
            {
                _lastAttackTime = Time.time;
            }

            return hitSomething;
        }

        /// <summary>
        /// Fallback path for targets that use CharacterHealth (legacy enemy health system).
        /// </summary>
        public bool TryAttack(CharacterHealth target)
        {
            if (!CanAttack() || target == null) return false;
            _lastAttackTime = Time.time;
            return ResolveAttack(target);
        }

        /// <summary>
        /// Multi-target variant for CharacterHealth recipients.
        /// </summary>
        public bool TryAttackTargets(IEnumerable<CharacterHealth> targets)
        {
            if (!CanAttack() || targets == null) return false;

            bool attempted = false;
            bool hitSomething = false;
            foreach (var target in targets)
            {
                if (target == null) continue;
                attempted = true;
                if (ResolveAttack(target)) hitSomething = true;
            }

            if (attempted)
            {
                _lastAttackTime = Time.time;
            }

            return hitSomething;
        }

        bool ResolveAttack(IDamageable target)
        {
            int atk = _stats.GetAttackRating();
            int defAC = 0;
            var targetStats = (target as Component)?.GetComponent<CharacterStats>();
            if (targetStats != null) defAC = targetStats.GetArmorClass();

            if (!CombatResolver.RollToHit(atk, defAC))
            {
                if (debugLogs) Debug.Log($"[Attacker] Miss vs {((Component)target)?.name ?? target.ToString()} (atk:{atk} vs ac:{defAC}).", this);
                return false; // miss
            }

            var dmg = _equip.weapon.Roll();
            target.ReceiveDamage(dmg, this);
            if (debugLogs) Debug.Log($"[Attacker] Hit IDamageable {((Component)target)?.name ?? target.ToString()} for {dmg?.packets?.Count ?? 0} packets.", this);
            return true;
        }

        bool ResolveAttack(CharacterHealth target)
        {
            int atk = _stats.GetAttackRating();
            // CharacterHealth has no armor; treat as AC 0.
            if (!CombatResolver.RollToHit(atk, 0))
            {
                if (debugLogs) Debug.Log($"[Attacker] Miss vs {target.name} (CharacterHealth).", this);
                return false;
            }

            var bundle = _equip.weapon.Roll();
            float total = 0f;
            if (bundle != null && bundle.packets != null)
            {
                for (int i = 0; i < bundle.packets.Count; i++)
                {
                    total += Mathf.Max(0f, bundle.packets[i].amount);
                }
            }
            if (total <= 0f) total = 1f;

            target.ApplyDamage(total, CommonDamageType.Physical, gameObject);
            if (debugLogs) Debug.Log($"[Attacker] Hit CharacterHealth {target.name} for {total:0.#} damage.", this);
            return true;
        }
    }
}
