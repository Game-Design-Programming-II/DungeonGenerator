using UnityEngine;

namespace ClassSystem.Combat
{
    public static class CombatResolver
    {
        public static bool RollToHit(int attackerRating, int defenderArmorClass)
        {
            int d20 = Random.Range(1, 21);
            int total = d20 + Mathf.Max(0, attackerRating);
            int target = 10 + Mathf.Max(0, defenderArmorClass);
            if (d20 == 20) return true; // crit auto hit
            if (d20 == 1) return false; // fumble auto miss
            return total >= target;
        }
    }
}

