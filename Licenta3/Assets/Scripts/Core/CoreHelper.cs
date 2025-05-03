using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class CoreHelper
    {
        float totalFrequency = 0;
        float totalFrequencyLog = 0;
        PatternManager patternManager;

        //Metode:
        public CoreHelper(PatternManager manager)
        {
            patternManager = manager;

            // for (int i = 0; i < patternManager.GetNumberOfPatterns(); i++)
            // {
            //     totalFrequency += patternManager.GetPatternFrequency(i);
            // }
            // totalFrequencyLog = Mathf.Log(totalFrequency, 2);
        }

        public int SelectSolutionPatternFromFrequency(List<int> possibleValues)
        {
            // Obținem lista de greutăți (frecvențe relative) pentru fiecare index
            List<float> valueFrequenciesFractions = GetListOfWeightsFromIndices(possibleValues);
            // Alegem un punct aleator în intervalul [0, suma tuturor greutăților)
            float randomValue = UnityEngine.Random.Range(0f, valueFrequenciesFractions.Sum());

            float sum = 0f;
            int index = 0;
            // Parcurgem greutățile și acumulăm până depășim randomValue
            foreach (var item in valueFrequenciesFractions)
            {
                sum += item;
                if (randomValue <= sum)
                {
                    // Returnăm indexul corespunzător
                    return index;
                }
                index++;
            }
            // În caz că nu am returnat în buclă, alegem ultimul index
            return index - 1;
        }

        private List<float> GetListOfWeightsFromIndices(List<int> possibleValues)
        {
            var valueFrequencies = possibleValues.Aggregate(new List<float>(), (acc, val) =>
            {
                acc.Add(patternManager.GetPatternFrequency(val));
                return acc;
            },
            acc => acc).ToList();
            return valueFrequencies;
        }

        public List<VectorPair> Create4DirectionNeighbours(Vector2Int cellCoordinates, Vector2Int previousCell)
        {
            List<VectorPair> list = new List<VectorPair>()
        {
            new VectorPair(cellCoordinates, cellCoordinates + new Vector2Int(1,  0), Direction.Right, previousCell),
            new VectorPair(cellCoordinates, cellCoordinates + new Vector2Int(-1, 0), Direction.Left,  previousCell),
            new VectorPair(cellCoordinates, cellCoordinates + new Vector2Int(0,  1), Direction.Up,    previousCell),
            new VectorPair(cellCoordinates, cellCoordinates + new Vector2Int(0, -1), Direction.Down,  previousCell),
        };
            return list;
        }

        public List<VectorPair> Create4DirectionNeighbours(Vector2Int cellCoordinate)
        {
            return Create4DirectionNeighbours(cellCoordinate, cellCoordinate);
        }

        public float CalculateEntropy(Vector2Int position, OutputGrid outputGrid)
        {
            float sum = 0;
            foreach (var possibleIndex in outputGrid.GetPossibleValuesForPosition(position))
            {
                totalFrequency += patternManager.GetPatternFrequency(possibleIndex);
                sum += patternManager.GetPatternFrequencyLog2(possibleIndex);
            }
            totalFrequencyLog = Mathf.Log(totalFrequency, 2);
            return totalFrequencyLog - (sum / totalFrequency);
        }

        public List<VectorPair> CheckIfNeighboursAreCollapsed(VectorPair pairToCheck, OutputGrid outputGrid)
        {
            return Create4DirectionNeighbours(pairToCheck.CellToPropagatePosition, pairToCheck.BaseCellPosition)
                .Where(x => outputGrid.CheckIfValidPosition(x.CellToPropagatePosition)
                         && outputGrid.CheckIfCellIsCollapsed(x.CellToPropagatePosition) == false)
                .ToList();
        }

        public bool CheckCellSolutionForCollision(Vector2Int cellCoordinates, OutputGrid outputGrid)
        {
            foreach (var neighbour in Create4DirectionNeighbours(cellCoordinates))
            {
                if (outputGrid.CheckIfValidPosition(neighbour.CellToPropagatePosition) == false)
                    continue;

                var possibleIndices = new HashSet<int>();
                foreach (var patternIndexAtNeighbour in outputGrid.GetPossibleValuesForPosition(neighbour.CellToPropagatePosition))
                {
                    var possibleNeighboursForBase = patternManager.GetPossibleNeighboursForPatternInDirection(patternIndexAtNeighbour, neighbour.DirectionFromBase.GetOppositeDirectionTo());
                    possibleIndices.UnionWith(possibleNeighboursForBase);
                }

                if (possibleIndices.Contains(outputGrid.GetPossibleValuesForPosition(cellCoordinates).First()) == false)
                    return true;
            }

            return false;
        }

    }

}
