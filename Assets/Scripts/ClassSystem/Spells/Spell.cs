using System.Collections.Generic;
using UnityEngine;
using ClassSystem.Combat;
using ClassSystem.Core.Utilities;

namespace ClassSystem.Spells
{
    public enum SpellTargetingMode { Self, Projectile }
    public enum SpellEffectKind { Damage, Heal }

    [CreateAssetMenu(menuName = "RPG/Spell", fileName = "New Spell")]
    public class Spell : ScriptableObject
    {
        [Header("Basics")]
        public string displayName = "Spell";
        public Sprite icon;
        public DamageType element;
        public float manaCost = 5f;
        [Tooltip("Seconds between casts")] public float cooldown = 0.5f;

        [Header("Effect")] 
        public SpellEffectKind effect = SpellEffectKind.Damage;
        [Tooltip("Total amount for instant casts, or per tick for continuous")] public int baseAmount = 5;
        public List<Dice> dice = new List<Dice>();
        [Tooltip("If true, applies effect repeatedly over Duration.")]
        public bool continuous = false;
        [Min(0.1f)] public float duration = 3f;
        [Min(0.05f)] public float tickInterval = 0.5f;

        [Header("Targeting")]
        public SpellTargetingMode targeting = SpellTargetingMode.Projectile;
        [Tooltip("Spawn point for projectile is provided by SpellCaster.muzzle if set; otherwise caster position.")]
        public GameObject projectilePrefab;
        public float projectileSpeed = 8f;
        public float projectileMaxDistance = 10f;
        public LayerMask hitLayers = ~0;

        [Header("VFX")]
        public GameObject castVfxPrefab;
        public GameObject impactVfxPrefab;

        public int RollAmount()
        {
            return baseAmount + RollUtil.Roll(dice);
        }
    }
}

