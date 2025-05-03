using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;


namespace WaveFunctionCollapse
{//Tilemap de indicii pattern-urilor
    public class PatternDataResults
    {
        private int[][] patternIndicesGrid;//grila cu indicii unici ai pattern-urilor (adica matricea de patern-uri)
        public Dictionary<int, PatternData> PatternIndexDictionary { get; private set; }//dicționarul index → PatternData, pentru a afla detaliile (frevența, conținutul)


        //Metode:
        public PatternDataResults(int[][] patternIndicesGrid, Dictionary<int, PatternData> patternIndexDictionary)
        {
            this.patternIndicesGrid = patternIndicesGrid;
            PatternIndexDictionary = patternIndexDictionary;
        }

        public int GetGridLengthX()
        {
            return patternIndicesGrid[0].Length;
        }

        public int GetGridLengthY()
        {
            return patternIndicesGrid.Length;
        }

        public int GetIndexAt(int x, int y)
        {
            return patternIndicesGrid[y][x];
        }

        public int GetNeighbourInDirection(int x, int y, Direction dir)
        {
            // dacă poziția (x,y) însăși nu e validă return -1
            if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x, y) == false)
                return -1;

            switch (dir)
            {
                case Direction.Up:
                    // verificăm mai întâi că (x, y+1) e în interior
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x, y + 1))
                        return GetIndexAt(x, y + 1);
                    return -1;

                case Direction.Down:
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x, y - 1))
                        return GetIndexAt(x, y - 1);
                    return -1;

                case Direction.Left:
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x - 1, y))
                        return GetIndexAt(x - 1, y);
                    return -1;

                case Direction.Right:
                    if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x + 1, y))
                        return GetIndexAt(x + 1, y);
                    return -1;

                default:
                    return -1;
            }
        }
    }

}
