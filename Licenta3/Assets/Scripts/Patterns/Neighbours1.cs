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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{//Strategia 1: consideram 2 patterns N*N vecine daca sunt situate una langa alta in matricea de grile din PatternDataResults
    public class Neighbours1 : INeighbours
    {
        public Dictionary<int, PatternNeighbours> FindNeighbours(PatternResults patternDataResults)//PatternDataResults are matricea de patterns
        {
            var result = new Dictionary<int, PatternNeighbours>();
            FindNeighboursForEachPattern(patternDataResults, result);
            return result;
        }

        private void FindNeighboursForEachPattern(PatternResults patternDataResults, Dictionary<int, PatternNeighbours> result)
        {
            for (int row = 0; row < patternDataResults.GetGridLengthY(); row++)
            {
                for (int col = 0; col < patternDataResults.GetGridLengthX(); col++)
                {
                    //pt fiecare pattern gasim pattern-urile vecine din matricea de patterns de la PaternDataResults pt fiecare directie(un vecin sus, unul jos etc.)
                    PatternNeighbours neighbours = FindPatterns.CheckNeighboursInEachDirection(col, row, patternDataResults);

                    //adăugăm în dicționarul final si avem result= Dictionary<int, PatternNeighbours>
                    FindPatterns.AddNeighboursToDictionary(result, patternDataResults.GetIndexAt(col, row), neighbours);
                }
            }
        }
    }
}


