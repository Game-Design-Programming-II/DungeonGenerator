using System;
using System.Collections.Generic;
using UnityEngine;
using ClassSystem.Combat;

namespace ClassSystem.Items
{
    [Serializable]
    public class ResistanceEntry
    {
        public DamageType type;
        [Range(-1f, 1f)]
        [Tooltip("0.2 = 20% less damage. -0.3 = 30% more damage (weakness).")]
        public float reduction = 0f;
    }

    [CreateAssetMenu(menuName = "RPG/Armor", fileName = "New Armor")]
    public class Armor : ScriptableObject
    {
        [Tooltip("Higher AC makes you harder to hit.")]
        public int armorClass = 10;
        [Tooltip("Damage reductions per damage type.")]
        public List<ResistanceEntry> resistances = new List<ResistanceEntry>();
    }
}

