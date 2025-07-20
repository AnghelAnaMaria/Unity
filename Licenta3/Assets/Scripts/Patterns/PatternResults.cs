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
using Helpers;


namespace WaveFunctionCollapse
{//Tilemap de indicii pattern-urilor
    public class PatternResults
    {
        private int[][] patternIndicesGrid;//matricea de patterns din input (input-ul tradus in matrice de patterns)
        public Dictionary<int, PatternData> patternIndexDictionary { get; private set; }//dicționarul (index, PatternData), pentru a afla detaliile (frevența, conținutul)
        //Pentru fiecare index de pattern (cheia din dicționar) am asociat un obiect PatternData, care stochează: instanța Pattern (valorile exacte ale sub-grilei), frecvența absolută și relativă, log-aritmul frecvenței (folosit pentru calculul entropiei în WFC).

        //Metode:
        public PatternResults(int[][] patternIndicesGrid, Dictionary<int, PatternData> patternIndexDictionary)
        {
            this.patternIndicesGrid = patternIndicesGrid;
            this.patternIndexDictionary = patternIndexDictionary;
        }

        public int GetGridLengthX()
        {
            return patternIndicesGrid[0].Length;
        }

        public int GetGridLengthY()
        {
            return patternIndicesGrid.Length;
        }

        public int GetIndexAt(int x, int y)
        {
            return patternIndicesGrid[y][x];
        }

        public int GetNeighbourInDirection(int x, int y, Dir dir)
        {
            // dacă poziția (x,y) însăși nu e validă return -1
            if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x, y) == false)//CheckJaggedArray2dIndexIsValid din Helpers
                return -1;

            switch (dir)
            {
                case Dir.Up:
                    // verificăm mai întâi că (x, y+1) e în interior
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x, y + 1))
                        return GetIndexAt(x, y + 1);
                    return -1;

                case Dir.Down:
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x, y - 1))
                        return GetIndexAt(x, y - 1);
                    return -1;

                case Dir.Left:
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x - 1, y))
                        return GetIndexAt(x - 1, y);
                    return -1;

                case Dir.Right:
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x + 1, y))
                        return GetIndexAt(x + 1, y);
                    return -1;

                default:
                    return -1;
            }
        }
    }

}
