using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class LowEntropyCell : IComparable<LowEntropyCell>, IEqualityComparer<LowEntropyCell>
    {
        public Vector2Int position { get; set; }
        public float entropy { get; set; }
        private float smallEntropyNoise;

        public LowEntropyCell(Vector2Int position, float entropy)
        {
            smallEntropyNoise = UnityEngine.Random.Range(0.001f, 0.005f);
            this.entropy = entropy + smallEntropyNoise;
            this.position = position;
        }

        public int CompareTo(LowEntropyCell other)
        {
            if (entropy > other.entropy) return 1;
            else if (entropy < other.entropy) return -1;
            else return 0;
        }

        public bool Equals(LowEntropyCell cell1, LowEntropyCell cell2)
        {
            return cell1.position.x == cell2.position.x && cell1.position.y == cell2.position.y;
        }

        public int GetHashCode(LowEntropyCell obj)//vrem ca daca 2 LowEntropyCell au acc pozitie(x,y) ele sa aiba acc hash.
        {
            return obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return position.GetHashCode();
        }

    }
}