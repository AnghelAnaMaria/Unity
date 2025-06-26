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

    //Pt Tilemap rezultat extins:
    public int chunkSize = 6;
    public int overlap = 2;
    public int gridWidth = 24;
    public int gridHeight = 24;

    //InputManager<UnityEngine.Tilemaps.TileBase> inputManager;//grila de int[][]
    public InputManager<UnityEngine.Tilemaps.TileBase> inputManager { get; private set; }
    public GameObject animatedTilePrefab;
    WFC core;//colapsam patterns
    PatternManager patternManager;//lucram cu patterns si grila de patterns
    TilemapOutput output;//desenam matricea finala (cu Tilebase)
    private HashSet<int> leftPatterns = new HashSet<int>();
    private HashSet<int> rightPatterns = new HashSet<int>();
    private HashSet<int> downPatterns = new HashSet<int>();
    private HashSet<int> upPatterns = new HashSet<int>();
    private HashSet<int> middlePatterns = new HashSet<int>();
    private Dictionary<Vector2Int, HashSet<int>> restrictions = new Dictionary<Vector2Int, HashSet<int>>();//restrictiile pt anumite celule din output
    private Dictionary<Vector2Int, HashSet<int>> softBanned = new Dictionary<Vector2Int, HashSet<int>>();//dictionar (pozitie, ce patterns nu ne dorim)
    private Dictionary<Vector2Int, int[][]> allChunkResults = new Dictionary<Vector2Int, int[][]>();

    public static Final Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }


    void Start()
    {
        CreateWFC();
        CreateLargeTilemap();

        // CreateTilemap();

    }
    // private IEnumerator Start()//corutina
    // {

    //     CreateWFC();
    //     int[][] result = core.CreateOutputGrid();
    //     output = new TilemapOutput(inputManager, outputTilemap);

    //     var ordered = core.CollapseOrder;

    //     if (result.Length == 0)
    //     {
    //         output.CreatePartialOutput(patternManager, core.OutputGrid, errorTile: null, pendingTile: null);
    //         yield break;
    //     }
    //     else
    //     {
    //         //Call animation
    //         yield return StartCoroutine(
    //             output.AnimateOrderedOutput(
    //                 ordered,      // lista de poziţii în ordinea colapsării
    //                 result,                  // grila finală int[][] de pattern-uri
    //                 patternManager,          // ca să convertim pattern → value
    //                 inputManager,            // ca să convertim value → TileBase
    //                 animatedTilePrefab,      // prefab‐ul cu AnimatedTileFall + SpriteRenderer
    //                 0.05f                    // întârzierea între două căderi
    //             )
    //         );
    //     }
    // }

    public void CreateWFC()
    {
        if (inputTilemap == null)
        {
            Debug.LogError("InputTilemap not assigned in the Inspector.");
            return;
        }

        // 1) Read the input Tilemap into a grid of IValue<TileBase>
        var reader = new WaveFunctionCollapse.Input(inputTilemap);//avem matricea din scena
        var grid = reader.ReadInputToGrid();// grid=IVal<TileBase>[][]
        inputManager = new InputManager<UnityEngine.Tilemaps.TileBase>(grid);//int[][] grid de indici de Tiles


        // 2) Extract patterns and their neighbours
        patternManager = new PatternManager(patternSize);
        var strategy = patternManager.ProcessStrategy();
        patternManager.ProcessGrid(inputManager, equalWeights, strategy);
        // DebugPrintAllPatterns();

        ApplyRestrictions();
        // 3) Initialize the WFC core 
        core = new WFC(outputWidth, outputHeight, maxIteration, patternManager, stepsBack, middlePatterns, softBanned, restrictions);

    }

    public void ApplyRestrictions()
    {
        // 4) Apply per-column constraints on the pattern-grid
        int N = patternSize;

        foreach (int pid in patternManager.GetAllPatternIndices())
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



        var allPatterns = Enumerable.Range(0, patternManager.GetNumberOfPatterns()).ToHashSet();
        for (int px = (int)outputWidth / 3; px < (int)2 * outputWidth / 3; px++)
            for (int py = (int)outputHeight / 3; py < (int)2 * outputHeight / 3; py++)
            {
                restrictions[new Vector2Int(px, py)] = allPatterns.Except(middlePatterns).ToHashSet();
            }
        // // for (int py = 0; py < outputHeight; py++)
        // //     restrictions[new Vector2Int(0, py)] = leftPatterns;//restrictii pt coloana stanga
        // // for (int py = 0; py < outputHeight; py++)
        // //     restrictions[new Vector2Int(outputWidth - 1, py)] = rightPatterns;//restrictii pt coloana dreapta
        // // core = new WFCCore(outputWidth, outputHeight, maxIteration, patternManager, restrictions);

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
            //StartCoroutine(output.AnimateOutput(patternManager, result, outputWidth, outputHeight, animatedTilePrefab));
        }
    }

    public void CreateLargeTilemap()
    {
        int[][] finalGrid = new int[gridHeight][];
        for (int y = 0; y < gridHeight; y++)
            finalGrid[y] = new int[gridWidth]; //jagged array-ul final/rezultatul, in care salvam chunk-urile

        bool hasFailedChunk = false;

        for (int chunkX = 0; chunkX < gridWidth; chunkX += (chunkSize - patternSize))//asigură că fiecare chunk începe la poziția potrivită, astfel încât între două chunkuri consecutive să existe overlap exact de N (unde N = patternSize)
        {
            for (int chunkY = 0; chunkY < gridHeight; chunkY += (chunkSize - patternSize))
            {
                // 1. Calculez dimensiunea chunkului real:
                int actualChunkWidth = Mathf.Min(chunkSize, gridWidth - chunkX);
                int actualChunkHeight = Mathf.Min(chunkSize, gridHeight - chunkY);

                // 1) Construieşti restricţiile iniţiale:
                var initialRestrictions = new Dictionary<Vector2Int, HashSet<int>>();
                var softBanned = new Dictionary<Vector2Int, HashSet<int>>();

                if (chunkX == 0 && chunkY > 0)//pt prima coloana
                {
                    // Găsește chunkul de sub chunkul actual:
                    Vector2Int belowChunkPos = new Vector2Int(chunkX, chunkY - (chunkSize - patternSize));//pozitia chunkului de sub
                    int[][] belowChunkResult = allChunkResults[belowChunkPos];//iau din dictionar chunkul de sub

                    // extrag PID-urile de pe ultimul rând al chunk-ului de sub
                    for (int localX = 0; localX < actualChunkWidth; localX++)
                    {
                        int pidBelow = belowChunkResult[actualChunkHeight - 1][localX];

                        // CHEIA este coordonata LOCALĂ în chunk: (localX, 0)
                        initialRestrictions[new Vector2Int(localX, 0)] = new HashSet<int> { pidBelow };

                    }

                }

                // Rulez WFC pe chunkul curent:
                var wfcChunk = new WFC(actualChunkWidth, actualChunkHeight, maxIteration,
                                       patternManager, stepsBack, middlePatterns,
                                       softBanned, initialRestrictions);

                int[][] chunkResult = wfcChunk.CreateOutputGrid();//generez chunk-ul
                allChunkResults[new Vector2Int(chunkX, chunkY)] = chunkResult;//salvez chunk-ul in dictionar

                if (chunkResult.Length == 0)
                {
                    hasFailedChunk = true;
                    continue;
                }


                // Copiez rezultatul în gridul final
                for (int localY = 0; localY < actualChunkHeight; localY++)
                {
                    for (int localX = 0; localX < actualChunkWidth; localX++)
                    {
                        finalGrid[chunkY + localY][chunkX + localX] = chunkResult[localY][localX];
                    }
                }
            }
        }

        output = new TilemapOutput(inputManager, outputTilemap);

        if (hasFailedChunk)
        {
            // Folosește metoda de partial draw pentru a desena tot ce s-a putut genera:
            output.CreatePartialOutput(patternManager, core != null ? core.OutputGrid : null, errorTile: null, pendingTile: null);
            // SAU, dacă vrei să desenezi direct din `finalGrid`, poți scrie o metodă de tipul:
            // output.CreatePartialOutputFromGrid(patternManager, finalGrid, gridWidth, gridHeight, errorTile: null, pendingTile: null);
        }
        else
        {
            // Harta este complet generată:
            output.CreateOutput(patternManager, finalGrid, gridWidth, gridHeight);
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