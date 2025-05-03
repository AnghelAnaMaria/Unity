using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class NeighboursStrategySize2OrMore : IFindNeighbourStrategy
    {
        public Dictionary<int, PatternNeighbours> FindNeighbours(PatternDataResults patternFinderResult)
        {
            //Dicționarul final: pentru fiecare pattern‐index, setul posibil de vecini
            var result = new Dictionary<int, PatternNeighbours>();
            //Parcurgem fiecare pattern sursă
            foreach (var patternDataToCheck in patternFinderResult.PatternIndexDictionary)
            {
                foreach (var possibleNeighbourForPattern in patternFinderResult.PatternIndexDictionary)
                {
                    FindNeighboursInAllDirections(result, patternDataToCheck, possibleNeighbourForPattern);
                }
            }
            return result;

        }

        private void FindNeighboursInAllDirections(Dictionary<int, PatternNeighbours> result, KeyValuePair<int, PatternData> patternDataToCheck, KeyValuePair<int, PatternData> possibleNeighbourForPattern)
        {
            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                // Dacă cele două pattern‐uri se potrivesc pe direcția curentă
                if (patternDataToCheck.Value.CompareGrid(dir, possibleNeighbourForPattern.Value))
                {
                    // Asigură existența unui entry pentru sursă
                    if (!result.ContainsKey(patternDataToCheck.Key))
                    {
                        result.Add(patternDataToCheck.Key, new PatternNeighbours());
                    }
                    // Adaugă indexul vecinului valid în dicționarul PatternNeighbours
                    result[patternDataToCheck.Key].AddPatternToDictionary(dir, possibleNeighbourForPattern.Key);
                }
            }
        }

    }
}

