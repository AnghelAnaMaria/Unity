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
using System.Linq;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class HelperManager
    {
        float totalFrequency = 0;
        float totalFrequencyLog = 0;
        PatternManager patternManager;//patterns
        Dictionary<Vector2Int, HashSet<int>> softBanned;

        //Metode:
        public HelperManager(PatternManager patternManager, Dictionary<Vector2Int, HashSet<int>> softBanned)
        {
            this.patternManager = patternManager;
            this.softBanned = softBanned;
        }

        public int SelectSolutionPatternFromFrequency(List<int> possibleValues, Vector2Int position, float epsilon = 0.01f)//possibleValues= lista de patterns posibile valide pt o celula din Tilemap
        {
            List<float> weights = GetListOfWeightsFromIndices(possibleValues);//lista de greutăți (frecvențe relative) pentru fiecare pattern

            //Aplic penalizarea epsilon pattern-urilor “soft banned”
            if (softBanned != null && softBanned.TryGetValue(position, out var banned))//position= pozitia pe care colapsam
            {
                for (int i = 0; i < possibleValues.Count; i++)//cautam in toate patterns
                {
                    if (banned.Contains(possibleValues[i]))//daca patterns nu vrem sa stea pe pozitia unde colapsam acum
                        weights[i] *= epsilon;//ii facem frecventa mai mica
                }
            }

            float randomValue = UnityEngine.Random.Range(0f, weights.Sum());//alegem un punct aleator în intervalul [0, suma tuturor greutăților) pt celula position

            float sum = 0f;
            //parcurgem greutățile și acumulăm până depășim randomValue
            for (int i = 0; i < weights.Count; i++)
            {
                sum += weights[i];
                if (randomValue <= sum)
                {
                    //returnăm pattern-ul din lista data
                    return possibleValues[i];
                }
            }
            //Fallback: ultimul pattern
            return possibleValues[possibleValues.Count - 1];

        }

        private List<float> GetListOfWeightsFromIndices(List<int> possibleValues)////possibleValues= lista de patterns
        {
            var valueFrequencies = possibleValues.Aggregate(new List<float>(), (acc, val) =>
            {
                acc.Add(patternManager.GetPatternFrequency(val));
                return acc;
            },
            acc => acc).ToList();
            return valueFrequencies;//returnam lista de frecvente (frecventele pt toate pattern-urile)
        }

        public List<CellPair> Create4DirectionNeighbours(Vector2Int cellCoordinates, Vector2Int previousCell)//retinem celula din Tilemap de unde pornim si vecinii celulei pt cele 4 directii
        {
            List<CellPair> list = new List<CellPair>(){
                new CellPair(cellCoordinates, cellCoordinates + new Vector2Int(1,  0), Dir.Right, previousCell),
                new CellPair(cellCoordinates, cellCoordinates + new Vector2Int(-1, 0), Dir.Left,  previousCell),
                new CellPair(cellCoordinates, cellCoordinates + new Vector2Int(0,  1), Dir.Up,    previousCell),
                new CellPair(cellCoordinates, cellCoordinates + new Vector2Int(0, -1), Dir.Down,  previousCell),
            };

            return list;
        }

        public List<CellPair> Create4DirectionNeighbours(Vector2Int cellCoordinate)//pentru apelul inițial (unde nu există celulă anterioară)
        {
            return Create4DirectionNeighbours(cellCoordinate, cellCoordinate);
        }

        public float CalculateEntropy(Vector2Int position, OutputGrid outputGrid)//Shannon entropy(functioneaza pt frecvente NORMALIZATE)
        {
            float sum = 0f;

            //Shannon entropy pt o celula:
            foreach (var possibleIndex in outputGrid.GetPossibleValuesForPosition(position))//pt fiecare pattern care poate sta in celula Vector2Int position
            {
                float frequency = patternManager.GetPatternFrequency(possibleIndex);// pᵢ =frecventa pt pattern =probabiliatea pt pattern; Stim ca aceste pᵢ sun normalizate
                if (frequency > 0f)//ca să evităm 0 * log2(0)
                {
                    sum -= frequency * patternManager.GetPatternFrequencyLog2(possibleIndex);// -∑pᵢ*log₂(pᵢ)
                }
            }

            return sum;
        }

        public List<CellPair> ReturnUncollapsedNeighbours(CellPair pairToCheck, OutputGrid outputGrid)//returnam vecinii necolapsati ai unei celule din grid (ai celulei tinta)
        {
            return Create4DirectionNeighbours(pairToCheck.CellToPropagatePosition, pairToCheck.BaseCellPosition)
                .Where(x => outputGrid.CheckIfValidCoords(x.CellToPropagatePosition) && outputGrid.CheckIfCellIsCollapsed(x.CellToPropagatePosition) == false)//x aici nu e coordonata X, ci un VectorPair, deci un (x,y)
                .ToList();
        }

        public bool CheckCellSolutionForCollision(Vector2Int cellCoordinates, OutputGrid outputGrid)//verifica daca o celula colapsata nu se potriveste cu vecinii (ca sa nu generam contradictii), adica daca avem collision
        {
            foreach (var neighbour in Create4DirectionNeighbours(cellCoordinates))//pt fiecare vecin VectorPair
            {
                if (outputGrid.CheckIfValidCoords(neighbour.CellToPropagatePosition) == false)//daca vecinul e in exteriorul grilei il sarim
                    continue;

                HashSet<int> possibleIndices = new HashSet<int>();
                foreach (int patternIndex in outputGrid.GetPossibleValuesForPosition(neighbour.CellToPropagatePosition))//pt fiecare pattern care poate sta pe pozitia vecinului
                {
                    HashSet<int> possibleNeighboursForBase = patternManager.GetPossibleNeighboursForPatternInDirection(patternIndex, neighbour.DirectionFromBase.GetOppositeDirectionTo());//de la vecin ne intoarcem la celula curenta si vedem ce patterns pot sta pe celula curenta (patterns care se potrivesc cu vecinul)
                    possibleIndices.UnionWith(possibleNeighboursForBase);
                }

                if (!possibleIndices.Contains(outputGrid.GetPossibleValuesForPosition(cellCoordinates).First()))//verificam daca printre patterns ale celulei curente se afla si cel colapsat -> daca da, nu avem contradictie
                    return true;
            }

            return false;
        }

    }

}