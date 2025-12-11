using System.Collections.Generic;
using UnityEngine;

namespace ClassSystem.Core
{
    [CreateAssetMenu(menuName = "RPG/Leveling Profile", fileName = "New LevelingProfile")]
    public class LevelingProfile : ScriptableObject
    {
        [Tooltip("XP required to reach each level (index 0 -> level 2, since level 1 is starting level)")]
        public List<int> xpToNextLevel = new List<int>() { 100, 200, 300, 400, 500 };

        [Tooltip("If list is too short, this formula is used to extrapolate.")]
        public AnimationCurve extrapolationCurve = AnimationCurve.EaseInOut(1, 100, 20, 5000);

        public int GetXpToNextLevel(int currentLevel)
        {
            int index = Mathf.Max(0, currentLevel - 1);
            if (index < xpToNextLevel.Count)
                return Mathf.Max(1, xpToNextLevel[index]);
            float eval = extrapolationCurve.Evaluate(currentLevel);
            return Mathf.Max(1, Mathf.RoundToInt(eval));
        }
    }
}

