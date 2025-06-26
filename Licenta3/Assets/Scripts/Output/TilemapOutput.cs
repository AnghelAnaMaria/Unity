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
        private InputManager<UnityEngine.Tilemaps.TileBase> inputManager;//matricea de int (int[][]) de input, fiecare int reprezentand un Tilebase

        //Getter:
        public Tilemap OutputImage => outputImage;

        public TilemapOutput(InputManager<UnityEngine.Tilemaps.TileBase> valueManager, Tilemap outputImage)
        {
            this.outputImage = outputImage;
            this.inputManager = valueManager;
        }

        public void CreateOutput(PatternManager manager, int[][] patternIndices, int width, int height)// int[][] patternIndices= gridul de Patterns 
        {
            if (patternIndices.Length == 0)
            {
                return;
            }
            this.outputImage.ClearAllTiles();//stergem ce e desenat in scena

            int[][] valueGrid = manager.ConvertPatternsToValues<UnityEngine.Tilemaps.TileBase>(patternIndices);//convertim rezultatul WFC la int[][] ce reprezinta tiles

            int rows = valueGrid.Length;
            int cols = valueGrid[0].Length;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int valueIndex = valueGrid[row][col];//luam indicele corespunzator Tilebase-ului
                    UnityEngine.Tilemaps.TileBase tile = (UnityEngine.Tilemaps.TileBase)inputManager.GetValueFromIndex(valueIndex).value;
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
                        tileToDraw = (UnityEngine.Tilemaps.TileBase)inputManager.GetValueFromIndex(valueIndex).value;
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

        public IEnumerator AnimateOrderedOutput(List<Vector2Int> collapseOrder, int[][] patternIndices, PatternManager manager, InputManager<TileBase> valueManager, GameObject animatedTilePrefab, float delayBetween = 0.05f)
        {
            outputImage.ClearAllTiles();// stergem ce e desenat
            int[][] valueGrid = manager.ConvertPatternsToValues<UnityEngine.Tilemaps.TileBase>(patternIndices);//convertim rezultatul WFC la int[][] ce reprezinta tiles

            // 2) Creăm o listă unică de poziții, păstrând ordinea primei colapsări
            var seen = new HashSet<Vector2Int>();
            var uniqueOrder = new List<Vector2Int>();
            foreach (var pos in collapseOrder)
            {
                if (seen.Add(pos))
                    uniqueOrder.Add(pos);
            }

            foreach (var pos in uniqueOrder)
            {

                int valueIndex = valueGrid[pos.y][pos.x];
                var tb = (TileBase)valueManager.GetValueFromIndex(valueIndex).value;
                if (tb == null)
                    continue;

                Vector3Int cell = new Vector3Int(pos.x, pos.y, 0);//coltul celulei de start animatie
                Vector3 targetWorld = outputImage.CellToWorld(cell) + new Vector3(0.5f, 0.5f, 0);//centrul celulei de start animatie

                // instanțiem prefab-ul de cădere
                var go = GameObject.Instantiate(animatedTilePrefab, targetWorld + Vector3.up * 2f, Quaternion.identity);

                // setăm sprite-ul corect
                go.GetComponent<SpriteRenderer>().sprite = (tb as Tile)?.sprite;

                // configurăm AnimatedTileFall
                var fall = go.GetComponent<AnimatedTileFall>();//componenta care știe să animeze o cădere
                fall.targetPosition = targetWorld;
                fall.onArrived = () =>
                {
                    outputImage.SetTile(cell, tb);// abia la sfârșitul animației plasăm Tile-ul
                };

                // Pauză între tile-uri
                yield return new WaitForSeconds(delayBetween);
            }
        }

    }
}