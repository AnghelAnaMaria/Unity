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

using UnityEngine;

namespace WaveFunctionCollapse
{
    public class PatternData
    {//Pattern-uri din input
        private Pattern pattern;//pattern-ul în sine (sub-grila N×N cu indici)
        private int frequency = 1;//cate aparitii ale acestui pattern s-au găsit în input
        private float frequencyRelative;//frecvența relativă NORMALIZATA= frequency / totalul tuturor pattern-urilor
        private float frequencyRelativeLog2;//logaritmul în baza 2 din (frecventa relativa NORMALIZATA); Se foloseste pt entropie

        //Get methods:
        public float FrequencyRelative { get => frequencyRelative; }
        public Pattern Pattern { get => pattern; }
        public float FrequencyRelativeLog2 { get => frequencyRelativeLog2; }

        public PatternData(Pattern pattern)
        {
            this.pattern = pattern;
            // la început _frequency = 0, relativ/log2=0
        }

        public void AddToFrequency()
        {
            frequency++;
        }

        public void CalculateRelativeFrequency(int total)//totalul e acelasi lucru cu suma frecventelor/probabilitatilor pt patterns -> folosim la NORMALIZARE
        {
            frequencyRelative = (float)frequency / total;
            frequencyRelativeLog2 = Mathf.Log(frequencyRelative, 2);
        }

        public bool CompareGrid(Dir dir, PatternData data)
        {
            return pattern.ComparePatternToAnotherPattern(dir, data.Pattern);
        }
    }
}
