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


