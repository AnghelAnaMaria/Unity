using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;


namespace WaveFunctionCollapse
{
    struct CollapseStep
    {
        public Vector2Int cell;
        public HashSet<int> previousPossibilities;
    }

    public class SolverManager
    {
        PatternManager patternManager;//rezultatul= grila de int care reprezinta grila finala de Tiles
        OutputGrid outputGrid;//starea curenta a WFC (multimea de pattern-uri inca posibile pt fiecare celula)
        HelperManager coreHelper;//evalueaza entropia, genereaza vecinii, detecteaza coliziunile in timpul propagarii
        Propagation propagationHelper;//logica pt VectorPair
        Dictionary<Vector2Int, HashSet<int>> softBanned;
        private HashSet<int> middlePatterns;
        private Stack<CollapseStep> lastSteps = new Stack<CollapseStep>();
        private int maxBacktrackSteps;
        public System.Action<Vector2Int, int> OnCellCollapsed;
        //System.Action<Vector2Int, int> este delegat= functie care primeste drept parametri un Vector2Int si un int =>DELAGAT
        //OnCellCollapsed este un field care poate ține referința la una sau mai multe metode cu semnătura Action<Vector2Int,int> =>EVENT

        //Metode:
        public SolverManager(OutputGrid outputGrid, PatternManager patternManager, int maxBacktrackSteps, HashSet<int> middlePatterns, Dictionary<Vector2Int, HashSet<int>> softBanned)
        {
            this.outputGrid = outputGrid;
            this.patternManager = patternManager;
            this.maxBacktrackSteps = maxBacktrackSteps;
            this.middlePatterns = middlePatterns;
            this.coreHelper = new HelperManager(this.patternManager);
            this.softBanned = softBanned;
            this.propagationHelper = new Propagation(this.outputGrid, this.coreHelper);
        }

