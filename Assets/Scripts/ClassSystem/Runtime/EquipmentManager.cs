using UnityEngine;
using ClassSystem.Items;
using ClassSystem.Combat;
using ClassSystem.Classes;

namespace ClassSystem.Runtime
{
    public class EquipmentManager : MonoBehaviour
    {
        [Header("Equipped Items")]        
        public Weapon weapon;
        public Armor armor;

        void OnValidate()
        {
            var stats = GetComponent<CharacterStats>();
            if (stats != null && stats.playerClass != null && weapon != null)
            {
                if (!stats.playerClass.Allows(weapon.weaponType))
                {
                    Debug.LogWarning($"{stats.playerClass.displayName} cannot equip weapon type {weapon.weaponType?.name}. Unequipping.", this);
                    weapon = null;
                }
            }
        }

        public bool TryEquipWeapon(Weapon newWeapon, PlayerClass playerClass, out string error)
        {
            error = null;
            if (newWeapon == null)
            {
                weapon = null;
                return true;
            }
            if (playerClass != null && !playerClass.Allows(newWeapon.weaponType))
            {
                error = $"Class {playerClass.displayName} is not proficient with {newWeapon.weaponType?.name}.";
                return false;
            }
            weapon = newWeapon;
            return true;
        }

        public void EquipArmor(Armor newArmor)
        {
            armor = newArmor;
        }

        public int GetArmorClass(int baseAC)
        {
            int ac = baseAC;
            if (armor != null)
                ac += armor.armorClass;
            return ac;
        }

        public float GetResistanceFactor(DamageType type)
        {
            float reduction = 0f;
            if (armor != null && armor.resistances != null)
            {
                for (int i = 0; i < armor.resistances.Count; i++)
                {
                    var r = armor.resistances[i];
                    if (r.type == type)
                        reduction += r.reduction;
                }
            }
            // Clamp so total reduction doesn't exceed 90% nor less than -90%
            reduction = Mathf.Clamp(reduction, -0.9f, 0.9f);
            return 1f - reduction;
        }
    }
}
