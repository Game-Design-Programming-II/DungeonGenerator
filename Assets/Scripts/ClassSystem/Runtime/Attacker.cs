using UnityEngine;
using ClassSystem.Combat;

namespace ClassSystem.Runtime
{
    [RequireComponent(typeof(CharacterStats))]
    [RequireComponent(typeof(EquipmentManager))]
    public class Attacker : MonoBehaviour
    {
        CharacterStats _stats;
        EquipmentManager _equip;
        float _lastAttackTime;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
            _equip = GetComponent<EquipmentManager>();
        }

        public bool CanAttack()
        {
            if (_equip.weapon == null) return false;
            if (Time.time < _lastAttackTime + _equip.weapon.attackCooldown) return false;
            return true;
        }

        public bool TryAttack(IDamageable target)
        {
            if (!CanAttack() || target == null) return false;
            _lastAttackTime = Time.time;

            int atk = _stats.GetAttackRating();
            int defAC = 0;
            var targetStats = (target as Component)?.GetComponent<CharacterStats>();
            if (targetStats != null) defAC = targetStats.GetArmorClass();

            if (!CombatResolver.RollToHit(atk, defAC))
                return false; // miss

            var dmg = _equip.weapon.Roll();
            target.ReceiveDamage(dmg, this);
            return true;
        }
    }
}

