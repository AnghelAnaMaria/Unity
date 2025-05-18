using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



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

        //Constructor:
        public PropagationHelper(OutputGrid outputGrid, CoreHelper coreHelper)
        {
            this.outputGrid = outputGrid;
            this.coreHelper = coreHelper;
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
            if (newPossiblePatternCount == 1)//daca am ramas cu 1 pattern dupa eliminare (pt celula tinta)
            {
                cellWithNoSolutionPresent = coreHelper.CheckCellSolutionForCollision(propagatePair.CellToPropagatePosition, outputGrid);//verific daca pattern-ul se potriveste cu toti vecinii
            }

        }

        public void AddNewPairsToPropagateQueue(Vector2Int cellToPropagatePosition, Vector2Int baseCellPosition)
        {
            List<VectorPair> list = coreHelper.Create4DirectionNeighbours(cellToPropagatePosition, baseCellPosition);
            foreach (VectorPair item in list)
            {
                pairsToPropagate.Enqueue(item);
            }
        }

        private void AddToLowEntropySet(Vector2Int cellToPropagatePosition)//in set fiecare celula necolapsata apare o data (cu entropia ei)
        {
            LowEntropyCell elementOfLowEntropySet = lowEntropySet.Where(x => x.position == cellToPropagatePosition).FirstOrDefault();//luam obiectul LowEntropyCell din set corespunzator vectorului argument

            if (elementOfLowEntropySet == null && outputGrid.CheckIfCellIsCollapsed(cellToPropagatePosition) == false)//daca celula e necolapsata si nu e in set
            {
                float entropy = coreHelper.CalculateEntropy(cellToPropagatePosition, outputGrid);//ii calculam entropia
                lowEntropySet.Add(new LowEntropyCell(cellToPropagatePosition, entropy));//o bagam in set
            }
            else
            {
                lowEntropySet.Remove(elementOfLowEntropySet);//sergem celula din set
                elementOfLowEntropySet.entropy = coreHelper.CalculateEntropy(cellToPropagatePosition, outputGrid);//recalculam entropia
                lowEntropySet.Add(elementOfLowEntropySet);//bagam celula iar in set
            }
        }

        internal void EnqueueUncollapseNeighbours(VectorPair propagatePair)
        {
            List<VectorPair> uncollapsedNeighbours = coreHelper.CheckIfNeighboursAreCollapsed(propagatePair, outputGrid);//vecinii necolapsati pt celula tinta
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