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
        Dictionary<Vector2Int, HashSet<int>> softBanned;


        //Metode:
        public CoreSolver(OutputGrid outputGrid, PatternManager patternManager, Dictionary<Vector2Int, HashSet<int>> softBanned)
        {
            this.outputGrid = outputGrid;
            this.patternManager = patternManager;
            this.coreHelper = new CoreHelper(this.patternManager);
            this.softBanned = softBanned;
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

        private void PropagateNeighbour(VectorPair propagatePair)//pt celula tinta; patterns compatibile gasite sunt salvate in dictionarul din OutputGrid
        {
            HashSet<int> possibleValuesAtNeighbour = outputGrid.GetPossibleValuesForPosition(propagatePair.CellToPropagatePosition);//patterns care pot sta in celula tinta
            int startCount = possibleValuesAtNeighbour.Count;//cate celule pot sta in celula tinta

            RemoveImpossibleNeighbours(propagatePair, possibleValuesAtNeighbour);//scapam de patterns incompatibile pt celula tinta

            int newPossiblePatternCount = possibleValuesAtNeighbour.Count;//cu cate pattern-uri compatibile am ramas
            propagationHelper.AnalyzePropagationResults(propagatePair, startCount, newPossiblePatternCount);
        }

        private void RemoveImpossibleNeighbours(VectorPair propagatePair, HashSet<int> possibleValuesAtNeighbour)//pt celula baza
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

            // float alpha = 0.05f;//we can change this value
            // LowEntropyCell bestCell = null;
            // float minScore = float.MaxValue;

            // foreach (var cell in propagationHelper.LowEntropySet)//pt fiecare LowEntropyCell cell din set
            // {
            //     int uncollapsedNeighbors = GetUncollapsedNeighborCount(cell.position);
            //     float score = cell.entropy - alpha * uncollapsedNeighbors;
            //     if (score < minScore)
            //     {
            //         minScore = score;
            //         bestCell = cell;
            //     }
            // }

            // if (bestCell == null)
            //     return outputGrid.GetRandomCellCoords();

            // propagationHelper.LowEntropySet.Remove(bestCell);//eliminam din set
            // Debug.Log("celula cu entropie minima: " + bestCell.position);
            // return bestCell.position;//returnam pozitia (x,y) a celulei cu entropie minima

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

            if (possibleValues.Count == 0)
            {
                propagationHelper.SetConflictFlag();
                Debug.Log("Am ramas cu 0 patterns pt celula.");
                return;
            }
            if (possibleValues.Count == 1)
            {
                outputGrid.SetPatternOnPosition(cellCoordinates.x, cellCoordinates.y, possibleValues[0]);
                propagationHelper.AddNewPairsToPropagateQueue(cellCoordinates, cellCoordinates);
                return;
            }

            var backup = new HashSet<int>(possibleValues);

            var validValues = new List<int>();
            foreach (int patternId in possibleValues)
            {
                outputGrid.SetPatternOnPosition(cellCoordinates.x, cellCoordinates.y, patternId);//temporar
                bool hasConflict = coreHelper.CheckCellSolutionForCollision(cellCoordinates, outputGrid);//check for collision
                outputGrid.ClearCell(cellCoordinates.x, cellCoordinates.y); // Undo
                outputGrid.SetPossiblePatterns(cellCoordinates.x, cellCoordinates.y, backup);

                if (!hasConflict)
                    validValues.Add(patternId);
            }

            if (validValues.Count == 0)
            {
                propagationHelper.SetConflictFlag();
                return;
            }

            // continue as before, but pick from the *filtered* list
            int chosenPatternId = coreHelper.SelectSolutionPatternFromFrequency(validValues, cellCoordinates, softBanned);

            outputGrid.SetPatternOnPosition(cellCoordinates.x, cellCoordinates.y, chosenPatternId);

            propagationHelper.AddNewPairsToPropagateQueue(cellCoordinates, cellCoordinates);

        }

        private int GetUncollapsedNeighborCount(Vector2Int cell)
        {
            int count = 0;
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                Vector2Int neighbor = cell;
                switch (dir)
                {
                    case Direction.Up: neighbor += Vector2Int.up; break;
                    case Direction.Down: neighbor += Vector2Int.down; break;
                    case Direction.Left: neighbor += Vector2Int.left; break;
                    case Direction.Right: neighbor += Vector2Int.right; break;
                }
                if (outputGrid.CheckIfValidCoords(neighbor) && !outputGrid.CheckIfCellIsCollapsed(neighbor))
                {
                    count++;
                }
            }
            return count;
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