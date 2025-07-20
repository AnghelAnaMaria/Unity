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
{
    public class PatternNeighbours
    {//Clasa PatternNeighbours este folosită pentru a păstra și gestiona, pentru fiecare pattern (sub-grilă N×N), lista de pattern-uri care îi pot fi alăturate pe fiecare dintre cele patru direcții.
        public Dictionary<Dir, HashSet<int>> directionPatternNeighbourDictionary = new Dictionary<Dir, HashSet<int>>();//Pentru fiecare direcție, setul de indici de pattern-uri care pot sta acolo

        //Metode:
        public void AddPatternToDictionary(Dir dir, int patternIndex)//Adaugă un singur neighbour (patternIndex) pentru o direcție anume
        {
            if (directionPatternNeighbourDictionary.ContainsKey(dir))
            {
                directionPatternNeighbourDictionary[dir].Add(patternIndex);
            }
            else
            {
                directionPatternNeighbourDictionary.Add(dir, new HashSet<int> { patternIndex });
            }
        }

        // Întoarce setul de neighbours pentru direcția dată (sau un set gol dacă n-avem niciunul)
        internal HashSet<int> GetNeighboursInDirection(Dir dir)
        {
            if (directionPatternNeighbourDictionary.ContainsKey(dir))
            {
                return directionPatternNeighbourDictionary[dir];
            }
            return new HashSet<int>();
        }

        // Fuzionează toate intrările dintr-un alt PatternNeighbours în acesta
        public void AddNeighbour(PatternNeighbours neighbours)
        {
            foreach (var item in neighbours.directionPatternNeighbourDictionary)//pentru fiecare item= (direcție, setDeIndexuri) din obiectul PatternNeighbours dat parametru
            {
                if (!this.directionPatternNeighbourDictionary.ContainsKey(item.Key))
                {
                    this.directionPatternNeighbourDictionary.Add(item.Key, new HashSet<int>());
                }

                this.directionPatternNeighbourDictionary[item.Key].UnionWith(item.Value);//punem in obj. this.PatternNeighbours vecinii celor 2 tiles
            }
        }

    }
}

