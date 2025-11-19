using System.Collections.Generic;
using UnityEngine;
using ClassSystem.Core;
using ClassSystem.Spells;
using ClassSystem.Items;

namespace ClassSystem.Classes
{
    [CreateAssetMenu(menuName = "RPG/Player Class", fileName = "New PlayerClass")]
    public class PlayerClass : ScriptableObject
    {
        public string displayName = "Class";
        public Sprite icon;
        [Header("Leveling")]
        public LevelingProfile levelingProfile;
        [Header("Stats")]
        public StatBlock statBlock;
        [Header("Equipment Rules")]
        [Tooltip("Which weapon types this class can use.")]
        public List<WeaponType> allowedWeaponTypes = new List<WeaponType>();
        [Header("Spells")]
        public List<Spell> knownSpells = new List<Spell>();

        public bool Allows(WeaponType type)
        {
            return type == null || allowedWeaponTypes.Contains(type);
        }

        public int GetXpToNextLevel(int currentLevel)
        {
            return levelingProfile != null ? levelingProfile.GetXpToNextLevel(currentLevel) : 100;
        }
    }
}

