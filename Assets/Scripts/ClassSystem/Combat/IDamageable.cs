using UnityEngine;

namespace ClassSystem.Combat
{
    public interface IDamageable
    {
        void ReceiveDamage(DamageBundle bundle, Object source = null);
        void ReceiveHealing(float amount, Object source = null);
        Transform GetTransform();
    }
}

