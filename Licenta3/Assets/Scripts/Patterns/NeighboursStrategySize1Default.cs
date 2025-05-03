using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class NeighbourStrategySize1Default : IFindNeighbourStrategy
    {
        public Dictionary<int, PatternNeighbours> FindNeighbours(PatternDataResults patternFinderResult)
        {
            var result = new Dictionary<int, PatternNeighbours>();
            FindNeighboursForEachPattern(patternFinderResult, result);
            return result;
        }

        private void FindNeighboursForEachPattern(PatternDataResults patternFinderResult, Dictionary<int, PatternNeighbours> result)
        {
            // parcurgem fiecare poziție din gridul de pattern-uri
            for (int row = 0; row < patternFinderResult.GetGridLengthY(); row++)
            {
                for (int col = 0; col < patternFinderResult.GetGridLengthX(); col++)
                {
                    // aflăm vecinii la N=1 în toate direcțiile
                    PatternNeighbours neighbours =
                        PatternFinder.CheckNeighboursInEachDirection(col, row, patternFinderResult);

                    // adăugăm în dicționarul final (se acumulează seturile de vecini)
                    PatternFinder.AddNeighboursToDictionary(
                        result,
                        patternFinderResult.GetIndexAt(col, row),
                        neighbours
                    );
                }
            }
        }
    }
}


