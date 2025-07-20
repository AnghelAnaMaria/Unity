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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{//Strategia 2: consideram 2 patterns N*N ca fiind vecine daca au (N-1)*N tiles egale
    public class Neighbours2More : INeighbours
    {
        public Dictionary<int, PatternNeighbours> FindNeighbours(PatternResults patternDataResults)//in PatternDataResults avem matricea de pattern-uri din care salvam pt fiecare pattern care ii sunt vecinii.
        {
            var result = new Dictionary<int, PatternNeighbours>();

            foreach (var patternDataToCheck in patternDataResults.patternIndexDictionary)//pt fiecare pattern (cu index unic din dictionar)
            {//Prima buclă alege pattern-ul „sursă” A
                foreach (var possibleNeighbourForPattern in patternDataResults.patternIndexDictionary)//pt fiecare pattern
                {//A doua buclă îl ia pe fiecare alt pattern B (posibil vecin)
                    FindNeighboursInAllDirections(result, patternDataToCheck, possibleNeighbourForPattern);
                }
            }
            return result;

        }

        private void FindNeighboursInAllDirections(Dictionary<int, PatternNeighbours> result, KeyValuePair<int, PatternData> patternDataToCheck, KeyValuePair<int, PatternData> possibleNeighbourForPattern)
        {
            foreach (Dir dir in Enum.GetValues(typeof(Dir)))//cautam in toate directiile
            {
                if (patternDataToCheck.Value.CompareGrid(dir, possibleNeighbourForPattern.Value))//cautam ca pattern-urile (subgrilele N*N) sa aiba (N-1)*N casute egale
                {
                    //Daca au casutele mentionate egale, salvam pattern-urile ca fiind vecine.
                    if (!result.ContainsKey(patternDataToCheck.Key))
                    {
                        result.Add(patternDataToCheck.Key, new PatternNeighbours());
                    }
                    result[patternDataToCheck.Key].AddPatternToDictionary(dir, possibleNeighbourForPattern.Key);
                }
            }
        }

    }
}

