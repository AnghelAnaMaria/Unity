using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WaveFunctionCollapse;


namespace WaveFunctionCollaps
{
    public class TilemapOutput : IOutputCreator<Tilemap>
    {
        private Tilemap outputImage;
        private ValuesManager<TileBase> valueManager;
        public Tilemap OutputImage => outputImage;

        public TilemapOutput(ValuesManager<TileBase> valueManager, Tilemap outputImage)
        {
            this.outputImage = outputImage;
            this.valueManager = valueManager;
        }

        public void CreateOutput(PatternManager manager, int[][] outputValues, int width, int height)
        {
            if (outputValues.Length == 0)
            {
                return;
            }
            this.outputImage.ClearAllTiles();

            int[][] valueGrid;
            valueGrid = manager.ConvertPatternToValues<TileBase>(outputValues);

            // use the real dimensions of valueGrid:
            int rows = valueGrid.Length;
            int cols = valueGrid[0].Length;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var valueIndex = valueGrid[row][col];
                    TileBase tile = (TileBase)valueManager.GetValueFromIndex(valueIndex).value;
                    outputImage.SetTile(new Vector3Int(col, row, 0), tile);
                }
            }

        }
    }
}

