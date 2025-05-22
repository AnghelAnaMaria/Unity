using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using WaveFunctionCollapse;
using UnityEditor;
using System.Linq;


public class Test : MonoBehaviour
{
    [Header("References")]
    public Tilemap inputTilemap;
    public Tilemap outputTilemap;

    [Header("WFC Settings")]
    public int patternSize = 2;
    public int maxIteration = 500;
    public int outputWidth = 5;
    public int outputHeight = 5;
    public bool equalWeights = false;
    public string strategyName;
    ValuesManager<TileBase> valueManager;//grila initiala de indici, unde fiecare index reprezinta un Tilebase
    WFCCore core;//colapsam patterns
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
            Debug.LogError("InputTilemap is not assigned in the Inspector.");
            return;
        }

        // 1) Read the input Tilemap into a grid of IValue<TileBase>
        var reader = new InputReader(inputTilemap);
        var grid = reader.ReadInputToGrid();
        valueManager = new ValuesManager<TileBase>(grid);//int[][] grid de indici de Tiles

        var size = valueManager.GetGridSize();
        Debug.Log($"Input grid: {size.x}×{size.y}, patternSize: {patternSize}");

        // 2) Extract patterns and their neighbours
        patternManager = new PatternManager(patternSize);
        patternManager.ProcessGrid(valueManager, equalWeights, strategyName);
        // DebugPrintAllPatterns();

        // 4) Apply per-column constraints on the pattern-grid
        PatternDataResults patternResults = PatternFinder.GetPatternDataFromGrid(valueManager, patternSize, equalWeights);//matricea input de patterns (adica matricea cu toate ferestrele, inclusiv cele care ies in afara grilei de indexi de Tiles)
        // int cols = patternResults.GetGridLengthX();
        // int rows = patternResults.GetGridLengthY();


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
        // var restrictions = new Dictionary<Vector2Int, HashSet<int>>();//restrictiile pt anumite celule din output
        // for (int py = 0; py < outputHeight; py++)
        //     restrictions[new Vector2Int(0, py)] = leftColumnPatterns;//restrictii pt coloana stanga
        // for (int py = 0; py < outputHeight; py++)
        //     restrictions[new Vector2Int(outputWidth - 1, py)] = rightColumnPatterns;//restrictii pt coloana dreapta

        // core = new WFCCore(outputWidth, outputHeight, maxIteration, patternManager, restrictions);

        var allPatterns = Enumerable.Range(0, patternManager.GetNumberOfPatterns()).ToHashSet();
        var softBanned = new Dictionary<Vector2Int, HashSet<int>>();//dictionar (pozitie, ce patterns nu ne dorim)

        // for (int py = 0; py < outputHeight; py++)//pe marginea stângă
        //     softBanned[new Vector2Int(0, py)] = allPatterns.Except(leftColumnPatterns).ToHashSet();
        // for (int py = 0; py < outputHeight; py++) //pe marginea dreaptă
        //     softBanned[new Vector2Int(outputWidth - 1, py)] = allPatterns.Except(rightColumnPatterns).ToHashSet();

        core = new WFCCore(outputWidth, outputHeight, maxIteration, patternManager, softBanned);

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
                    var tb = valueManager.GetValueFromIndex(v).value;
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
        output = new TilemapOutput(valueManager, outputTilemap);
        int[][] result = core.CreateOutputGrid();//colapsam patterns si avem rezultatul result

        // —— debug: ce pattern-uri au ajuns pe coloana 0 în output? ——
        Debug.Log("=== Patterns rezultate stanga ===");
        for (int row = 0; row < result.Length; row++)
        {
            int ind = result[row][0];//indexul pattern-ului
            var pattern = patternManager.GetPatternDataFromIndex(ind).Pattern;//pattern
            int valIndex = pattern.GetGridValue(0, 0);//coltul stanga-jos din pattern
            var tb = valueManager.GetValueFromIndex(valIndex).value;//IValue<Tilebase>
            string spriteName = (tb is Tile t)
                ? t.sprite.name
                : tb.name;//nume sprite

            Debug.Log($"row {row}: pattern {ind}, sprite={spriteName}");
        }
        Debug.Log("=======================================");

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