using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Helpers;
using UnityEngine;

namespace WaveFunctionCollapse
{//clasa care leagă toate piesele la un loc după ce am extras mai întâi pattern-urile şi datele despre ele
    public class PatternManager
    {
        Dictionary<int, PatternData> patternDataIndexDictionary = new Dictionary<int, PatternData>();

        Dictionary<int, PatternNeighbours> patternPossibleNeighboursDictionary = new Dictionary<int, PatternNeighbours>();

        int patternSize = -1;//dimensiunea N a sub-grilelor (pattern-urilor) N×N pe care le extragi din grid.

        IFindNeighbourStrategy strategy;


        //Metode:
        public PatternManager(int patternSize)//avem N pt pattern
        {
            this.patternSize = patternSize;
        }

        public void ProcessGrid<T>(ValuesManager<T> valueManager, bool equalWeights, string strategyName = null)//avem strategie
        {
            NeighbourStrategyFactory strategyFactory = new NeighbourStrategyFactory();
            strategy = strategyFactory.CreateInstance(strategyName == null ? patternSize + "" : strategyName);
            CreatePatterns(valueManager, strategy, equalWeights);
        }

        private void CreatePatterns<T>(ValuesManager<T> valueManager, IFindNeighbourStrategy strategy, bool equalWeights)
        {
            PatternDataResults patternDataResults = PatternFinder.GetPatternDataFromGrid(valueManager, patternSize, equalWeights);//avem matricea de patterns
            patternDataIndexDictionary.Clear();
            foreach (var kv in patternDataResults.patternIndexDictionary)
                patternDataIndexDictionary.Add(kv.Key, kv.Value);
            Debug.Log($"[PatternManager] Extracted {patternDataIndexDictionary.Count} patterns: " + string.Join(",", patternDataIndexDictionary.Keys));

            GetPatternNeighbours(patternDataResults, strategy);
        }

        private void GetPatternNeighbours(PatternDataResults patternDataResults, IFindNeighbourStrategy strategy)
        {
            patternPossibleNeighboursDictionary = PatternFinder.FindPossibleNeighboursForAllPatterns(strategy, patternDataResults);//avem vecinii pt fiecare pattern
            Debug.Log($"[PatternManager] Found neighbour‐lists for {patternPossibleNeighboursDictionary.Count} patterns: " + string.Join(",", patternPossibleNeighboursDictionary.Keys));
        }

        public IEnumerable<int> GetAllPatternIndices()
        {
            return patternDataIndexDictionary.Keys;
        }

        internal int[][] ConvertPatternsToValues<T>(int[][] outputValues)//reconstruieste grila int[][] (ce reprezinta Tiles) pornind de la grila de patterns
        {
            int patternOutputHeight = outputValues.Length;//cate pattern-uri pe verticala
            int patternOutputWidth = outputValues[0].Length;//cate pattern-uri pe orizontala
            int valueGridHeight = patternOutputHeight + (patternSize - 1);//cate randuri are grila int[][], cu toate suprapunerile de patterns (patterns se suprapun intre ele cu N-1 celule, iar ultimul pattern se suprapune numai cu 1 celula)
            int valueGridWidth = patternOutputWidth + (patternSize - 1);//câte coloane are grila int[][]
            int[][] valueGrid = MyCollectionExtension.CreateJaggedArray<int[][]>(valueGridHeight, valueGridWidth);//aici salvam grila cu toate valorile int ce reprezinta Tiles

            for (int row = 0; row < patternOutputHeight; row++)
            {
                for (int col = 0; col < patternOutputWidth; col++)//pt fiecare pattern
                {
                    Pattern pattern = GetPatternDataFromIndex(outputValues[row][col]).Pattern;
                    GetPatternValues(patternOutputWidth, patternOutputHeight, valueGrid, row, col, pattern);//reconstruieste grila int[][] (ce reprezinta Tiles)
                }
            }

            return valueGrid;
        }

        private void GetPatternValues(int patternOutputWidth, int patternOutputHeight, int[][] valueGrid, int row, int col, Pattern pattern)// (Depinde care este pozitia pattern-ului in matricea de patterns -> 4 cazuri)
        {
            if (row == patternOutputHeight - 1 && col == patternOutputWidth - 1)//cel mai jos-dreapta pattern
            {
                for (int row_1 = 0; row_1 < patternSize; row_1++)
                {
                    for (int col_1 = 0; col_1 < patternSize; col_1++)
                    {
                        valueGrid[row + row_1][col + col_1] = pattern.GetGridValue(col_1, row_1);
                    }
                }
            }
            else if (row == patternOutputHeight - 1)//patterns de jos (nu parcurgem si pe coloane pt ca oricum se suprapun)
            {
                for (int row_1 = 0; row_1 < patternSize; row_1++)
                {
                    valueGrid[row + row_1][col] = pattern.GetGridValue(0, row_1);
                }
            }
            else if (col == patternOutputWidth - 1)//patterns de la dreapta (nu parcurgem si pe randuri pt ca oricum se suprapun)
            {
                for (int col_1 = 0; col_1 < patternSize; col_1++)
                {
                    valueGrid[row][col + col_1] = pattern.GetGridValue(col_1, 0);
                }
            }
            else//restul de patterns (care nu sunt la periferie la dreapta sau jos) participa cu o valoare int
            {
                valueGrid[row][col] = pattern.GetGridValue(0, 0);
            }
        }

        //Get methods:
        public PatternData GetPatternDataFromIndex(int index)
        {
            return patternDataIndexDictionary[index];
        }

        public HashSet<int> GetPossibleNeighboursForPatternInDirection(int patternIndex, Direction dir)
        {
            return patternPossibleNeighboursDictionary[patternIndex].GetNeighboursInDirection(dir);
        }

        public float GetPatternFrequency(int index)
        {
            return GetPatternDataFromIndex(index).FrequencyRelative;
        }

        public float GetPatternFrequencyLog2(int index)
        {
            return GetPatternDataFromIndex(index).FrequencyRelativeLog2;
        }

        public int GetNumberOfPatterns()
        {
            return patternDataIndexDictionary.Count;
        }


    }
}