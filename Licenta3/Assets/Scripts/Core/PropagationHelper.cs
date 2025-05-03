using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



namespace WaveFunctionCollapse
{
    public class PropagationHelper
    {
        private OutputGrid outputGrid;
        private CoreHelper coreHelper;
        private bool cellWithNoSolutionPresent;
        private SortedSet<LowEntropyCell> lowEntropySet = new SortedSet<LowEntropyCell>();
        private Queue<VectorPair> pairsToPropagate = new Queue<VectorPair>();



        public SortedSet<LowEntropyCell> LowEntropySet { get => lowEntropySet; }
        public Queue<VectorPair> PairsToPropagate { get => pairsToPropagate; }

        public PropagationHelper(OutputGrid outputGrid, CoreHelper coreHelper)
        {
            this.outputGrid = outputGrid;
            this.coreHelper = coreHelper;
        }

        public bool CheckIfPairShouldBeProcessed(VectorPair propagationPair)
        {
            return outputGrid.CheckIfValidPosition(propagationPair.CellToPropagatePosition)
                && propagationPair.AreWeCheckingPreviousCellAgain() == false;
        }

        public void AnalyzePropagationResults(VectorPair propagatePair, int startCount, int newPossiblePatternCount)
        {
            if (newPossiblePatternCount > 1 && startCount > newPossiblePatternCount)
            {
                AddNewPairsToPropagateQueue(propagatePair.CellToPropagatePosition, propagatePair.BaseCellPosition);
                AddToLowEntropySet(propagatePair.CellToPropagatePosition);
            }
            if (newPossiblePatternCount == 0)
            {
                cellWithNoSolutionPresent = true;
            }
            if (newPossiblePatternCount == 1)
            {
                cellWithNoSolutionPresent = coreHelper.CheckCellSolutionForCollision(propagatePair.CellToPropagatePosition, outputGrid);
            }
        }

        internal void EnqueueUncollapseNeighbours(VectorPair propagatePair)
        {
            var uncollapsedNeighbours = coreHelper.CheckIfNeighboursAreCollapsed(propagatePair, outputGrid);
            foreach (var uncollapsed in uncollapsedNeighbours)
            {
                pairsToPropagate.Enqueue(uncollapsed);
            }
        }


        private void AddToLowEntropySet(Vector2Int cellToPropagatePosition)
        {
            var elementOfLowEntropySet = lowEntropySet
                .Where(x => x.Position == cellToPropagatePosition)
                .FirstOrDefault();

            if (elementOfLowEntropySet == null && outputGrid.CheckIfCellIsCollapsed(cellToPropagatePosition) == false)
            {
                float entropy = coreHelper.CalculateEntropy(cellToPropagatePosition, outputGrid);
                lowEntropySet.Add(new LowEntropyCell(cellToPropagatePosition, entropy));
            }
            else
            {
                lowEntropySet.Remove(elementOfLowEntropySet);
                elementOfLowEntropySet.Entropy = coreHelper.CalculateEntropy(cellToPropagatePosition, outputGrid);
                lowEntropySet.Add(elementOfLowEntropySet);
            }
        }

        public void AddNewPairsToPropagateQueue(Vector2Int cellToPropagatePosition, Vector2Int baseCellPosition)
        {
            var list = coreHelper.Create4DirectionNeighbours(cellToPropagatePosition, baseCellPosition);
            foreach (var item in list)
            {
                pairsToPropagate.Enqueue(item);
            }
        }

        public bool CheckForConflicts()
        {
            return cellWithNoSolutionPresent;
        }

        public void SetConflictFlag()
        {
            cellWithNoSolutionPresent = true;
        }

    }
}

