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


namespace WaveFunctionCollapse
{
    public class CellPair
    {
        public Vector2Int BaseCellPosition { get; set; }//poziția celulei de bază din care pornim propagarea
        public Vector2Int CellToPropagatePosition { get; set; }//poziția celulei în care vom propaga
        public Dir DirectionFromBase { get; set; }//direcția în care propagăm de la celula de bază
        public Vector2Int PreviousCellPosition { get; set; }//poziția celulei procesate anterior (pentru backtracking, dacă e nevoie)

        //Metode:
        public CellPair(Vector2Int baseCellPosition, Vector2Int cellToPropagatePosition, Dir directionFromBase, Vector2Int previousCellPosition)
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