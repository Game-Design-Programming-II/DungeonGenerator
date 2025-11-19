using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClassSystem.Core
{
    [Serializable]
    public class StatEntry
    {
        public StatType stat;
        [Tooltip("Value at level 1")] public float baseValue = 0f;
        [Tooltip("How much this stat increases per level (additive)")]
        public float perLevel = 0f;

        public float GetValue(int level)
        {
            int lvl = Mathf.Max(1, level);
            return baseValue + perLevel * (lvl - 1);
        }
    }

    [CreateAssetMenu(menuName = "RPG/Stat Block", fileName = "New StatBlock")]
    public class StatBlock : ScriptableObject
    {
        [SerializeField] private List<StatEntry> entries = new List<StatEntry>();

        public float Get(StatType type, int level)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].stat == type)
                    return entries[i].GetValue(level);
            }
            return 0f;
        }

        public IReadOnlyList<StatEntry> Entries => entries;
    }
}

