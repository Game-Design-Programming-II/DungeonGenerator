using System;
using UnityEngine;
using ClassSystem.Core;
using ClassSystem.Classes;
using ClassSystem.Combat;

namespace ClassSystem.Runtime
{
    [DisallowMultipleComponent]
    public class CharacterStats : MonoBehaviour, IDamageable
    {
        [Header("Class & Equipment")]
        public PlayerClass playerClass;
        public EquipmentManager equipment;

        [Header("Progression")]
        [Min(1)] public int level = 1;
        public int currentXp = 0;

        [Header("Vitals (runtime)")]
        public float currentHealth;
        public float currentMana;

        public event Action<float> OnDamaged;
        // Detailed damage event including the damage source object (e.g., Attacker, Transform of caster)
        public event Action<float, UnityEngine.Object> OnDamagedBy;
        public event Action<float> OnHealed;
        public event Action OnDied;

        void Reset()
        {
            equipment = GetComponent<EquipmentManager>();
            if (equipment == null) equipment = gameObject.AddComponent<EquipmentManager>();
        }

        void Start()
        {
            RecalculateVitals(fullRestore: true);
        }

        void Update()
        {
            RegenerateMana(Time.deltaTime);
        }

        public void AssignClass(PlayerClass newClass, bool fullRestore = true)
        {
            playerClass = newClass;
            level = 1;
            currentXp = 0;
            RecalculateVitals(fullRestore);
        }

        public float GetStat(StatType type)
        {
            if (playerClass == null || playerClass.statBlock == null) return 0f;
            return playerClass.statBlock.Get(type, level);
        }

        public float GetMaxHealth() => Mathf.Max(1f, GetStat(StatType.Health));
        public float GetMaxMana() => Mathf.Max(0f, GetStat(StatType.Mana));
        public int GetArmorClass()
        {
            int baseAC = Mathf.RoundToInt(GetStat(StatType.Defense));
            return equipment != null ? equipment.GetArmorClass(baseAC) : baseAC;
        }

        public int GetAttackRating()
        {
            return Mathf.RoundToInt(GetStat(StatType.Dexterity));
        }

        public void AddExperience(int amount)
        {
            if (playerClass == null || amount <= 0) return;
            currentXp += amount;
            var next = playerClass.GetXpToNextLevel(level);
            while (currentXp >= next)
            {
                currentXp -= next;
                level++;
                RecalculateVitals(fullRestore: true);
                next = playerClass.GetXpToNextLevel(level);
            }
        }

        void RecalculateVitals(bool fullRestore)
        {
            float maxHP = GetMaxHealth();
            float maxMP = GetMaxMana();
            if (fullRestore || currentHealth <= 0f) currentHealth = maxHP;
            else currentHealth = Mathf.Min(currentHealth, maxHP);

            if (fullRestore) currentMana = maxMP;
            else currentMana = Mathf.Min(currentMana, maxMP);
        }

        void RegenerateMana(float deltaTime)
        {
            float regen = Mathf.Max(0f, GetStat(StatType.ManaRegen));
            if (regen <= 0f) return;
            currentMana = Mathf.Min(GetMaxMana(), currentMana + regen * deltaTime);
        }

        public bool TryConsumeMana(float cost)
        {
            if (currentMana < cost) return false;
            currentMana -= cost;
            return true;
        }

        public void ReceiveDamage(DamageBundle bundle, UnityEngine.Object source = null)
        {
            if (bundle == null) return;
            float total = 0f;
            for (int i = 0; i < bundle.packets.Count; i++)
            {
                var p = bundle.packets[i];
                float amt = p.amount;
                if (equipment != null && p.type != null)
                {
                    amt *= equipment.GetResistanceFactor(p.type);
                }
                total += Mathf.Max(0f, amt);
            }
            if (total <= 0f) return;
            currentHealth -= total;
            OnDamaged?.Invoke(total);
            OnDamagedBy?.Invoke(total, source);
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                OnDied?.Invoke();
            }
        }

        public void ReceiveHealing(float amount, UnityEngine.Object source = null)
        {
            if (amount <= 0f) return;
            float before = currentHealth;
            currentHealth = Mathf.Min(GetMaxHealth(), currentHealth + amount);
            OnHealed?.Invoke(currentHealth - before);
        }

        public Transform GetTransform() => transform;
    }
}
