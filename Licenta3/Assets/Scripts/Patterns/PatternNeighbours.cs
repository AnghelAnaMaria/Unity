using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public class PatternNeighbours
    {//Clasa PatternNeighbours este folosită pentru a păstra și gestiona, pentru fiecare pattern (sub-grilă N×N), lista de pattern-uri care îi pot fi alăturate pe fiecare dintre cele patru direcții.
        public Dictionary<Direction, HashSet<int>> directionPatternNeighbourDictionary = new Dictionary<Direction, HashSet<int>>();//Pentru fiecare direcție, setul de indici de pattern-uri care pot sta acolo

        //Metode:
        public void AddPatternToDictionary(Direction dir, int patternIndex)//Adaugă un singur neighbour (patternIndex) pentru o direcție anume
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
        internal HashSet<int> GetNeighboursInDirection(Direction dir)
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

