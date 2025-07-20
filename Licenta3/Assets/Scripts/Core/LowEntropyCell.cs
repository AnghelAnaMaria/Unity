// MIT License
// 
// Copyright (c) 2020 Sunny Valley Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// Modified by: Anghel Ana-Maria, iulie 2025 

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
