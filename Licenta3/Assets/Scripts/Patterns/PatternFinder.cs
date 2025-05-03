using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Helpers;



namespace WaveFunctionCollapse
{//clasa responsabilă de scanarea grilei de indici (produsă de ValuesManager) si de extragerea pattern-urile unice, de indexarea pattern-urilor, de calcularea frecvenţele lor şi, apoi, de generarea regulilor de vecinătate.
    public class PatternFinder
    {
        internal static PatternDataResults GetPatternDataFromGrid<T>(ValuesManager<T> valueManager, int patternSize, bool equalWeights)//internal este un modificator de acces care înseamnă „vizibil doar în acest assembly”. 
        { //                                                                      ||                      ||
          //                                                              gridul de int[][]                N
            Dictionary<string, PatternData> patternHashcodeDictionary = new Dictionary<string, PatternData>();
            Dictionary<int, PatternData> patternIndexDictionary = new Dictionary<int, PatternData>();
            Vector2 sizeOfGrid = valueManager.GetGridSize();
            int patternGridSizeX = 0, patternGridSizeY = 0;
            int rowMin = -1, colMin = -1, rowMax = -1, colMax = -1;

            if (patternSize < 3)//fereastra mai mica de 3x3, gasim pattern cu wrap
            {
                patternGridSizeX = (int)sizeOfGrid.x + 3 - patternSize;//patternGridSizeX/Y = numărul total de poziţii în care fereastra N×N „pluteşte” peste grilă (inclusiv peste margini, pentru toroidal wrap-around).
                patternGridSizeY = (int)sizeOfGrid.y + 3 - patternSize;
                rowMax = patternGridSizeY - 1;
                colMax = patternGridSizeX - 1;
            }
            else
            {//gasim pattern fara wrap
                patternGridSizeX = (int)sizeOfGrid.x + patternSize - 1;
                patternGridSizeY = (int)sizeOfGrid.y + patternSize - 1;
                rowMin = 1 - patternSize;
                colMin = 1 - patternSize;
                rowMax = (int)sizeOfGrid.y;
                colMax = (int)sizeOfGrid.x;
            }

            int[][] patternIndicesGrid = MyCollectionExtension.CreateJaggedArray<int[][]>(patternGridSizeY, patternGridSizeX);
            int totalFrequency = 0, patternIndex = 0;

            for (int row = rowMin; row < rowMax; row++)
            {
                for (int col = colMin; col < colMax; col++)
                {
                    // 1) extrag sub-grila N×N
                    int[][] gridValues = valueManager.GetPatternValuesFromGridAt(col, row, patternSize);
                    // 2) calculez hash-ul pattern-ului
                    string hashValue = HashCodeCalculator.CalculateHashCode(gridValues);

                    if (!patternHashcodeDictionary.ContainsKey(hashValue))
                    {
                        // 3a) dacă e un pattern nou, îl înregistrez și îi dau un index nou
                        Pattern pattern = new Pattern(gridValues, hashValue, patternIndex);
                        patternIndex++;
                        AddNewPattern(patternHashcodeDictionary, patternIndexDictionary, hashValue, pattern);
                    }
                    else
                    {
                        // 3b) dacă pattern-ul exista deja și folosesc greutăți variabile,
                        //     îi cresc frecvența
                        if (!equalWeights)
                        {
                            var existing = patternHashcodeDictionary[hashValue];
                            patternIndexDictionary[existing.Pattern.Index].AddToFrequency();
                        }
                    }
                    totalFrequency++;
                    if (patternSize < 3)
                    {
                        patternIndicesGrid[row + 1][col + 1] =
                            patternHashcodeDictionary[hashValue].Pattern.Index;
                    }
                    else
                    {
                        patternIndicesGrid[row + patternSize - 1]
                                          [col + patternSize - 1] =
                            patternHashcodeDictionary[hashValue].Pattern.Index;
                    }
                }
            }
            CalculateRelativeFrequency(patternIndexDictionary, totalFrequency);
            return new PatternDataResults(patternIndicesGrid, patternIndexDictionary);

        }

        private static void CalculateRelativeFrequency(Dictionary<int, PatternData> patternIndexDictionary, int totalFrequency)
        {
            foreach (var item in patternIndexDictionary.Values)
            {
                item.CalculateRelativeFrequency(totalFrequency);
            }
        }


        private static void AddNewPattern(Dictionary<string, PatternData> patternHashcodeDictionary, Dictionary<int, PatternData> patternIndexDictionary, string hashValue, Pattern pattern)
        {
            // Creăm un PatternData nou pe baza obiectului Pattern
            PatternData data = new PatternData(pattern);

            // Mapăm hash-ul pattern-ului către acest PatternData
            patternHashcodeDictionary.Add(hashValue, data);

            // Mapăm şi indexul numeric al pattern-ului către acelaşi PatternData
            patternIndexDictionary.Add(pattern.Index, data);
        }

        internal static Dictionary<int, PatternNeighbours> FindPossibleNeighboursForAllPatterns(IFindNeighbourStrategy strategy, PatternDataResults patternFinderResult)
        {
            return strategy.FindNeighbours(patternFinderResult);
        }

        public static PatternNeighbours CheckNeighboursInEachDirection(int x, int y, PatternDataResults patternDataResults)
        {
            PatternNeighbours patternNeighbours = new PatternNeighbours();

            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                int possiblePatternIndex = patternDataResults.GetNeighbourInDirection(x, y, dir);
                if (possiblePatternIndex >= 0)
                {
                    patternNeighbours.AddPatternToDictionary(dir, possiblePatternIndex);
                }
            }

            return patternNeighbours;
        }

        public static void AddNeighboursToDictionary(Dictionary<int, PatternNeighbours> dictionary, int patternIndex, PatternNeighbours neighbours)
        {
            if (!dictionary.ContainsKey(patternIndex))
            {
                dictionary.Add(patternIndex, neighbours);
            }
            dictionary[patternIndex].AddNeighbour(neighbours);
        }

    }
}

