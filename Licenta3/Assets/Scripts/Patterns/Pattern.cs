using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;


namespace WaveFunctionCollapse
{//Pattern class
    public class Pattern
    {
        private int index;//index-ul numeric unic al acestui pattern
        private int[][] grid;//conţine valorile (indici) în forma sub‐grilei N×N
        public string HashIndex { get; set; } //HashIndex = identificator unic al pattern-ului, calculat o singură dată.
                                              //ţine şirul hex (de tip string) rezultat din aplicarea unui algoritm MD5 pe valorile întregi ale pattern-ului (sub-grila N×N)
        public int Index => index; //expune _index în mod readonly


        //Metode:
        public Pattern(int[][] grid, string hashCode, int index)
        {
            this.grid = grid;
            HashIndex = hashCode;
            this.index = index;
        }

        public void SetGridValue(int x, int y, int value)
        {
            grid[y][x] = value;
        }

        public int GetGridValue(int x, int y)
        {
            return grid[y][x];
        }

        public bool CheckValueAtPosition(int x, int y, int value)
        {
            return value.Equals(GetGridValue(x, y));
        }

        internal bool ComparePatternToAnotherPattern(Direction dir, Pattern pattern)
        {
            int[][] myGrid = GetGridValuesInDirection(dir);
            int[][] otherGrid = pattern.GetGridValuesInDirection(dir.GetOppositeDirectionTo());

            for (int row = 0; row < myGrid.Length; row++)
            {
                for (int col = 0; col < myGrid[0].Length; col++)
                {
                    if (myGrid[row][col] != otherGrid[row][col])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private int[][] GetGridValuesInDirection(Direction dir)
        {
            int[][] gridPartToCompare;
            switch (dir)
            {
                case Direction.Up:
                    gridPartToCompare = MyCollectionExtension.CreateJaggedArray<int[][]>(grid.Length - 1, grid.Length);
                    CreatePartOfGrid(0, grid.Length, 1, grid.Length, gridPartToCompare);
                    break;

                case Direction.Down:
                    gridPartToCompare = MyCollectionExtension.CreateJaggedArray<int[][]>(grid.Length - 1, grid.Length);
                    CreatePartOfGrid(0, grid.Length, 0, grid.Length - 1, gridPartToCompare);
                    break;

                case Direction.Left:
                    gridPartToCompare = MyCollectionExtension.CreateJaggedArray<int[][]>(grid.Length, grid.Length - 1);
                    CreatePartOfGrid(0, grid.Length - 1, 0, grid.Length, gridPartToCompare);
                    break;

                case Direction.Right:
                    gridPartToCompare = MyCollectionExtension.CreateJaggedArray<int[][]>(grid.Length, grid.Length - 1);
                    CreatePartOfGrid(1, grid.Length, 0, grid.Length, gridPartToCompare);
                    break;

                default:
                    return grid;
            }

            return gridPartToCompare;
        }

        private void CreatePartOfGrid(int xmin, int xmax, int ymin, int ymax, int[][] gridPartToCompare)
        {
            List<int> tempList = new List<int>();

            //Pas 1: Creăm o listă în care punem “laolaltă” toate valorile din sub-dreptunghiul _grid[ymin..ymax-1][xmin..xmax-1].
            for (int row = ymin; row < ymax; row++)
            {
                for (int col = xmin; col < xmax; col++)
                {
                    tempList.Add(grid[row][col]);
                }
            }
            // Acum tempList conține H * W elemente,
            // unde H = ymax - ymin, și W = xmax - xmin, 
            // în ordinea (y0,x0), (y0,x1), … (y0,xW-1), (y1,x0), … etc.

            //Pas 2: 
            for (int i = 0; i < tempList.Count; i++)
            {
                int x = i % gridPartToCompare.Length;
                int y = i / gridPartToCompare.Length;
                gridPartToCompare[x][y] = tempList[i];
            }
            // Luăm lista “aplatizată” și o scriem în target, care e un array pătrat [H][W] (în cazul pattern-urilor, H == W == patternSize-1 sau patternSize).
            // Folosim div și mod ca să transformăm indexul liniar iîn coordonate 2D (tx, ty).
        }

    }
}

