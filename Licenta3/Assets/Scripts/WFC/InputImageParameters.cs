using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;
using System;
using System.Linq;


namespace WaveFunctionCollapse
{//Clasa asta obtine informatiile Tilemap-ului din scena
    public class InputImageParameters
    {
        public Vector2Int? bottomRightTileCoords = null;
        public Vector2Int? topLeftTileCoords = null; //coordOpt poate fi null sau un Vector2Int valid
        public BoundsInt inputTileMapBounds;//struct in care retin marginile x si y ale Tilemap-ului din scena
        public TileBase[] inputTilemapTilesArray;//aici pun TileBase-urile din scena Unity (fac asta in constructor)
        public Queue<TileContainer> stackOfTiles = new Queue<TileContainer>();//stack cu referinte la Tilebase si pozitia lor (x si y)
        private int width = 0,
                    height = 0;
        private Tilemap inputTilemap;//referinta la Tilemap din scena de unde ne salvam datele 



        //Getteri & Setteri:
        public Queue<TileContainer> StackOfTiles
        {
            get => stackOfTiles;
            set => stackOfTiles = value;
        }
        public int Height
        {
            get => height;
            set => height = value;
        }
        public int Width
        {
            get => width;
            set => width = value;
        }

        //Constructor:
        public InputImageParameters(Tilemap inputTilemap)
        {
            this.inputTilemap = inputTilemap;
            this.inputTileMapBounds = this.inputTilemap.cellBounds;//cellBounds e fct in Unity, in clasa Tilemap.
            this.inputTilemapTilesArray = this.inputTilemap.GetTilesBlock(this.inputTileMapBounds);//extragi toate tile-urile TileBase din acel dreptunghi, sub forma unui array 1D (de la stanga jos pana la dreapta sus). GetTilesBlock e fct in Unity, in clasa Tilemap.
            ExtractNonEmptyTiles();//parcurgi acel array pentru a identifica zonele nenule
            VerifyInputTiles();//validezi că ai o zonă perfect umplută (dreptunghi fără goluri)
        }

        private void ExtractNonEmptyTiles()
        {
            for (int row = 0; row < inputTileMapBounds.size.y; row++)
            {
                for (int col = 0; col < inputTileMapBounds.size.x; col++)
                {
                    int index = col + (row * inputTileMapBounds.size.x);
                    TileBase tile = inputTilemapTilesArray[index];

                    // primul tile nenul îl marchează drept colțul din dreapta-jos
                    if (bottomRightTileCoords == null && tile != null)
                    {
                        bottomRightTileCoords = new Vector2Int(col, row);
                    }

                    if (tile != null)
                    {
                        // adaugă în coadă fiecare tile nenul cu poziția lui
                        stackOfTiles.Enqueue(new TileContainer(tile, col, row));
                        // actualizează colțul de sus-stânga cu ultima poziție nenulă
                        topLeftTileCoords = new Vector2Int(col, row);
                    }
                }
            }
        }


        private void VerifyInputTiles()
        {
            // Dacă nu am găsit niciun tile nenul, inputul e gol → arunc excepție
            if (topLeftTileCoords == null || bottomRightTileCoords == null)
            {
                throw new System.Exception("WFC: Input tilemap is empty");
            }

            // Extragem coordonatele minime și maxime din cele două colțuri
            int minX = bottomRightTileCoords.Value.x;
            int maxX = topLeftTileCoords.Value.x;
            int minY = bottomRightTileCoords.Value.y;
            int maxY = topLeftTileCoords.Value.y;

            // Calculăm lățimea și înălțimea dreptunghiului inclusiv marginile
            width = Math.Abs(maxX - minX) + 1;
            height = Math.Abs(maxY - minY) + 1;

            // Verificăm că avem exact width*height tile-uri nenule în coadă
            int tileCount = width * height;
            if (stackOfTiles.Count != tileCount)
            {
                throw new System.Exception("WFC: Tilemap has empty fields");
            }

            // Verificăm că niciun tile nu iese în afara dreptunghiului complet umplut
            if (stackOfTiles.Any(tile => tile.X > maxX || tile.X < minX || tile.Y > maxY || tile.Y < minY))
            {
                throw new System.Exception("WFC: Tilemap image should be a filled rectangle");
            }
        }

    }
}