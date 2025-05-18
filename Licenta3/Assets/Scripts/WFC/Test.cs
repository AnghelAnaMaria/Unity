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
        PatternDataResults patternResults = PatternFinder.GetPatternDataFromGrid(valueManager, patternSize, equalWeights);//matricea input de patterns
        int cols = patternResults.GetGridLengthX();
        int rows = patternResults.GetGridLengthY();

        int colOffset = (patternSize < 3) ? 1 : (patternSize - 1);//ne trebuie pt a face rost de coloana 0 a ferestrelor (care intern incep de la -1 sau -(N-1))

        HashSet<int> firstColumnPatterns = new HashSet<int>();//lista cu patterns care au coltul pe prima coloana a grilei de patterns
        Debug.Log("Patterns posibile stanga: ");
        for (int py = 0; py < rows; py++)
        {
            int index = patternResults.GetIndexAt(colOffset, py);//pattern care sta pe prima coloana
            firstColumnPatterns.Add(index);

            // Găsești Pattern-ul și iei sprite-ul din colțul stânga-jos (0,0)
            var pat = patternManager.GetPatternDataFromIndex(index).Pattern;
            int valIndex = pat.GetGridValue(0, 0);
            var tb = valueManager.GetValueFromIndex(valIndex).value;
            string spriteName = (tb is Tile t)
                                ? t.sprite.name
                                : tb.name;

            Debug.Log($"index posibil stânga: {index}, sprite = {spriteName}");
        }
        Debug.Log("---------------------");

        // 3) Initialize the WFC core
        var restrictions = new Dictionary<Vector2Int, HashSet<int>>();
        for (int py = 0; py < outputHeight; py++)
            restrictions[new Vector2Int(0, py)] = firstColumnPatterns;

        core = new WFCCore(outputWidth, outputHeight, maxIteration, patternManager, restrictions);

        for (int py = 0; py < outputHeight; py++)//prima coloana din grila finala de patterns
        {
            core.OutputGrid.RestrictPossibleValuesAt(0, py, firstColumnPatterns);
        }

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

        output.CreateOutput(patternManager, result, outputWidth, outputHeight);
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