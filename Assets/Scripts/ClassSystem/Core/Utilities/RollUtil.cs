using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClassSystem.Core.Utilities
{
    [Serializable]
    public struct Dice
    {
        public int count;
        public int sides;
    }

    public static class RollUtil
    {
        public static int Roll(int count, int sides)
        {
            int total = 0;
            for (int i = 0; i < count; i++)
                total += UnityEngine.Random.Range(1, sides + 1);
            return total;
        }

        public static int Roll(IReadOnlyList<Dice> dice)
        {
            int total = 0;
            if (dice == null) return 0;
            for (int i = 0; i < dice.Count; i++)
            {
                var d = dice[i];
                if (d.count > 0 && d.sides > 0)
                    total += Roll(d.count, d.sides);
            }
            return total;
        }
    }
}

