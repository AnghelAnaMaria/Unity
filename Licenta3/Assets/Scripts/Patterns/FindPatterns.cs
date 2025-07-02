using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Helpers;



namespace WaveFunctionCollapse
{//Gasim patterns mergand cu o fereastra peste Tilemap-ul din scena (transformat in grid de int) si salvam aceste paterns intr-un obiect PatternDataResults.
 //clasa responsabilă de scanarea grilei de indici (produsă de ValuesManager) si de extragerea pattern-urile unice, de indexarea pattern-urilor, de calcularea frecvenţele lor şi, apoi, de generarea regulilor de vecinătate.
    public class FindPatterns
    {
        internal static PatternResults GetPatternDataFromGrid<T>(InputManager<T> inputManager, int patternSize, bool equalWeights)//internal este un modificator de acces care înseamnă „vizibil doar în acest assembly”. 
        { //                                                                      ||                      ||
          //                                                              gridul de int[][] din scena      N
            Dictionary<string, PatternData> patternHashcodeDictionary = new Dictionary<string, PatternData>();//aici salvam hash-uri pt. subgrilele de NxN de int din gridul din scena
            Dictionary<int, PatternData> patternIndexDictionary = new Dictionary<int, PatternData>();
            Vector2 sizeOfGrid = inputManager.GetGridSize();//dimensiunea gridului de int[][] din scena
            int patternGridSizeX = 0, patternGridSizeY = 0;//dimensiunea matricei de int(indexi de patterns)
            int rowMin = -1, colMin = -1, rowMax = -1, colMax = -1;

            //Gasim coordonatele peste care merge fereastra si dimensiunea matricei de int(indexi de patterns):
            if (patternSize < 3)//Cand N<3: wrap-around cand extind cu exact 1 celulă pe fiecare margine:
            {
                patternGridSizeX = (int)sizeOfGrid.x + 3 - patternSize;//plutim cu fereastra NxN peste gridul initial si depasim gridul initial cu o pozitie si stanga si dreapta 
                patternGridSizeY = (int)sizeOfGrid.y + 3 - patternSize;
                rowMax = patternGridSizeY - 1;//-1 pt ca incepem de la rowMin = -1
                colMax = patternGridSizeX - 1;
            }
            else//Cand N>=3: wrap-around cand extind cu mai mult de 1 celula pe fiecare margine, dar fereastra sa contina cel putin un Tilebase real din Tilemap-ul original(restul celulelor sunt umplute prin wrap-around)
            {
                patternGridSizeX = (int)sizeOfGrid.x + patternSize - 1;
                patternGridSizeY = (int)sizeOfGrid.y + patternSize - 1;
                rowMin = 1 - patternSize;
                colMin = 1 - patternSize;
                rowMax = (int)sizeOfGrid.y;
                colMax = (int)sizeOfGrid.x;
            }

            //Matricea de indexi de patterns:
            int[][] patternIndicesGrid = JaggedArray.CreateJaggedArray<int[][]>(patternGridSizeY, patternGridSizeX);
            int totalFrequency = 0;

            //Creez pattern-urile:
            int patternIndex = 0;

            for (int row = rowMin; row < rowMax; row++)//pt fiecare fereastra (fiecare x si y de fereastra)
            {
                for (int col = colMin; col < colMax; col++)
                {
                    // 1) extrag sub-grila N×N pornind de la celula (col,row) care este coltul ferestrei NxN
                    int[][] gridValues = inputManager.GetPatternValuesFromGridAt(col, row, patternSize);
                    // 2) calculez hash-ul pattern-ului
                    string hashValue = Hash.CalculateHashCode(gridValues);

                    if (!patternHashcodeDictionary.ContainsKey(hashValue))//salvam local pattern-urile in dictionare
                    {
                        Pattern pattern = new Pattern(gridValues, hashValue, patternIndex);
                        patternIndex++;
                        AddNewPattern(patternHashcodeDictionary, patternIndexDictionary, hashValue, pattern);
                    }
                    else
                    {
                        if (!equalWeights)//crestem frecventa
                        {
                            var existing = patternHashcodeDictionary[hashValue];//existing= (cheie,valoare) deci patternHashcodeDictionary[hashValue]=valoarea PatternData
                            patternIndexDictionary[existing.Pattern.Index].AddToFrequency();//cresc frecventa pt pattern (frecventa= cate aparitii are pattern-ul); cum dictionarele de sus contin referinte la PatternData objects nu e nevoie sa cresc frecventa decat o data (va fi updatat in ambele dictionare)
                        }
                    }
                    //Cresc frecventa totala:
                    totalFrequency++;
                    //Salvam in matricea de indexi de patterns:
                    if (patternSize < 3)
                    {
                        patternIndicesGrid[row + 1][col + 1] = patternHashcodeDictionary[hashValue].Pattern.Index;//ca sa incepem de la index 0
                    }
                    else
                    {
                        patternIndicesGrid[row + patternSize - 1][col + patternSize - 1] = patternHashcodeDictionary[hashValue].Pattern.Index;//ca sa incepem de la index 0
                    }
                    // // (a) extragem fereastra originală
                    // int[][] baseGrid = valuesManager.GetPatternValuesFromGridAt(col, row, patternSize);

                    // // (b) generăm lista de variante (original + rotații)
                    // var variants = new List<int[][]> { baseGrid };
                    // if (patternSize > 1)
                    // {
                    //     var r90 = Rotate90(baseGrid);
                    //     var r180 = Rotate90(r90);
                    //     var r270 = Rotate90(r180);
                    //     variants.Add(r90);
                    //     variants.Add(r180);
                    //     variants.Add(r270);
                    // }

                    // // (c) pentru fiecare variantă, înregistrăm sau creștem frecvența
                    // foreach (var gv in variants)
                    // {
                    //     string h = Hash.CalculateHashCode(gv);
                    //     if (!patternHashcodeDictionary.ContainsKey(h))
                    //     {
                    //         // pattern nou
                    //         var pat = new Pattern(gv, h, patternIndex);
                    //         AddNewPattern(patternHashcodeDictionary, patternIndexDictionary, h, pat);
                    //         patternIndex++;
                    //     }
                    //     else if (!equalWeights)
                    //     {
                    //         // deja există → creștem doar frecvența
                    //         var existing = patternHashcodeDictionary[h];
                    //         patternIndexDictionary[existing.Pattern.Index].AddToFrequency();
                    //     }

                    //     totalFrequency++;

                    //     // salvăm în matricea de rezultate
                    //     int pid = patternHashcodeDictionary[h].Pattern.Index;
                    //     if (patternSize < 3)
                    //         patternIndicesGrid[row + 1][col + 1] = pid;
                    //     else
                    //         patternIndicesGrid[row + patternSize - 1]
                    //                           [col + patternSize - 1] = pid;
                    // }

                }
            }
            //Calculez frecventa pt patterns
            CalculateRelativeFrequency(patternIndexDictionary, totalFrequency);
            return new PatternResults(patternIndicesGrid, patternIndexDictionary);
        }

        //Rotim patterns:
        private static int[][] Rotate90(int[][] grid)
        {
            int N = grid.Length;
            var r = JaggedArray.CreateJaggedArray<int[][]>(N, N);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                    r[x][N - 1 - y] = grid[y][x];
            return r;
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

        internal static Dictionary<int, PatternNeighbours> FindPossibleNeighboursForAllPatterns(INeighbours strategy, PatternResults patternDataResults)
        {
            return strategy.FindNeighbours(patternDataResults);
        }

        public static PatternNeighbours CheckNeighboursInEachDirection(int x, int y, PatternResults patternDataResults)//luam vecinii unui index din matricea de indexi de patterns
        {
            PatternNeighbours patternNeighbours = new PatternNeighbours();

            foreach (Dir dir in Enum.GetValues(typeof(Dir)))
            {
                int possiblePatternIndex = patternDataResults.GetNeighbourInDirection(x, y, dir);//indexii vecini in toate cele 4 directii
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

