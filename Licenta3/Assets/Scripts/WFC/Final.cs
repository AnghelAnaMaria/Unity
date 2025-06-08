#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WaveFunctionCollapse;
using System.Linq;


public class Final : MonoBehaviour
{
    [Header("References")]
    //public Tilemap inputTilemap;
    public Tilemap inputTilemap;
    public Tilemap outputTilemap;

    [Header("WFC Settings")]
    public int patternSize = 2;
    public int maxIteration = 500;
    public int outputWidth = 5;
    public int outputHeight = 5;
    public bool equalWeights = false;
    public string strategyName;
    public int stepsBack = 5;
    InputManager<UnityEngine.Tilemaps.TileBase> inputManager;//grila de int[][]
    WFC core;//colapsam patterns
    PatternManager patternManager;//lucram cu patterns si grila de patterns
    TilemapOutput output;//desenam matricea finala (cu Tilebase)

    void Start()
    {
        CreateWFC();
        CreateTilemap();

    }

    public void CreateWFC()
    {
        if (inputTilemap == null)
        {
            Debug.LogError("InputTilemap not assigned in the Inspector.");
            return;
        }

        // 1) Read the input Tilemap into a grid of IValue<TileBase>
        var reader = new WaveFunctionCollapse.Input(inputTilemap);
        var grid = reader.ReadInputToGrid();
        inputManager = new InputManager<UnityEngine.Tilemaps.TileBase>(grid);//int[][] grid de indici de Tiles


        // 2) Extract patterns and their neighbours
        patternManager = new PatternManager(patternSize);
        var strategy = patternManager.ProcessStrategy();
        patternManager.ProcessGrid(inputManager, equalWeights, strategy);
        // DebugPrintAllPatterns();

        // 4) Apply per-column constraints on the pattern-grid
        int N = patternSize;
        // Seturi în care salvez indexii ceruți
        var leftPatterns = new HashSet<int>();
        var rightPatterns = new HashSet<int>();
        var downPatterns = new HashSet<int>();
        var upPatterns = new HashSet<int>();
        var middlePatterns = new HashSet<int>();

        foreach (int pid in patternManager.GetAllPatternIndices())//MA MAI UIT AICI!!!!
        {
            var patternData = patternManager.GetPatternDataFromIndex(pid);
            var pattern = patternData.Pattern;
            // Jos-stanga (0,0)
            int idxJosStanga = pattern.GetGridValue(0, 0);
            var tileJosStanga = inputManager.GetValueFromIndex(idxJosStanga).value;
            // var tileJosStanga = FindTileByIndex(idxJosStanga, allValueManagers);
            string nameJosStanga = (tileJosStanga is Tile t1) ? t1.sprite.name : tileJosStanga.name;
            if (nameJosStanga == "Grass" || nameJosStanga == "Collider" || nameJosStanga == "ColliderWithRightWall")
                leftPatterns.Add(pid);
            if (nameJosStanga == "Grass" || nameJosStanga == "Collider")
                middlePatterns.Add(pid);

            // Jos-dreapta (N-1,0)
            int idxJosDreapta = pattern.GetGridValue(N - 1, 0);
            var tileJosDreapta = inputManager.GetValueFromIndex(idxJosDreapta).value;
            // var tileJosDreapta = FindTileByIndex(idxJosDreapta, allValueManagers);
            string nameJosDreapta = (tileJosDreapta is Tile t2) ? t2.sprite.name : tileJosDreapta.name;
            if (nameJosDreapta == "Grass" || nameJosDreapta == "Collider" || nameJosDreapta == " ColliderWithLeftWall")
                rightPatterns.Add(pid);

            // Jos-stanga (0,0)
            int idxSusDreapta = pattern.GetGridValue(0, 0);
            var tileSusDreapta = inputManager.GetValueFromIndex(idxSusDreapta).value;
            //var tileSusDreapta = FindTileByIndex(idxSusDreapta, allValueManagers);
            string nameSusDreapta = (tileSusDreapta is Tile t3) ? t3.sprite.name : tileSusDreapta.name;
            if (nameSusDreapta == "Grass" || nameSusDreapta == "Collider" || nameSusDreapta == "ColliderWithUpWall")
                downPatterns.Add(pid);

            // Sus-stanga (0,N-1)
            int idxSusStanga = pattern.GetGridValue(0, N - 1);
            var tileSusStanga = inputManager.GetValueFromIndex(idxSusStanga).value;
            // var tileSusStanga = FindTileByIndex(idxSusStanga, allValueManagers);
            string nameSusStanga = (tileSusStanga is Tile t4) ? t4.sprite.name : tileSusStanga.name;
            if (nameSusStanga == "Grass" || nameSusStanga == "Collider" || nameSusStanga == "ColliderWithDownWall")
                upPatterns.Add(pid);
        }

        // Poți afișa sau folosi mai departe aceste seturi!
        // Debug.Log($"leftPatterns index : {string.Join(",", leftPatterns)}");
        // Debug.Log($"rightPatterns index : {string.Join(",", rightPatterns)}");
        // Debug.Log($"downPatterns index : {string.Join(",", downPatterns)}");
        // Debug.Log($"upPatterns index : {string.Join(",", upPatterns)}");


        // //Left input patterns:
        // int leftColOffset = (patternSize < 3) ? 1 : (patternSize - 1);//ne trebuie pt a face rost de coloana 0 a ferestrelor (care intern incep de la -1 sau -(N-1))
        // HashSet<int> leftColumnPatterns = new HashSet<int>();//lista cu patterns/ferestre care stau pe prima coloana fara sa iasa din int[][] grila de indexi de Tiles
        // for (int py = 0; py < rows; py++)
        // {
        //     int index = patternResults.GetIndexAt(leftColOffset, py);//pattern care sta pe prima coloana
        //     leftColumnPatterns.Add(index);

        //     // //pt fiecare pattern iau sprite-ul din colțul stânga-jos (0,0) ca sa ne convingem ca avem pattern de margine stanga din input 
        //     // var pat = patternManager.GetPatternDataFromIndex(index).Pattern;
        //     // int valIndex = pat.GetGridValue(0, 0);
        //     // var tb = valueManager.GetValueFromIndex(valIndex).value;
        //     // string spriteName = (tb is Tile t)
        //     //                     ? t.sprite.name
        //     //                     : tb.name;

        //     // Debug.Log($"index posibil stânga: {index}, sprite = {spriteName}");
        // }

        // //Right input patterns:
        // int rightColOffset = cols - 1 - (patternSize - 1);
        // HashSet<int> rightColumnPatterns = new HashSet<int>();//lista cu patterns/ferestre care stau pe ultima coloana fara sa iasa din int[][] grila de indexi de Tiles
        // for (int py = 0; py < rows; py++)
        // {
        //     int index = patternResults.GetIndexAt(rightColOffset, py);//pattern care sta pe ultima coloana (fereastra nu iese din gridul de int care reprezinta Tiles)
        //     rightColumnPatterns.Add(index);

        //     //pt fiecare pattern iau sprite-ul din colțul dreapta-jos (N-1,0) ca sa ne convingem ca avem pattern de margine dreapta din input 
        //     var pat = patternManager.GetPatternDataFromIndex(index).Pattern;
        //     int valIndex = pat.GetGridValue(patternSize - 1, 0);
        //     var tb = valueManager.GetValueFromIndex(valIndex).value;
        //     string spriteName = (tb is Tile t)
        //                         ? t.sprite.name
        //                         : tb.name;

        //     Debug.Log($"index posibil dreapta: {index}, sprite = {spriteName}");
        // }


        // 3) Initialize the WFC core 
        var restrictions = new Dictionary<Vector2Int, HashSet<int>>();//restrictiile pt anumite celule din output
        var allPatterns = Enumerable.Range(0, patternManager.GetNumberOfPatterns()).ToHashSet();
        for (int px = (int)outputWidth / 4; px < (int)3 * outputWidth / 4; px++)
            for (int py = (int)outputHeight / 4; py < (int)3 * outputHeight / 4; py++)
            {
                restrictions[new Vector2Int(px, py)] = allPatterns.Except(middlePatterns).ToHashSet();
            }
        // // for (int py = 0; py < outputHeight; py++)
        // //     restrictions[new Vector2Int(0, py)] = leftPatterns;//restrictii pt coloana stanga
        // // for (int py = 0; py < outputHeight; py++)
        // //     restrictions[new Vector2Int(outputWidth - 1, py)] = rightPatterns;//restrictii pt coloana dreapta
        // // core = new WFCCore(outputWidth, outputHeight, maxIteration, patternManager, restrictions);

        var softBanned = new Dictionary<Vector2Int, HashSet<int>>();//dictionar (pozitie, ce patterns nu ne dorim)
        // for (int py = 0; py < outputHeight; py++)//pe marginea stângă
        //     softBanned[new Vector2Int(0, py)] = allPatterns.Except(leftPatterns).ToHashSet();
        // for (int py = 0; py < outputHeight; py++)//pe marginea dreaptă
        //     softBanned[new Vector2Int(outputWidth - 1, py)] = allPatterns.Except(rightPatterns).ToHashSet();
        // for (int px = 0; px < outputWidth; px++)//jos
        //     softBanned[new Vector2Int(px, 0)] = allPatterns.Except(downPatterns).ToHashSet();
        // for (int px = 0; px < outputWidth; px++)//sus
        //     softBanned[new Vector2Int(px, outputHeight - 1)] = allPatterns.Except(upPatterns).ToHashSet();
        // // for (int px = outputWidth / 3; px < 2 * outputWidth / 3; px++)
        // //     for (int py = outputHeight / 3; py < 2 * outputHeight / 3; py++)
        // //     {
        // //         softBanned[new Vector2Int(px, py)] = middlePatterns.ToHashSet();
        // //     }
        core = new WFC(outputWidth, outputHeight, maxIteration, patternManager, stepsBack, softBanned, restrictions);

    }

