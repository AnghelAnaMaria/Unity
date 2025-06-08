using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Helpers;


namespace WaveFunctionCollapse
{///Clasa ValuesManager<T> are rolul de a „traduce” grila de valori generice (IValue<T>[][]) într-o reprezentare pe bază de indici 
///întregi și de a oferi utilitare pentru extragerea de „pattern-uri” (sub-matrici) cu wrap-around.
    public class InputManager<T>
    {
        int[][] grid;//grid[y][x] va conține, la final, indicele valorii aflate în poziția (x,y)
        Dictionary<int, IVal<T>> valueIndexDictionary = new Dictionary<int, IVal<T>>();//valueIndexDictionary mapează fiecare indice la obiectul IValue<T> original
        int index = 0;//index este contorul pe care îl incrementăm de fiecare dată când întâlnim o valoare nouă

        public InputManager(IVal<T>[][] gridOfValues)
        {
            CreateGridOfIndices(gridOfValues);
        }

        private void CreateGridOfIndices(IVal<T>[][] gridOfValues)
        {
            grid = JaggedArray.CreateJaggedArray<int[][]>(gridOfValues.Length, gridOfValues[0].Length);
            for (int row = 0; row < gridOfValues.Length; row++)
            {
                for (int col = 0; col < gridOfValues[0].Length; col++)
                {
                    SetIndexToGridPosition(gridOfValues, row, col);
                }
            }
        }

        //Mapare
        //– Dacă am văzut valoarea anterior, folosesc același indice (kv.Key).
        //– Altfel îi atribui index și apoi index++.
        private void SetIndexToGridPosition(IVal<T>[][] gridOfValues, int row, int col)
        {
            var value = gridOfValues[row][col];

            if (valueIndexDictionary.ContainsValue(value))// vrem ca aceleași valori (IValue<T>) să primească același număr. De aceea lucram cu ContainsValue + Equals.
            {
                var kv = valueIndexDictionary.FirstOrDefault(x => x.Value.Equals(value));//kv e de forma (cheie, valoare)
                grid[row][col] = kv.Key;//adaug in gridul int[][]
            }
            else
            {
                grid[row][col] = index;//adaug in gridul int[][]
                valueIndexDictionary.Add(index, value);
                index++;
            }
        }

        internal Vector2 GetGridSize()
        {
            if (grid == null)
            {
                return Vector2.zero;
            }
            return new Vector2(grid[0].Length, grid.Length);// Vector2(x,y) cu :x = numărul de coloane (lăţimea grilei)
                                                            //                  y = numărul de rânduri (înălţimea grilei)
        }

        public int GetGridValue(int x, int y)// conventie API
        {
            if (x >= grid[0].Length || y >= grid.Length || x < 0 || y < 0)
            {
                throw new System.IndexOutOfRangeException("Grid does not contain x: " + x + " y: " + y + " value");
            }
            return grid[y][x];
        }

        public IVal<T> GetValueFromIndex(int index)
        {
            if (valueIndexDictionary.ContainsKey(index))
            {
                return valueIndexDictionary[index];
            }
            throw new System.Exception("No index " + index + " in valueDictionary");
        }

        public int GetGridValuesIncludingOffset(int x, int y)
        {
            int yMax = grid.Length;// câte rânduri există → limita superioară pentru y
            int xMax = grid[0].Length;// câte coloane există → limita superioară pentru x

            if (x < 0 && y < 0)
            {
                return GetGridValue(xMax + x, yMax + y);
            }
            if (x < 0 && y >= yMax)
            {
                return GetGridValue(xMax + x, y - yMax);
            }
            if (x >= xMax && y < 0)
            {
                return GetGridValue(x - xMax, yMax + y);
            }
            if (x >= xMax && y >= yMax)
            {
                return GetGridValue(x - xMax, y - yMax);
            }
            if (x < 0)
            {
                return GetGridValue(xMax + x, y);
            }
            if (x >= xMax)
            {
                return GetGridValue(x - xMax, y);
            }
            if (y < 0)
            {
                return GetGridValue(x, yMax + y);
            }
            if (y >= yMax)
            {
                return GetGridValue(x, y - yMax);
            }
            return GetGridValue(x, y);
        }

        public int[][] GetPatternValuesFromGridAt(int x, int y, int patternSize)
        {
            int[][] arrayToReturn = JaggedArray.CreateJaggedArray<int[][]>(patternSize, patternSize);//jagged array patternSize × patternSize

            for (int row = 0; row < patternSize; row++)
            {
                for (int col = 0; col < patternSize; col++)
                {
                    // Extragem valoarea din grilă cu offset (cu wrap-around)
                    arrayToReturn[row][col] = GetGridValuesIncludingOffset(x + col, y + row);
                }
            }

            return arrayToReturn;//jagged array de int 
        }


    }
}
