using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class WFC
    {
        private OutputGrid outputGrid;//grid final
        private PatternManager patternManager;//patterns, vecini pt patterns, strategia
        private int maxIterations;
        private Dictionary<Vector2Int, HashSet<int>> softBanned;//patterns care nu vrem sa apara pe anumite pozitii
        private Dictionary<Vector2Int, HashSet<int>> initialRestrictions;
        int maxBacktrackSteps;
        private HashSet<int> middlePatterns;
        public List<Vector2Int> CollapseOrder { get; } = new List<Vector2Int>();//pt animatie



        public OutputGrid OutputGrid => outputGrid;
        public WFC(int outputWidth, int outputHeight, int maxIterations, PatternManager patternManager, int maxBacktrackSteps, HashSet<int> middlePatterns, Dictionary<Vector2Int, HashSet<int>> softBanned = null, Dictionary<Vector2Int, HashSet<int>> initialRestrictions = null)
        {
            this.outputGrid = new OutputGrid(outputWidth, outputHeight, patternManager.GetNumberOfPatterns());
            this.maxIterations = maxIterations;
            this.patternManager = patternManager;
            this.maxBacktrackSteps = maxBacktrackSteps;
            this.middlePatterns = middlePatterns;
            this.softBanned = softBanned;
            this.initialRestrictions = initialRestrictions;
            ApplyInitialRestrictions();
        }

        private void ApplyInitialRestrictions()
        {
            if (initialRestrictions == null) return;
            foreach (var restriction in initialRestrictions)//fiecare pereche restriction= (Vector2Int pos, HashSet<int> patterns)
            {
                outputGrid.RestrictPossibleValuesAt(restriction.Key.x, restriction.Key.y, restriction.Value);
            }
        }

        public int[][] CreateOutputGrid()
        {
            CollapseOrder.Clear();
            int iteration = 0;
            SolverManager solverManager = new SolverManager(outputGrid, patternManager, maxBacktrackSteps, middlePatterns, softBanned);
            solverManager.OnCellCollapsed = (pos, pat) => CollapseOrder.Add(pos);//ma abonez la evenimenul OnCellCollapsed


            while (iteration < this.maxIterations)
            {
                int innerIteration = 100;

                while (!solverManager.CheckForConflicts() && !solverManager.CheckIfSolved())//cat timp nu avem coliziuni(conflicte) si cat timp nu s-a rezolvat grila
                {
                    Vector2Int position = solverManager.GetLowestEntropyCell();
                    solverManager.CollapseCell(position);//also adds neighbours to queue
                    solverManager.Propagate();
                    innerIteration--;
                    if (innerIteration <= 0)
                    {
                        Debug.Log("Propagation is taking too long");
                        return new int[0][];
                    }
                }

                if (solverManager.CheckForConflicts())
                {
                    Debug.Log("\nConflict occurred. Iteration: " + iteration);

                    bool didBacktrack = solverManager.BacktrackLastSteps();
                    if (!didBacktrack)
                    {
                        iteration++;
                        outputGrid.ResetAllPossibilities();
                        ApplyInitialRestrictions();
                        solverManager = new SolverManager(this.outputGrid, this.patternManager, this.maxBacktrackSteps, this.middlePatterns, softBanned);
                        solverManager.OnCellCollapsed = (pos, pat) => CollapseOrder.Add(pos);

                        if (initialRestrictions != null)
                        {
                            foreach (var kvp in initialRestrictions)
                            {
                                var pos = kvp.Key;
                                var allowed = kvp.Value;
                                if (allowed.Count == 1)
                                {
                                    // Folosește chiar CollapseCell — el știe să pună pattern-ul când e o singură posibilitate
                                    solverManager.CollapseCell(pos);
                                    // 3) Apoi propagatează imediat
                                    solverManager.Propagate();
                                }
                            }
                        }

                    }
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