        public void Propagate()
        {
            //Cât timp mai sunt perechi de procesat in coada
            while (propagationHelper.PairsToPropagate.Count > 0)
            {
                CellPair propagatePair = propagationHelper.PairsToPropagate.Dequeue();//luam din coada
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

        private void ProcessCell(CellPair propagatePair)
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

        private void PropagateNeighbour(CellPair propagatePair)//pt celula tinta; patterns compatibile gasite sunt salvate in dictionarul din OutputGrid
        {
            Debug.Log("Am apelat metoda PropagateNeighbour");
            HashSet<int> possibleValuesAtNeighbour = outputGrid.GetPossibleValuesForPosition(propagatePair.CellToPropagatePosition);//patterns care pot sta in celula tinta
            int startCount = possibleValuesAtNeighbour.Count;//cate celule pot sta in celula tinta
            Debug.Log("Patterns posibile (si incompatibile): " + possibleValuesAtNeighbour.Count);

            RemoveImpossibleNeighbours(propagatePair, possibleValuesAtNeighbour);//scapam de patterns incompatibile pt celula tinta
            Debug.Log("Patterns compatibile: " + possibleValuesAtNeighbour.Count);

            int newPossiblePatternCount = possibleValuesAtNeighbour.Count;//cu cate pattern-uri compatibile am ramas
            propagationHelper.AnalyzePropagationResults(propagatePair, startCount, newPossiblePatternCount);
        }

        private void RemoveImpossibleNeighbours(CellPair propagatePair, HashSet<int> possibleValuesAtNeighbour)//pt celula baza
        {
            HashSet<int> possibleIndices = new HashSet<int>();

            foreach (int patternIndexAtBase in outputGrid.GetPossibleValuesForPosition(propagatePair.BaseCellPosition))//pt fiecare pattern care poate sta in celula baza
            {
                HashSet<int> possibleNeighboursForBase = patternManager.GetPossibleNeighboursForPatternInDirection(patternIndexAtBase, propagatePair.DirectionFromBase);//ne uitam la patterns vecine (in sus/ jos/ stanga/ dreapta) pattern-ului din celula de baza
                possibleIndices.UnionWith(possibleNeighboursForBase);
            }

            possibleValuesAtNeighbour.IntersectWith(possibleIndices);//adica scapam de patterns incompatibile

            if (possibleValuesAtNeighbour.Count == 1)
            {
                int onlyPattern = possibleValuesAtNeighbour.First();
                // 1) fixezi pattern-ul în grilă
                // outputGrid.SetPatternOnPosition(propagatePair.CellToPropagatePosition.x, propagatePair.CellToPropagatePosition.y, onlyPattern);
                // 2) declanșezi evenimentul ca WFC (clasa părinte) să adauge poziția în CollapseOrder
                OnCellCollapsed?.Invoke(propagatePair.CellToPropagatePosition, onlyPattern);
                // 3) continui propagarea de aici
                // propagationHelper.AddNewPairsToPropagateQueue(propagatePair.CellToPropagatePosition, propagatePair.CellToPropagatePosition);
                return;
            }

            if (IsInMiddle(propagatePair.BaseCellPosition))
                possibleValuesAtNeighbour.ExceptWith(middlePatterns);
        }

        private bool IsInMiddle(Vector2Int cell)
        {
            int middleWidth = outputGrid.width / 3;
            int middleHeight = outputGrid.height / 3;
            int middleStartX = (outputGrid.width - middleWidth) / 2;
            int middleStartY = (outputGrid.height - middleHeight) / 2;
            return cell.x >= middleStartX && cell.x < middleStartX + middleWidth &&
                   cell.y >= middleStartY && cell.y < middleStartY + middleHeight;
        }


        public Vector2Int GetLowestEntropyCell()
        {
            Debug.Log("Am apelat metoda GetLowestEntropyCell");
            if (propagationHelper.LowEntropySet.Count <= 0)
            {
                var coords = outputGrid.GetRandomCellCoords();
                Debug.Log("Low entropy random coords: " + coords);
                return coords;
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
                Debug.Log("Low entropy cell coords: " + returnVector);
                return returnVector;//returnam pozitia (x,y) a celulei cu entropie minima
            }
        }

        public void CollapseCell(Vector2Int cellCoordinates)
        {
            Debug.Log("Am apelat metoda CollapseCell");
            //Backtrack last 5 steps:
            lastSteps.Push(new CollapseStep //save the current possibilities for this cell before collapsing
            {
                cell = cellCoordinates,
                previousPossibilities = outputGrid.GetPossibleValuesForPosition(cellCoordinates)
            });
            if (lastSteps.Count > maxBacktrackSteps)
            {
                // Remove oldest if >5
                var temp = lastSteps.ToArray();
                lastSteps.Clear();
                for (int i = temp.Length - 1; i >= temp.Length - maxBacktrackSteps; i--)
                    lastSteps.Push(temp[i]);
            }
            Debug.Log("Avem " + lastSteps.Count + " pasi in coada de backtrack.");



            //Rest:
            //List<int> possibleValues = outputGrid.GetPossibleValuesForPosition(cellCoordinates).ToList();//construiesc lista de pattern-uri (ID-uri) care mai pot sta pe celulă
            var possibleSet = outputGrid.GetPossibleValuesForPosition(cellCoordinates);
            if (IsInMiddle(cellCoordinates))
                possibleSet = possibleSet.Except(middlePatterns).ToHashSet();

            List<int> possibleValues = possibleSet.ToList();
            Debug.Log("Avem " + possibleValues.Count + " patterns posibile pt celula cu coordonate " + cellCoordinates);

            var backup = new HashSet<int>(possibleValues);
            Debug.Log("In backup avem tot " + backup.Count + " patterns pt celula cu coordonate " + cellCoordinates);

            if (possibleValues.Count == 0)
            {
                Debug.Log("Am ramas cu 0 patterns pt celula.");
                propagationHelper.SetConflictFlag();
                return;
            }
            if (possibleValues.Count == 1)
            {
                Debug.Log("Am ramas cu 1 pattern pt ccelula");
                outputGrid.SetPatternOnPosition(cellCoordinates.x, cellCoordinates.y, possibleValues[0]);
                OnCellCollapsed?.Invoke(cellCoordinates, possibleValues[0]);
                propagationHelper.AddNewPairsToPropagateQueue(cellCoordinates, cellCoordinates);
                return;
            }

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
                Debug.Log("nu mai avem patterns valide pt celula");
                return;
            }

            // continue as before, but pick from the *filtered* list
            int chosenPatternId = coreHelper.SelectSolutionPatternFromFrequency(validValues, cellCoordinates, softBanned);
            Debug.Log("pattern ales: " + chosenPatternId);


            outputGrid.SetPatternOnPosition(cellCoordinates.x, cellCoordinates.y, chosenPatternId);
            OnCellCollapsed?.Invoke(cellCoordinates, chosenPatternId);

            propagationHelper.AddNewPairsToPropagateQueue(cellCoordinates, cellCoordinates);

        }

        private int GetUncollapsedNeighborCount(Vector2Int cell)
        {
            int count = 0;
            foreach (Dir dir in System.Enum.GetValues(typeof(Dir)))
            {
                Vector2Int neighbor = cell;
                switch (dir)
                {
                    case Dir.Up: neighbor += Vector2Int.up; break;
                    case Dir.Down: neighbor += Vector2Int.down; break;
                    case Dir.Left: neighbor += Vector2Int.left; break;
                    case Dir.Right: neighbor += Vector2Int.right; break;
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

        public bool BacktrackLastSteps()
        {
            if (lastSteps.Count == 0)
                return false;

            int stepsToBacktrack = Mathf.Min(lastSteps.Count, maxBacktrackSteps);//5 pasi (sau mai putini, daca nu avem 5)
            var stepsToRestore = new List<CollapseStep>();//punem pasii aici
            for (int i = 0; i < stepsToBacktrack; i++)
                stepsToRestore.Add(lastSteps.Pop());//scoatem din coada pasii, punem in lista cu pasi


            stepsToRestore.Reverse();//cel mai vechi pas devine primul
            foreach (var step in stepsToRestore)
            {
                // 1. Restore possibilities
                outputGrid.SetPossiblePatterns(step.cell.x, step.cell.y, step.previousPossibilities);

                // 2. Add to propagation queue (so the constraints are re-propagated)
                // Assuming you can access the queue here or via PropagationHelper:
                propagationHelper.PairsToPropagate.Enqueue(new CellPair(step.cell, step.cell, Dir.Up, step.cell));

                // 3. Recalculate entropy (add back to low entropy set)
                propagationHelper.AddToLowEntropySet(step.cell);
            }
            return true;
        }



    }
}