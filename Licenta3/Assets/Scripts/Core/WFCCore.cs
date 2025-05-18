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
        private Dictionary<Vector2Int, HashSet<int>> initialRestrictions;//pt patterns prima coloana


        public OutputGrid OutputGrid => outputGrid;
        public WFCCore(int outputWidth, int outputHeight, int maxIterations, PatternManager patternManager, Dictionary<Vector2Int, HashSet<int>> initialRestrictions = null)
        {
            this.outputGrid = new OutputGrid(outputWidth, outputHeight, patternManager.GetNumberOfPatterns());
            this.maxIterations = maxIterations;
            this.patternManager = patternManager;
            this.initialRestrictions = initialRestrictions;
            ApplyInitialRestrictions();
        }

        private void ApplyInitialRestrictions()
        {
            if (initialRestrictions == null) return;
            foreach (var kv in initialRestrictions)//fiecare pereche kv= (Vector2Int pos, HashSet<int> patterns)
            {
                outputGrid.RestrictPossibleValuesAt(kv.Key.x, kv.Key.y, kv.Value);
            }
        }

        public int[][] CreateOutputGrid()
        {
            int iteration = 0;
            CoreSolver coreSolver = null;
            while (iteration < this.maxIterations)
            {
                if (coreSolver == null)
                    coreSolver = new CoreSolver(outputGrid, patternManager);
                int innerIteration = 100;

                while (!coreSolver.CheckForConflicts() && !coreSolver.CheckIfSolved())//cat timp nu avem coliziuni(conflicte) si cat timp nu s-a rezolvat grila
                {
                    Vector2Int position = coreSolver.GetLowestEntropyCell();
                    coreSolver.CollapseCell(position);
                    coreSolver.Propagate();
                    innerIteration--;
                    if (innerIteration <= 0)
                    {
                        Debug.Log("Propagation is taking too long");
                        return new int[0][];
                    }
                }

                if (coreSolver.CheckForConflicts())
                {
                    Debug.Log("\nConflict occurred. Iteration: " + iteration);
                    iteration++;
                    outputGrid.ResetAllPossibilities();
                    ApplyInitialRestrictions();
                    coreSolver = new CoreSolver(this.outputGrid, this.patternManager);
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