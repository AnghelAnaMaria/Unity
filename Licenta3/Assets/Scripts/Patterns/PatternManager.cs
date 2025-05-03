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
        Dictionary<int, PatternData> patternDataIndexDictionary;//un dicţionar care asociază fiecărui index (un int unic) obiectul PatternData conţinând grila pattern-ului în sine, frecvenţa relativă, hash-ul ş.a.

        Dictionary<int, PatternNeighbours> patternPossibleNeighboursDictionary;//pentru fiecare pattern retinem setul de indexuri ale pattern-urilor care pot sta în fiecare direcţie (sus, jos, stânga, dreapta).

        int patternSize = -1;//dimensiunea N a sub-grilelor (pattern-urilor) N×N pe care le extragi din grid.

        IFindNeighbourStrategy strategy;//o instanţă de IFindNeighbourStrategy care ştie cum să determine, pentru două pattern-uri, dacă ele se potrivesc unul lângă altul în anumite direcţii (ex.: potrivirea marginilor, culorile, formele etc.).


        //Metode:
        public PatternManager(int patternSize)
        {
            this.patternSize = patternSize;
        }

        public void ProcessGrid<T>(ValuesManager<T> valueManager, bool equalWeights, string strategyName = null)
        {
            NeighbourStrategyFactory strategyFactory = new NeighbourStrategyFactory();
            strategy = strategyFactory.CreateInstance(strategyName == null ? patternSize + "" : strategyName);
            CreatePatterns(valueManager, strategy, equalWeights);
        }

        internal int[][] ConvertPatternToValues<T>(int[][] outputValues)
        {
            int patternOutputWidth = outputValues[0].Length;
            int patternOutputHeight = outputValues.Length;
            int valueGridWidth = patternOutputWidth + (patternSize - 1);
            int valueGridHeight = patternOutputHeight + (patternSize - 1);
            int[][] valueGrid = MyCollectionExtension.CreateJaggedArray<int[][]>(valueGridHeight, valueGridWidth);

            for (int row = 0; row < patternOutputHeight; row++)
            {
                for (int col = 0; col < patternOutputWidth; col++)
                {
                    Pattern pattern = GetPatternDataFromIndex(outputValues[row][col]).Pattern;
                    GetPatternValues(patternOutputWidth, patternOutputHeight, valueGrid, row, col, pattern);
                }
            }

            return valueGrid;
        }

        private void GetPatternValues(int patternOutputWidth, int patternOutputHeight, int[][] valueGrid, int row, int col, Pattern pattern)
        {
            if (row == patternOutputHeight - 1 && col == patternOutputWidth - 1)
            {
                for (int row_1 = 0; row_1 < patternSize; row_1++)
                {
                    for (int col_1 = 0; col_1 < patternSize; col_1++)
                    {
                        valueGrid[row + row_1][col + col_1] = pattern.GetGridValue(col_1, row_1);
                    }
                }
            }
            else if (row == patternOutputHeight - 1)
            {
                for (int row_1 = 0; row_1 < patternSize; row_1++)
                {
                    valueGrid[row + row_1][col] = pattern.GetGridValue(0, row_1);
                }
            }
            else if (col == patternOutputWidth - 1)
            {
                for (int col_1 = 0; col_1 < patternSize; col_1++)
                {
                    valueGrid[row][col + col_1] = pattern.GetGridValue(col_1, 0);
                }
            }
            else
            {
                valueGrid[row][col] = pattern.GetGridValue(0, 0);
            }
        }


        private void CreatePatterns<T>(ValuesManager<T> valueManager, IFindNeighbourStrategy strategy, bool equalWeights)
        {
            var patternFinderResult = PatternFinder.GetPatternDataFromGrid(valueManager, patternSize, equalWeights);//fct care  extrage toate sub-grilele N×N, calculează hash-uri, dedup, numără frecvenţe
            //For test:
            // StringBuilder builder = null;
            // List<string> list = new List<string>();

            // for (int row = 0; row < patternFinderResult.GetGridLengthY(); row++)
            // {
            //     builder = new StringBuilder();
            //     for (int col = 0; col < patternFinderResult.GetGridLengthX(); col++)
            //     {
            //         builder.Append(patternFinderResult.GetIndexAt(col, row) + " ");
            //     }
            //     list.Add(builder.ToString());
            // }

            // list.Reverse();

            // foreach (var item in list)
            // {
            //     Debug.Log(item);
            // }
            patternDataIndexDictionary = patternFinderResult.PatternIndexDictionary;
            GetPatternNeighbours(patternFinderResult, strategy);
        }

        private void GetPatternNeighbours(PatternDataResults patternFinderResult, IFindNeighbourStrategy strategy)
        {
            patternPossibleNeighboursDictionary =
                PatternFinder.FindPossibleNeighboursForAllPatterns(strategy, patternFinderResult);
        }

        public PatternData GetPatternDataFromIndex(int index)
        {
            return patternDataIndexDictionary[index];
        }

        public HashSet<int> GetPossibleNeighboursForPatternInDirection(int patternIndex, Direction dir)
        {
            return patternPossibleNeighboursDictionary[patternIndex]
                .GetNeighboursInDirection(dir);
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
