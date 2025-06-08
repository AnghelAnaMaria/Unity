using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Helpers;//Am importat namespace-ul ca sa folosim CreateJaggedArray din MyCollectionExtension.cs

namespace WaveFunctionCollapse
{//clasa care imi defineste Tilemap pt WFC
    public class Input : IGenericInput<UnityEngine.Tilemaps.TileBase>
    {
        private Tilemap inputTilemap;

        public Input(Tilemap input)
        {
            inputTilemap = input;
        }

        public IVal<UnityEngine.Tilemaps.TileBase>[][] ReadInputToGrid()
        {
            var grid = ReadInputTileMap();//grid e un TileBase[][] în care fiecare element este un pointer la un TileBase existent în memorie (cel din Tilemap).

            TileBaseVal[][] gridOfValues = null;//cream un 2D array cu instante TileBaseValue
            if (grid != null)
            {
                gridOfValues = JaggedArray.CreateJaggedArray<TileBaseVal[][]>(grid.Length, grid[0].Length);
            }
            //Împachetează fiecare TileBase în TileBaseValue
            for (int row = 0; row < grid.Length; row++)
            {
                for (int col = 0; col < grid[0].Length; col++)
                {
                    gridOfValues[row][col] = new TileBaseVal(grid[row][col]);//Aici ia fiecare TileBase și îl „împachetează” într-o instanță de TileBaseValue.  Nu facem o copie a sprite-ului sau a datelor tile-ului, ci doar păstrăm aceeași referință, dar într-un obiect wrapper care implementează IValue<TileBase>.
                }
            }
            //Returnează IValue<TileBase>[][] către WFC
            return gridOfValues;//Aceste obiecte TileBaseValue sunt stocate în gridOfValues, un jagged array local pe care îl returnezi. 
        }                       //Când returnezi gridOfValues în ReadInputToGrid(), motorul WFC (sau codul care apelează metoda) prinde acest array într-o variabilă proprie. Pe durata execuției, atât gridOfValues, cât și referințele la TileBaseValue (care conțin TileBase-ul), vor rămâne în memorie atâta vreme cât mai există referințe către ele.

        private UnityEngine.Tilemaps.TileBase[][] ReadInputTileMap()//Facem rost de informatiile Tilemap-ului din scena
        {
            Params imageParameters = new Params(inputTilemap);//aici am informatiile Tilemap-ului din scena
            return CreateTileBaseGrid(imageParameters);
        }

        private UnityEngine.Tilemaps.TileBase[][] CreateTileBaseGrid(Params imageParameters)//cream in memorie un Tilemap folosind informatiile din scena
        {
            // 1) Alocăm un jagged array de dimensiunea Height × Width
            UnityEngine.Tilemaps.TileBase[][] gridOfInputTiles = JaggedArray.CreateJaggedArray<UnityEngine.Tilemaps.TileBase[][]>(imageParameters.Height, imageParameters.Width);
            // 2) Populăm fiecare poziție, extrăgând câte un TileContainer din coadă
            for (int row = 0; row < imageParameters.Height; row++)
            {
                for (int col = 0; col < imageParameters.Width; col++)
                {
                    // Dequeue() ne dă următorul TileContainer; luăm proprietatea .Tile= instana TileBase
                    gridOfInputTiles[row][col] = imageParameters.StackOfTiles.Dequeue().Tile;
                }
            }
            // 3) Returnăm grila completă
            return gridOfInputTiles;
        }

    }
}