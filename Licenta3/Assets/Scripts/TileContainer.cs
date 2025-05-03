using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace WaveFunctionCollapse
{//Clasa asta obtine informatiile Tilebase-ului din scena
    public class TileContainer
    {
        public TileBase Tile { get; set; }//referinta la Tilebase din Tilemap
        public int X { get; set; }
        public int Y { get; set; }


        //Constructor:
        public TileContainer(TileBase tile, int X, int Y)
        {
            this.Tile = tile;
            this.X = X;
            this.Y = Y;
        }
    }
}


