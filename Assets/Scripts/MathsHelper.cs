using Unity.VisualScripting;
using UnityEngine;

namespace MathsHelper
{
    [System.Serializable]
    public class UIntRange
    {
        [SerializeField] private uint _minValue;
        [SerializeField] private uint _maxValue;

        public uint GetMinValue { get { return _minValue; } }
        public uint GetMaxValue { get { return _maxValue; } }

        public uint GetRandomUintValue { get { return (uint)Random.Range((int)_minValue, (int)_maxValue + 1); } }
        public int GetRandomIntValue { get { return Random.Range((int)_minValue, (int)_maxValue + 1); } }
    }

    public class LineCalculator
    {
        public static bool IsIntersectingLine2D(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
        {
            bool isIntersecting = false;

            float denominator = (p4.y - p3.y) * (p2.x - p1.x) - (p4.x - p3.x) * (p2.y - p1.y);

            if (denominator != 0)
            {
                float u_a = ((p4.x - p3.x) * (p1.y - p3.y) - (p4.y - p3.y) * (p1.x - p3.x)) / denominator;
                float u_b = ((p2.x - p1.x) * (p1.y - p3.y) - (p2.y - p1.y) * (p1.x - p3.x)) / denominator;

                if (u_a >= 0 && u_a <= 1 && u_b >= 0 && u_b <= 1)
                {
                    isIntersecting = true;
                }
            }

            return isIntersecting;
        }
    }
}
