// MIT License
// 
// Copyright (c) 2020 Sunny Valley Studio
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
// Modified by: Anghel Ana-Maria, iulie 2025 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.VisualScripting;



namespace WaveFunctionCollapse
{
    public class Propagation
    {
        private OutputGrid outputGrid;//starea curenta a WFC (multimea de pattern-uri inca posibile pt fiecare celula)
        private HelperManager coreHelper;//evalueaza entropia, genereaza vecinii, detecteaza coliziunile in timpul propagarii
        private bool cellWithNoSolutionPresent;//devine true la coliziune (o celulă rămâne fără niciun pattern posibil sau coliziune imediată)
        private SortedSet<LowEntropyCell> lowEntropySet = new SortedSet<LowEntropyCell>();//ca sa extragem celula cu cea mai mica entropie
        private Queue<CellPair> pairsToPropagate = new Queue<CellPair>();//coada de VectorPair pt care celula de baza si-a restrans domeniul de patterns


        //Getters:
        public SortedSet<LowEntropyCell> LowEntropySet { get => lowEntropySet; }
        public Queue<CellPair> PairsToPropagate { get => pairsToPropagate; }

        //Setters:
        public SortedSet<LowEntropyCell> SetLowEntropySet
        {
            get => lowEntropySet;
            set => lowEntropySet = value;
        }
        public Queue<CellPair> SetPairsToPropagate
        {
            get => pairsToPropagate;
            set => pairsToPropagate = value;
        }

        //Constructor:
        public Propagation(OutputGrid outputGrid, HelperManager coreHelper)
        {
            this.outputGrid = outputGrid ?? throw new System.ArgumentNullException("null outputGrid");
            this.coreHelper = coreHelper ?? throw new System.ArgumentNullException("null coreHelper");
            this.lowEntropySet = new SortedSet<LowEntropyCell>();
            this.pairsToPropagate = new Queue<CellPair>();
        }

        public bool CheckIfPairShouldBeProcessed(CellPair propagationPair)
        {
            return outputGrid.CheckIfValidCoords(propagationPair.CellToPropagatePosition)
                && propagationPair.AreWeCheckingPreviousCellAgain() == false;
        }

        public void AnalyzePropagationResults(CellPair propagatePair, int startCount, int newPossiblePatternCount)
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
            List<CellPair> list = coreHelper.Create4DirectionNeighbours(cellToPropagatePosition, baseCellPosition);//vecinii pt cellToPropagatePosition
            foreach (CellPair item in list)
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

        internal void EnqueueUncollapseNeighbours(CellPair propagatePair)
        {
            List<CellPair> uncollapsedNeighbours = coreHelper.ReturnUncollapsedNeighbours(propagatePair, outputGrid);//vecinii necolapsati pt celula tinta
            foreach (CellPair uncollapsed in uncollapsedNeighbours)//bagam vecinii necolapsati in coada
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