    void DebugPrintAllPatterns()
    {
        foreach (int pid in patternManager.GetAllPatternIndices())
        {
            var pd = patternManager.GetPatternDataFromIndex(pid);
            var pat = pd.Pattern;
            Debug.Log($"Pattern {pid}:");
            for (int y = 0; y < patternSize; y++)
            {
                string line = "";
                for (int x = 0; x < patternSize; x++)
                {
                    int v = pat.GetGridValue(x, y);
                    var tb = inputManager.GetValueFromIndex(v).value;
                    string name = (tb is Tile t) ? t.sprite.name : tb.name;
                    line += name.PadRight(12);
                }
                Debug.Log(line);
            }
            Debug.Log("---");
        }
    }

    public void CreateTilemap()
    {
#if UNITY_EDITOR
        Debug.ClearDeveloperConsole();
#endif

        output = new TilemapOutput(inputManager, outputTilemap);
        int[][] result = core.CreateOutputGrid();//colapsam patterns si avem rezultatul result

        // —— debug: ce pattern-uri au ajuns pe coloana 0 în output? ——
        // Debug.Log("=== Patterns rezultate stanga ===");
        // for (int row = 0; row < result.Length; row++)
        // {
        //     int ind = result[row][0];//indexul pattern-ului
        //     var pattern = patternManager.GetPatternDataFromIndex(ind).Pattern;//pattern
        //     int valIndex = pattern.GetGridValue(0, 0);//coltul stanga-jos din pattern
        //     var tb = valueManager.GetValueFromIndex(valIndex).value;//IValue<Tilebase>
        //     string spriteName = (tb is Tile t)
        //         ? t.sprite.name
        //         : tb.name;//nume sprite

        //     Debug.Log($"row {row}: pattern {ind}, sprite={spriteName}");
        // }
        // Debug.Log("=======================================");

        if (result.Length == 0)
        {
            // WFC failed, draw what was achieved so far:
            output.CreatePartialOutput(patternManager, core.OutputGrid, errorTile: null, pendingTile: null);
        }
        else
        {
            // Success, draw the full result
            output.CreateOutput(patternManager, result, outputWidth, outputHeight);
        }
    }

    public void DrawPartialOutput()
    {
        if (core != null && patternManager != null && output != null)
        {
            output.CreatePartialOutput(patternManager, core.OutputGrid, errorTile: null, pendingTile: null);
        }
    }

    public void SaveTilemap()
    {
        if (output.OutputImage != null)
        {
            outputTilemap = output.OutputImage;
            GameObject objectToSave = outputTilemap.gameObject;

            PrefabUtility.SaveAsPrefabAsset(objectToSave, "Assets/Prefabs/output.prefab");
        }
    }
}