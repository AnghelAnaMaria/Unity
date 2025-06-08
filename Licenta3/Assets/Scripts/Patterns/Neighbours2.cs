using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{//Strategia 2: consideram 2 patterns N*N ca fiind vecine daca au (N-1)*N tiles egale
    public class Neighbours2 : INeighbours
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

