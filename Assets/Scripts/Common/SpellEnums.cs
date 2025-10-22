using UnityEngine;

namespace Common
{
    // Element/type used for damage/heal effects. Extend as needed.
    public enum DamageType
    {
        None,
        Fire,
        Ice,
        Holy,
        Force,
        Arcane,
        Nature,
        Physical
    }

    // How a spell reaches its target.
    public enum SpellTargeting
    {
        Self,
        Projectile
    }

    // How a spell applies its effect.
    public enum EffectDelivery
    {
        Instant,
        OverTime
    }
}