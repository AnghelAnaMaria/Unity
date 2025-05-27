using System;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public class LowEntropyCell : IComparable<LowEntropyCell>
    {
        public Vector2Int position { get; }
        public float entropy { get; }
        private float smallEntropyNoise;

        public LowEntropyCell(Vector2Int position, float entropy)
        {
            smallEntropyNoise = UnityEngine.Random.Range(0.001f, 0.005f);
            this.entropy = entropy + smallEntropyNoise;
            this.position = position;
        }

        public int CompareTo(LowEntropyCell other)
        {
            // Sort by entropy, then position.x, then position.y (to avoid duplicates)
            int cmp = entropy.CompareTo(other.entropy);
            if (cmp != 0) return cmp;
            cmp = position.x.CompareTo(other.position.x);
            if (cmp != 0) return cmp;
            return position.y.CompareTo(other.position.y);
        }

        public override bool Equals(object obj)
        {
            if (obj is LowEntropyCell other)
                return position.Equals(other.position);
            return false;
        }

        public override int GetHashCode()
        {
            return position.GetHashCode();
        }
    }
}
