using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class CoreSolver
    {
        PatternManager patternManager;
        OutputGrid outputGrid;
        CoreHelper coreHelper;
        PropagationHelper propagationHelper;


        //Metode:
        public CoreSolver(OutputGrid outputGrid, PatternManager patternManager)
        {
            this.outputGrid = outputGrid;
            this.patternManager = patternManager;
            this.coreHelper = new CoreHelper(this.patternManager);
            this.propagationHelper = new PropagationHelper(this.outputGrid, this.coreHelper);
        }

        public void Propagate()
        {
            // Cât timp mai sunt perechi de procesat
            while (propagationHelper.PairsToPropagate.Count > 0)
            {
                var propagatePair = propagationHelper.PairsToPropagate.Dequeue();
                if (propagationHelper.CheckIfPairShouldBeProcessed(propagatePair))
                {
                    ProcessCell(propagatePair);
                }
                // Dacă s-a rezolvat complet sau a apărut un conflict, întrerupem propagarea
                if (propagationHelper.CheckForConflicts() || outputGrid.CheckIfGridIsSolved())
                {
                    return;
                }
            }
            // Dacă nu mai avem ce propaga și nu mai avem celule cu entropie:
            // și totuși există un conflict, setăm flag-ul de conflict
            if (propagationHelper.CheckForConflicts() && propagationHelper.PairsToPropagate.Count == 0 && propagationHelper.LowEntropySet.Count == 0)
            {
                propagationHelper.SetConflictFlag();
            }
        }

        private void ProcessCell(VectorPair propagatePair)
        {
            if (outputGrid.CheckIfCellIsCollapsed(propagatePair.CellToPropagatePosition))
            {
                propagationHelper.EnqueueUncollapseNeighbours(propagatePair);
            }
            else
            {
                PropagateNeighbour(propagatePair);
            }
        }

        private void PropagateNeighbour(VectorPair propagatePair)
        {
            var possibleValuesAtNeighbour = outputGrid.GetPossibleValuesForPosition(propagatePair.CellToPropagatePosition);
            int startCount = possibleValuesAtNeighbour.Count;

            RemoveImpossibleNeighbours(propagatePair, possibleValuesAtNeighbour);

            int newPossiblePatternCount = possibleValuesAtNeighbour.Count;
            propagationHelper.AnalyzePropagationResults(propagatePair, startCount, newPossiblePatternCount);
        }

        private void RemoveImpossibleNeighbours(VectorPair propagatePair, HashSet<int> possibleValuesAtNeighbour)
        {
            HashSet<int> possibleIndices = new HashSet<int>();

            foreach (var patternIndexAtBase in outputGrid.GetPossibleValuesForPosition(propagatePair.BaseCellPosition))
            {
                var possibleNeighboursForBase = patternManager.GetPossibleNeighboursForPatternInDirection(patternIndexAtBase, propagatePair.DirectionFromBase);
                possibleIndices.UnionWith(possibleNeighboursForBase);
            }

            possibleValuesAtNeighbour.IntersectWith(possibleIndices);
        }

        public Vector2Int GetLowestEntropyCell()
        {
            if (propagationHelper.LowEntropySet.Count <= 0)
            {
                return outputGrid.GetRandomCell();
            }
            else
            {
                var lowestEntropyElement = propagationHelper.LowEntropySet.First();
                Vector2Int returnVector = lowestEntropyElement.Position;
                propagationHelper.LowEntropySet.Remove(lowestEntropyElement);
                return returnVector;
            }
        }

        public void CollapseCell(Vector2Int cellCoordinates)
        {
            // Obținem lista posibilităților pentru celula dată
            var possibleValues = outputGrid.GetPossibleValuesForPosition(cellCoordinates).ToList();

            // Dacă nu există opțiuni sau doar una, nu facem nimic
            if (possibleValues.Count == 0 || possibleValues.Count == 1)
                return;

            // Alegem soluția bazată pe frecvențe
            int index = coreHelper.SelectSolutionPatternFromFrequency(possibleValues);
            outputGrid.SetPatternOnPosition(cellCoordinates.x, cellCoordinates.y, possibleValues[index]);

            // Verificăm dacă soluția aleasă cauzează un conflict
            if (coreHelper.CheckCellSolutionForCollision(cellCoordinates, outputGrid) == false)
            {
                // Dacă nu e conflict, adăugăm perechile noi pentru propagare
                propagationHelper.AddNewPairsToPropagateQueue(cellCoordinates, cellCoordinates);
            }
            else
            {
                // Dacă e conflict, setăm flag-ul de conflict
                propagationHelper.SetConflictFlag();
            }
        }

        public bool CheckIfSolved()
        {
            return outputGrid.CheckIfGridIsSolved();
        }

        public bool CheckForConflicts()
        {
            return propagationHelper.CheckForConflicts();
        }


    }
}

