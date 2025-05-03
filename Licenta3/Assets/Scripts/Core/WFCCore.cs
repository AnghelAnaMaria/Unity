using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class WFCCore
    {
        private OutputGrid outputGrid;
        private PatternManager patternManager;
        private int maxIterations;

        public WFCCore(int outputWidth, int outputHeight, int maxIterations, PatternManager patternManager)
        {
            this.outputGrid = new OutputGrid(outputWidth, outputHeight, patternManager.GetNumberOfPatterns());
            this.patternManager = patternManager;
            this.maxIterations = maxIterations;
        }

        public int[][] CreateOutputGrid()
        {
            int iteration = 0;
            while (iteration < this.maxIterations)
            {
                CoreSolver solver = new CoreSolver(this.outputGrid, this.patternManager);
                int innerIteration = 100;
                while (!solver.CheckForConflicts() && !solver.CheckIfSolved())
                {
                    Vector2Int position = solver.GetLowestEntropyCell();
                    solver.CollapseCell(position);
                    solver.Propagate();
                    innerIteration--;
                    if (innerIteration <= 0)
                    {
                        Debug.Log("Propagation is taking too long");
                        return new int[0][];
                    }
                }

                if (solver.CheckForConflicts())
                {
                    Debug.Log("\nConflict occurred. Iteration: " + iteration);
                    iteration++;
                    outputGrid.ResetAllPossibilities();
                    solver = new CoreSolver(this.outputGrid, this.patternManager);
                }
                else
                {
                    Debug.Log("Solved on: " + iteration);
                    this.outputGrid.PrintResultsToConsole();
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

