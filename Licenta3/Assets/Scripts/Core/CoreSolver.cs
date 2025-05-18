using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class CoreSolver
    {
        PatternManager patternManager;//rezultatul= grila de int care reprezinta grila finala de Tiles
        OutputGrid outputGrid;//starea curenta a WFC (multimea de pattern-uri inca posibile pt fiecare celula)
        CoreHelper coreHelper;//evalueaza entropia, genereaza vecinii, detecteaza coliziunile in timpul propagarii
        PropagationHelper propagationHelper;//logica pt VectorPair


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
            //Cât timp mai sunt perechi de procesat in coada
            while (propagationHelper.PairsToPropagate.Count > 0)
            {
                VectorPair propagatePair = propagationHelper.PairsToPropagate.Dequeue();//luam din coada
                if (propagationHelper.CheckIfPairShouldBeProcessed(propagatePair))//daca celula tinta nu e in afara gridului si nu ne intoarcem inapoi la celula baza
                {
                    ProcessCell(propagatePair);
                }
                // Dacă s-a rezolvat complet sau a apărut un conflict, întrerupem propagarea
                if (propagationHelper.CheckForConflicts() || outputGrid.CheckIfGridIsSolved())
                {
                    return;
                }
            }
            // Dacă nu mai avem ce propaga și nu mai avem celule cu entropie: și totuși există un conflict, setăm flag-ul de conflict
            if (propagationHelper.CheckForConflicts() && propagationHelper.PairsToPropagate.Count == 0 && propagationHelper.LowEntropySet.Count == 0)
            {
                propagationHelper.SetConflictFlag();
            }
        }

        private void ProcessCell(VectorPair propagatePair)
        {
            if (outputGrid.CheckIfCellIsCollapsed(propagatePair.CellToPropagatePosition))//daca celula tinta e colapsata
            {
                propagationHelper.EnqueueUncollapseNeighbours(propagatePair);//bagam vecinii necolapsati ai celulei tinta in coada
            }
            else
            {
                PropagateNeighbour(propagatePair);
            }
        }

        private void PropagateNeighbour(VectorPair propagatePair)
        {
            HashSet<int> possibleValuesAtNeighbour = outputGrid.GetPossibleValuesForPosition(propagatePair.CellToPropagatePosition);//patterns care pot sta in celula tinta
            int startCount = possibleValuesAtNeighbour.Count;//cate celule pot sta in celula tinta

            RemoveImpossibleNeighbours(propagatePair, possibleValuesAtNeighbour);//scapam de patterns incompatibile pt celula tinta

            int newPossiblePatternCount = possibleValuesAtNeighbour.Count;//cu cate pattern-uri compatibile am ramas
            propagationHelper.AnalyzePropagationResults(propagatePair, startCount, newPossiblePatternCount);
        }

        private void RemoveImpossibleNeighbours(VectorPair propagatePair, HashSet<int> possibleValuesAtNeighbour)
        {
            HashSet<int> possibleIndices = new HashSet<int>();

            foreach (int patternIndexAtBase in outputGrid.GetPossibleValuesForPosition(propagatePair.BaseCellPosition))//pt fiecare pattern care poate sta in celula baza
            {
                HashSet<int> possibleNeighboursForBase = patternManager.GetPossibleNeighboursForPatternInDirection(patternIndexAtBase, propagatePair.DirectionFromBase);//ne uitam la patterns vecine (in sus/ jos/ stanga/ dreapta) pattern-ului din celula de baza
                possibleIndices.UnionWith(possibleNeighboursForBase);
            }

            possibleValuesAtNeighbour.IntersectWith(possibleIndices);//adica scapam de patterns vecine incompatibile
        }

        public Vector2Int GetLowestEntropyCell()
        {
            if (propagationHelper.LowEntropySet.Count <= 0)
            {
                return outputGrid.GetRandomCellCoords();
            }
            else
            {
                LowEntropyCell lowestEntropyElement = propagationHelper.LowEntropySet.First();//luam din set
                Vector2Int returnVector = lowestEntropyElement.position;
                propagationHelper.LowEntropySet.Remove(lowestEntropyElement);//eliminam din set
                return returnVector;//returnam pozitia (x,y) a celulei cu entropie minima
            }
        }

        public void CollapseCell(Vector2Int cellCoordinates)
        {
            List<int> possibleValues = outputGrid.GetPossibleValuesForPosition(cellCoordinates).ToList();//construiesc lista de pattern-uri (ID-uri) care mai pot sta pe celulă

            if (possibleValues.Count == 0 || possibleValues.Count == 1)
                return;

            int index = coreHelper.SelectSolutionPatternFromFrequency(possibleValues);//alegem, din lista de mai sus, soluția(index-ul) bazată pe frecvențe
            outputGrid.SetPatternOnPosition(cellCoordinates.x, cellCoordinates.y, possibleValues[index]);//salvam pattern-ul pt celula

            //verificăm dacă soluția aleasă cauzează un conflict
            if (coreHelper.CheckCellSolutionForCollision(cellCoordinates, outputGrid) == false)
            {
                propagationHelper.AddNewPairsToPropagateQueue(cellCoordinates, cellCoordinates);//dacă nu e conflict, adăugăm vecinii celulei in coada
            }
            else
            {
                propagationHelper.SetConflictFlag();//dacă e conflict, setăm flag-ul de conflict
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