using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClassSystem.Combat
{
    [Serializable]
    public struct DamagePacket
    {
        public DamageType type;
        public float amount;
    }

    [Serializable]
    public class DamageBundle
    {
        public List<DamagePacket> packets = new List<DamagePacket>();

        public DamageBundle() { }
        public DamageBundle(params DamagePacket[] arr)
        {
            packets.AddRange(arr);
        }
    }
}

