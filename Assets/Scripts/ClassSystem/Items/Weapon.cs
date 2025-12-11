using System;
using System.Collections.Generic;
using UnityEngine;
using ClassSystem.Combat;
using ClassSystem.Core.Utilities;

namespace ClassSystem.Items
{
    [Serializable]
    public class WeaponDamageEntry
    {
        public DamageType type;
        [Min(0)] public int baseAmount = 0;
        public List<Dice> dice = new List<Dice>();

        public int RollDamage()
        {
            return baseAmount + RollUtil.Roll(dice);
        }
    }

    [CreateAssetMenu(menuName = "RPG/Weapon", fileName = "New Weapon")]
    public class Weapon : ScriptableObject
    {
        public WeaponType weaponType;
        [Tooltip("Dice-based damage split by types (e.g., Fire 4 + 2d6; Lightning 1 + 1d4)")]
        public List<WeaponDamageEntry> damage = new List<WeaponDamageEntry>();
        [Tooltip("Optional swing/cooldown time in seconds")] public float attackCooldown = 0.5f;

        public DamageBundle Roll()
        {
            var bundle = new DamageBundle();
            for (int i = 0; i < damage.Count; i++)
            {
                var entry = damage[i];
                bundle.packets.Add(new DamagePacket { type = entry.type, amount = entry.RollDamage() });
            }
            return bundle;
        }
    }
}

