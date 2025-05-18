using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;//pt StringBuilder
using Helpers;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace WaveFunctionCollapse
{
    public class OutputGrid
    {
        private Dictionary<int, HashSet<int>> indexPossiblePatternDictionary;//(index liniar al unei celule din grid, indexi ale patterns ce pot sta in celula);           index liniar = x + width * y
        public int width { get; }//dimensiune grid final(output)
        public int height { get; }//dimensiune grid final(output)
        private int maxNumberOfPatterns = 0;//nr de pattern-uri distincte din gridul final

        //Metode:
        public OutputGrid(int width, int height, int numberOfPatterns)
        {
            this.width = width;
            this.height = height;
            this.maxNumberOfPatterns = numberOfPatterns;
            ResetAllPossibilities();
        }

        public void ResetAllPossibilities()
        {
            HashSet<int> allPossiblePatternList = new HashSet<int>(Enumerable.Range(0, maxNumberOfPatterns));//genereaza lista de indexi 0,1,2..,maxNumberOfPatterns-1
            indexPossiblePatternDictionary = new Dictionary<int, HashSet<int>>();

            for (int i = 0; i < height * width; i++)//pt fiecare celula din grid incepem cu toate pattern-urile permise
            {
                indexPossiblePatternDictionary[i] = new HashSet<int>(allPossiblePatternList);
            }
        }

        public void RestrictPossibleValuesAt(int x, int y, IEnumerable<int> allowedPatternIndices)
        {
            int index = x + width * y;
            if (indexPossiblePatternDictionary.TryGetValue(index, out var set))//patterns care pot sta pe index
            {
                set.IntersectWith(allowedPatternIndices);//restrictii de patterns puse de noi
            }

        }

        public bool CheckCellExists(Vector2Int position)
        {
            int index = GetIndexFromCoordinates(position);
            return indexPossiblePatternDictionary.ContainsKey(index);
        }

        private int GetIndexFromCoordinates(Vector2Int position)
        {
            return position.x + width * position.y;
        }

        public bool CheckIfCellIsCollapsed(Vector2Int position)
        {
            return GetPossibleValuesForPosition(position).Count <= 1;
        }

        public HashSet<int> GetPossibleValuesForPosition(Vector2Int position)
        {
            int index = GetIndexFromCoordinates(position);
            if (indexPossiblePatternDictionary.ContainsKey(index))
            {
                return indexPossiblePatternDictionary[index];
            }
            return new HashSet<int>();
        }

        internal void PrintToConsole()//afisam care sunt posibilitatile de patterns pt fiecare celula (afisam ca o matrice)
        {
            StringBuilder builder = null;//StringBuilder= obiect optimizat pentru construcţia de string-uri prin apeluri repetitive de Append, fără să aloce un string nou la fiecare concatenare.
            List<string> list = new List<string>();

            for (int row = 0; row < this.height; row++)//pt fiecare rand din grid facem un StringBuilder si il adaugam in lista
            {
                builder = new StringBuilder();
                for (int col = 0; col < this.width; col++)
                {
                    var results = GetPossibleValuesForPosition(new Vector2Int(col, row));
                    string cellText;
                    if (results.Count == 1)
                    {
                        // un singur element: {X}
                        cellText = "{" + results.First() + "}";
                    }
                    else
                    {
                        // mai multe elemente: {a,b,c}
                        cellText = "{" + string.Join(",", results) + "}";
                    }
                    builder.Append(cellText).Append("  ");
                }
                list.Add(builder.ToString());//adaugam StringBuilder in lista
            }

            list.Reverse();//pt că în coordinate matriceale row = 0 înseamnă jos, dar în consola Unity vrem să citim de sus în jos, pur și simplu inversăm lista

            foreach (var item in list)
            {
                Debug.Log(item);
            }
            Debug.Log("---");
        }

        public bool CheckIfGridIsSolved()
        {
            return !indexPossiblePatternDictionary.Any(x => x.Value.Count > 1);
        }

        internal bool CheckIfValidCoords(Vector2Int position)
        {
            return MyCollectionExtension.ValidateCoordinates(position.x, position.y, width, height);
        }

        public Vector2Int GetRandomCellCoords()
        {
            int randIndex = UnityEngine.Random.Range(0, indexPossiblePatternDictionary.Count);
            return GetCoordsFromIndex(randIndex);
        }

        private Vector2Int GetCoordsFromIndex(int randIndex)
        {
            Vector2Int coords = Vector2Int.zero;
            coords.x = randIndex / this.width;
            coords.y = randIndex % this.height;
            return coords;
        }

        public void SetPatternOnPosition(int x, int y, int patternIndex)
        {
            int index = GetIndexFromCoordinates(new Vector2Int(x, y));
            indexPossiblePatternDictionary[index] = new HashSet<int> { patternIndex };
        }

        public int[][] GetSolvedOutputGrid()//returneaza matricea finala de indici de patterns (toate celulele sunt colapsate)
        {
            int[][] returnGrid = MyCollectionExtension.CreateJaggedArray<int[][]>(height, width);

            if (!CheckIfGridIsSolved())
                return MyCollectionExtension.CreateJaggedArray<int[][]>(0, 0);

            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int index = GetIndexFromCoordinates(new Vector2Int(col, row));
                    returnGrid[row][col] = indexPossiblePatternDictionary[index].First();
                }
            }

            return returnGrid;
        }
    }
}