using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using WaveFunctionCollapse;


namespace WaveFunctionCollapse
{//Desenam matricea finala in scena 
    public class TilemapOutput : IOutputCreator<Tilemap>
    {
        private Tilemap outputImage;
        private InputManager<UnityEngine.Tilemaps.TileBase> valueManager;//matricea de int (int[][]) de input, fiecare int reprezentand un Tilebase
        public Tilemap OutputImage => outputImage;

        public TilemapOutput(InputManager<UnityEngine.Tilemaps.TileBase> valueManager, Tilemap outputImage)
        {
            this.outputImage = outputImage;
            this.valueManager = valueManager;
        }

        public void CreateOutput(PatternManager manager, int[][] patternIndices, int width, int height)//patternIndices= gridul de indici pattern, returnat de WFC. Dam gridul la PatternManager ca sa il convertim la indici de Tilebase
        {
            if (patternIndices.Length == 0)
            {
                return;
            }
            this.outputImage.ClearAllTiles();//stergem ce e desenat in scena

            int[][] valueGrid;
            valueGrid = manager.ConvertPatternsToValues<UnityEngine.Tilemaps.TileBase>(patternIndices);//convertim rezultatul WFC la matrice de indexi ce reprezinta Tilebase

            int rows = valueGrid.Length;
            int cols = valueGrid[0].Length;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int valueIndex = valueGrid[row][col];//luam indicele corespunzator Tilebase-ului
                    UnityEngine.Tilemaps.TileBase tile = (UnityEngine.Tilemaps.TileBase)valueManager.GetValueFromIndex(valueIndex).value;
                    outputImage.SetTile(new Vector3Int(col, row, 0), tile);
                }
            }

        }

        public void CreatePartialOutput(PatternManager manager, OutputGrid outputGrid, UnityEngine.Tilemaps.TileBase errorTile = null, UnityEngine.Tilemaps.TileBase pendingTile = null)
        {
            outputImage.ClearAllTiles();

            for (int row = 0; row < outputGrid.height; row++)
            {
                for (int col = 0; col < outputGrid.width; col++)
                {
                    var possiblePatterns = outputGrid.GetPossibleValuesForPosition(new Vector2Int(col, row));
                    UnityEngine.Tilemaps.TileBase tileToDraw = null;

                    if (possiblePatterns.Count == 1)
                    {
                        int patternIndex = possiblePatterns.First();
                        // Pick the "anchor" value for this pattern (usually [0,0] or similar)
                        var pattern = manager.GetPatternDataFromIndex(patternIndex).Pattern;
                        int valueIndex = pattern.GetGridValue(0, 0);
                        tileToDraw = (UnityEngine.Tilemaps.TileBase)valueManager.GetValueFromIndex(valueIndex).value;
                    }
                    else if (possiblePatterns.Count == 0)
                    {
                        tileToDraw = errorTile; // Contradiction cell
                    }
                    else
                    {
                        tileToDraw = pendingTile; // Uncollapsed cell (optional)
                    }

                    if (tileToDraw != null)
                        outputImage.SetTile(new Vector3Int(col, row, 0), tileToDraw);
                    // else: leave cell empty/transparent
                }
            }
        }

    }
}