using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace WaveFunctionCollapse
{
    public class VectorPair
    {
        public Vector2Int BaseCellPosition { get; set; }//poziția celulei de bază din care pornim propagarea
        public Vector2Int CellToPropagatePosition { get; set; }//poziția celulei în care vom propaga
        public Direction DirectionFromBase { get; set; }//direcția în care propagăm de la celula de bază
        public Vector2Int PreviousCellPosition { get; set; }//poziția celulei procesate anterior (pentru backtracking, dacă e nevoie)

        //Metode:
        public VectorPair(Vector2Int baseCellPosition, Vector2Int cellToPropagatePosition, Direction directionFromBase, Vector2Int previousCellPosition)
        {
            BaseCellPosition = baseCellPosition;
            CellToPropagatePosition = cellToPropagatePosition;
            DirectionFromBase = directionFromBase;
            PreviousCellPosition = previousCellPosition;
        }

        public bool AreWeCheckingPreviousCellAgain()
        {
            return PreviousCellPosition == CellToPropagatePosition;
        }
    }
}

