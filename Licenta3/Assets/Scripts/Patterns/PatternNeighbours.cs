using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveFunctionCollapse
{
    public class PatternNeighbours
    {
        // Pentru fiecare direcție, setul de indici de pattern-uri care pot sta acolo
        public Dictionary<Direction, HashSet<int>> directionPatternNeighbourDictionary = new Dictionary<Direction, HashSet<int>>();

        // Adaugă un singur neighbour (patternIndex) pentru o direcție anume
        public void AddPatternToDictionary(Direction dir, int patternIndex)
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
            foreach (var item in neighbours.directionPatternNeighbourDictionary)
            {
                // dacă n-avem încă intrare pentru această direcție, o creăm
                if (!directionPatternNeighbourDictionary.ContainsKey(item.Key))
                {
                    directionPatternNeighbourDictionary.Add(item.Key, new HashSet<int>());
                }
                // apoi unim (adăugăm) toate index‐urile din setul venit ca parametru
                directionPatternNeighbourDictionary[item.Key].UnionWith(item.Value);
            }
        }

    }
}

