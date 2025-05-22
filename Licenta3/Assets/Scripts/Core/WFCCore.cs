using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class WFCCore
    {
        private OutputGrid outputGrid;//grid final
        private PatternManager patternManager;//patterns, vecini pt patterns, strategia
        private int maxIterations;
        private Dictionary<Vector2Int, HashSet<int>> softBanned;//patterns care nu vrem sa apara pe anumite pozitii
        private int outputWidth;
        private int outputHeight;



        public OutputGrid OutputGrid => outputGrid;
        public WFCCore(int outputWidth, int outputHeight, int maxIterations, PatternManager patternManager, Dictionary<Vector2Int, HashSet<int>> softBanned = null)
        {
            this.outputWidth = outputWidth;
            this.outputHeight = outputHeight;
            this.outputGrid = new OutputGrid(outputWidth, outputHeight, patternManager.GetNumberOfPatterns());
            this.maxIterations = maxIterations;
            this.patternManager = patternManager;
            this.softBanned = softBanned;
            //ApplyInitialRestrictions();
        }

        // private void ApplyInitialRestrictions()
        // {
        //     if (initialRestrictions == null) return;
        //     foreach (var restriction in initialRestrictions)//fiecare pereche restriction= (Vector2Int pos, HashSet<int> patterns)
        //     {
        //         outputGrid.RestrictPossibleValuesAt(restriction.Key.x, restriction.Key.y, restriction.Value);
        //     }
        // }

        public int[][] CreateOutputGrid()
        {
            int iteration = 0;
            CoreSolver coreSolver = null;
            while (iteration < this.maxIterations)
            {
                if (coreSolver == null)
                    coreSolver = new CoreSolver(outputGrid, patternManager, softBanned);
                int innerIteration = 100;

                while (!coreSolver.CheckForConflicts() && !coreSolver.CheckIfSolved())//cat timp nu avem coliziuni(conflicte) si cat timp nu s-a rezolvat grila
                {
                    Vector2Int position = coreSolver.GetLowestEntropyCell();
                    coreSolver.CollapseCell(position);//also adds neighbours to queue
                    coreSolver.Propagate();
                    innerIteration--;
                    if (innerIteration <= 0)
                    {
                        Debug.Log("Propagation is taking too long");
                        Debug.Log("Remaining pattern possibilities:");
                        for (int y = 0; y < outputHeight; y++)
                        {
                            for (int x = 0; x < outputWidth; x++)
                            {
                                Vector2Int pos = new Vector2Int(x, y);
                                if (!outputGrid.CheckIfCellIsCollapsed(pos))
                                {
                                    var poss = outputGrid.GetPossibleValuesForPosition(pos);
                                    Debug.Log($"Cell ({x},{y}): {string.Join(",", poss)}");
                                }
                            }
                        }
                        return new int[0][];
                    }
                }

                if (coreSolver.CheckForConflicts())
                {
                    Debug.Log("\nConflict occurred. Iteration: " + iteration);
                    iteration++;
                    outputGrid.ResetAllPossibilities();
                    // ApplyInitialRestrictions();
                    coreSolver = new CoreSolver(this.outputGrid, this.patternManager, softBanned);
                }
                else
                {
                    Debug.Log("Solved on: " + iteration);
                    this.outputGrid.PrintToConsole();
                    break;
                }
            }

            if (iteration >= this.maxIterations)
            {
                Debug.Log("Couldn't solve the tilemap.");

            }

            return this.outputGrid.GetSolvedOutputGrid();
        }

    }
}