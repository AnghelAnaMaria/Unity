using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;



namespace WaveFunctionCollapse
{
    public class PropagationHelper
    {
        private OutputGrid outputGrid;//starea curenta a WFC (multimea de pattern-uri inca posibile pt fiecare celula)
        private CoreHelper coreHelper;//evalueaza entropia, genereaza vecinii, detecteaza coliziunile in timpul propagarii
        private bool cellWithNoSolutionPresent;//devine true la coliziune (o celulă rămâne fără niciun pattern posibil sau coliziune imediată)
        private SortedSet<LowEntropyCell> lowEntropySet = new SortedSet<LowEntropyCell>();//ca sa extragem celula cu cea mai mica entropie
        private Queue<VectorPair> pairsToPropagate = new Queue<VectorPair>();//coada de VectorPair pt care celula de baza si-a restrans domeniul de patterns


        //Getters:
        public SortedSet<LowEntropyCell> LowEntropySet { get => lowEntropySet; }
        public Queue<VectorPair> PairsToPropagate { get => pairsToPropagate; }

        //Setters:
        public SortedSet<LowEntropyCell> SetLowEntropySet
        {
            get => lowEntropySet;
            set => lowEntropySet = value;
        }
        public Queue<VectorPair> SetPairsToPropagate
        {
            get => pairsToPropagate;
            set => pairsToPropagate = value;
        }

        //Constructor:
        public PropagationHelper(OutputGrid outputGrid, CoreHelper coreHelper)
        {
            this.outputGrid = outputGrid ?? throw new System.ArgumentNullException("outputGrid");
            this.coreHelper = coreHelper ?? throw new System.ArgumentNullException("coreHelper");
            this.lowEntropySet = new SortedSet<LowEntropyCell>();
            this.pairsToPropagate = new Queue<VectorPair>();
        }

        public bool CheckIfPairShouldBeProcessed(VectorPair propagationPair)
        {
            return outputGrid.CheckIfValidCoords(propagationPair.CellToPropagatePosition)
                && propagationPair.AreWeCheckingPreviousCellAgain() == false;
        }

        public void AnalyzePropagationResults(VectorPair propagatePair, int startCount, int newPossiblePatternCount)
        {//propagatePair= obiect care descrie propagarea de la o celula de baza la o celula tinta
         //startCount= nr paterns inainte de eliminare pt celula tinta;    newPossiblePatternCount= nr patterns dupa eliminare pt celula tinta
            if (newPossiblePatternCount > 1 && startCount > newPossiblePatternCount)//daca am eliminat din patterns posibile pt celula tinta (domeniu restrans, dar nu colapsat)
            {
                AddNewPairsToPropagateQueue(propagatePair.CellToPropagatePosition, propagatePair.BaseCellPosition);//bag in coada de modificari in "lant" toti vecinii celulei tinta
                AddToLowEntropySet(propagatePair.CellToPropagatePosition);//pt fiecare modificare recalculam entropia
            }
            if (newPossiblePatternCount == 0)//daca am ramas fara patterns dupa eliminare (pt celula tinta)
            {
                cellWithNoSolutionPresent = true;//flag
            }
            else if (newPossiblePatternCount == 1)//daca am ramas cu 1 pattern dupa eliminare (pt celula tinta)
            {
                cellWithNoSolutionPresent = coreHelper.CheckCellSolutionForCollision(propagatePair.CellToPropagatePosition, outputGrid);//verific daca pattern-ul se potriveste cu toti vecinii
            }

        }

        public void AddNewPairsToPropagateQueue(Vector2Int cellToPropagatePosition, Vector2Int baseCellPosition)
        {
            List<VectorPair> list = coreHelper.Create4DirectionNeighbours(cellToPropagatePosition, baseCellPosition);//vecinii pt cellToPropagatePosition
            foreach (VectorPair item in list)
            {
                pairsToPropagate.Enqueue(item);
            }
        }

        public void AddToLowEntropySet(Vector2Int cellToPropagatePosition)//in set fiecare celula necolapsata apare o data (cu entropia ei)
        {
            if (coreHelper == null)
            {
                Debug.LogError("coreHelper is null!");
                return;
            }
            if (outputGrid == null)
            {
                Debug.LogError("outputGrid is null!");
                return;
            }
            if (lowEntropySet == null)
            {
                Debug.LogError("lowEntropySet is null!");
                return;
            }

            // Always remove any old cell for this position (if it exists)
            lowEntropySet.RemoveWhere(x => x.position == cellToPropagatePosition);

            // Only add if not collapsed
            if (!outputGrid.CheckIfCellIsCollapsed(cellToPropagatePosition))
            {
                float entropy = coreHelper.CalculateEntropy(cellToPropagatePosition, outputGrid);
                lowEntropySet.Add(new LowEntropyCell(cellToPropagatePosition, entropy));
            }
        }

        internal void EnqueueUncollapseNeighbours(VectorPair propagatePair)
        {
            List<VectorPair> uncollapsedNeighbours = coreHelper.ReturnUncollapsedNeighbours(propagatePair, outputGrid);//vecinii necolapsati pt celula tinta
            foreach (VectorPair uncollapsed in uncollapsedNeighbours)//bagam vecinii necolapsati in coada
            {
                pairsToPropagate.Enqueue(uncollapsed);
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