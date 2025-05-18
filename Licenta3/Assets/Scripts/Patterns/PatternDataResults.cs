using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Helpers;


namespace WaveFunctionCollapse
{//Tilemap de indicii pattern-urilor
    public class PatternDataResults
    {
        private int[][] patternIndicesGrid;//matricea de patterns din input (input-ul tradus in matrice de patterns)
        public Dictionary<int, PatternData> patternIndexDictionary { get; private set; }//dicționarul (index, PatternData), pentru a afla detaliile (frevența, conținutul)
        //Pentru fiecare index de pattern (cheia din dicționar) am asociat un obiect PatternData, care stochează: instanța Pattern (valorile exacte ale sub-grilei), frecvența absolută și relativă, log-aritmul frecvenței (folosit pentru calculul entropiei în WFC).

        //Metode:
        public PatternDataResults(int[][] patternIndicesGrid, Dictionary<int, PatternData> patternIndexDictionary)
        {
            this.patternIndicesGrid = patternIndicesGrid;
            this.patternIndexDictionary = patternIndexDictionary;
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
            if (patternIndicesGrid.CheckJaggedArray2dIndexIsValid(x, y) == false)//CheckJaggedArray2dIndexIsValid din Helpers
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
