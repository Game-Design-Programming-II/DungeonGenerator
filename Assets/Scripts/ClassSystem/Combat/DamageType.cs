using UnityEngine;

namespace ClassSystem.Combat
{
    [CreateAssetMenu(menuName = "RPG/Damage Type", fileName = "New DamageType")]
    public class DamageType : ScriptableObject
    {
        public Color color = Color.white;
    }
}

