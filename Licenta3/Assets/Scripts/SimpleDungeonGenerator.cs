using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;

public class SimpleDungeonGenerator : MonoBehaviour
{

    [SerializeField] private Tilemap roomMap, colliderMap, leftWalls, rightWalls, upWalls, downWalls, rightBoundary, upBoundary;

    [SerializeField] private TileBase floorTile, colliderTile, leftWall, rightWall, upWall, downWall, rightBoundaryWall, upBoundarytWall;

    [SerializeField] private InputActionReference generate;

    private bool CanEdit = false;

    [SerializeField] private UnityEvent OnFinishedRoomGenerator;

    //private DungeonData dungeonData;
    [SerializeField] private DungeonData dungeonData;

    [SerializeField] private ApartmentConfig apartmentConfig;

    private void Awake()
    {
        dungeonData = FindAnyObjectByType<DungeonData>();
        if (dungeonData == null)
        {
            dungeonData = gameObject.AddComponent<DungeonData>();
        }

        if (apartmentConfig == null)
        {
            Debug.LogError("ApartmentConfig is missing! Please assign it in the Inspector.");
        }

        if (generate == null || generate.action == null)
        {
            Debug.LogError("InputActionReference 'generate' is missing or not set up correctly!");
            return;
        }

        generate.action.started += Generate;
        generate.action.Enable();

    }

    private void Generate(InputAction.CallbackContext obj)
    {
        if (dungeonData == null)
        {
            Debug.LogError("DungeonData is missing! Cannot generate dungeon.");
            return;
        }
        dungeonData.ClearAll();
        GenerateApartment();

        if (dungeonData != null)
        {
            dungeonData.GenerateDungeonTiles();
            dungeonData.GenerateDungeonCollider();
            CreateDungeonCollider(); //pt Tilemap
            GenerateWalls();
            GenerateRoomBoundaryWalls();
        }

        AdjustCameraToDungeon();
        OnFinishedRoomGenerator?.Invoke();

        //Permitem editarea manuală (in joc, dupa ce rulez si apas pe tasta "G")
        CanEdit = true;
    }

    public void OnDungeonGenerationComplete()
    {
        Debug.Log("Dungeon generation complete!");
        // Add additional behavior here, such as enabling UI elements, triggering animations, etc.
    }

    private void GenerateApartment()
    {
        if (apartmentConfig == null)
        {
            Debug.LogError("ApartmentConfig nu a fost atribuit!");
            return;
        }

        Dictionary<RoomType, Room> localGeneratedRooms = new Dictionary<RoomType, Room>();
        foreach (var item in apartmentConfig.GetRooms())//facem obiecte Room, fara a spune care le sunt vecinii
        {
            Room newRoom = new Room(Vector2.zero, item.GetRoomType(), item.GetRoomDimensions());
            localGeneratedRooms.Add(item.GetRoomType(), newRoom);
        }

        CalculateRoomCenters(localGeneratedRooms);//setam centrele camerelor

        foreach (var item in localGeneratedRooms)
        {
            Room currentRoom = item.Value;
            Room roomCreated = CreateRectangularRoomAt(currentRoom.RoomCenterPos(), currentRoom.RoomType(), currentRoom.Dimensions());
            dungeonData.AddRoom(roomCreated);
        }
    }
    private void CalculateRoomCenters(Dictionary<RoomType, Room> generatedRooms)
    {
        List<Room> roomsList = new List<Room>(generatedRooms.Values);
        Shuffle(roomsList);

        if (roomsList.Count > 0)
        {
            Room firstRoom = roomsList[0];
            firstRoom.SetRoomCenter(Vector2.zero);
        }

        // 3. Plasăm restul camerelor lângă camerele deja plasate
        for (int i = 1; i < roomsList.Count; i++)
        {
            Room currentRoom = roomsList[i];

            // Căutăm o cameră vecină și o plasăm lângă ea
            Room neighboringRoom = roomsList[UnityEngine.Random.Range(0, i)]; // Alege aleatoriu o cameră deja plasată
            Vector2 newRoomCenter = GetNeighborPosition(neighboringRoom, currentRoom);
            currentRoom.SetRoomCenter(newRoomCenter);
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private Vector2 GetNeighborPosition(Room neighborRoom, Room currentRoom)
    {
        // Determinăm o poziție validă pentru camera curentă lângă camera vecină
        Vector2 neighborCenter = neighborRoom.RoomCenterPos();
        Vector2 currentDimensions = currentRoom.Dimensions();
        Vector2 neighborDimensions = neighborRoom.Dimensions();

        // Alegem o direcție aleatorie pentru a plasa camera
        int direction = UnityEngine.Random.Range(0, 4); // 0 = sus, 1 = jos, 2 = stânga, 3 = dreapta

        switch (direction)
        {
            case 0: // Sus
                return new Vector2(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + currentDimensions.y / 2);
            case 1: // Jos
                return new Vector2(neighborCenter.x, neighborCenter.y - neighborDimensions.y / 2 - currentDimensions.y / 2);
            case 2: // Stânga
                return new Vector2(neighborCenter.x - neighborDimensions.x / 2 - currentDimensions.x / 2, neighborCenter.y);
            case 3: // Dreapta
                return new Vector2(neighborCenter.x + neighborDimensions.x / 2 + currentDimensions.x / 2, neighborCenter.y);
            default:
                return Vector2.zero;
        }
    }

    private Room CreateRectangularRoomAt(Vector2 roomCenterPosition, RoomType roomType, Vector2 roomSize)
    {
        Vector2 half = roomSize / 2;
        Room room = new Room(roomCenterPosition, roomType, roomSize);

        for (var x = -half.x; x < half.x; x++)
        {
            for (var y = -half.y; y < half.y; y++)
            {
                Vector2 position = roomCenterPosition + new Vector2(x, y);
                Vector3Int mapPosition = roomMap.WorldToCell(position);
                room.AddFloorTiles((Vector2Int)mapPosition);
                roomMap.SetTile(mapPosition, floorTile);
            }
        }
        return room;
    }

    private void CreateDungeonCollider()
    {
        if (dungeonData == null)
        {
            Debug.LogError("DungeonData is missing! Cannot create dungeon collider.");
            return;
        }
        colliderMap.ClearAllTiles();//Pt a evita duplicarea
        foreach (Vector2Int pos in dungeonData.ColliderTiles)
        {
            colliderMap.SetTile((Vector3Int)pos, colliderTile);
        }
    }

    private void GenerateWalls()//Also for update walls
    {
        if (dungeonData == null)
        {
            Debug.LogError("DungeonData is missing! Cannot generate walls.");
            return;
        }
        leftWalls.ClearAllTiles();
        rightWalls.ClearAllTiles();
        downWalls.ClearAllTiles();
        upWalls.ClearAllTiles();

        foreach (Vector2Int posRoom in dungeonData.DungeonTiles)
        {
            Vector2Int leftWallTile = posRoom + Vector2Int.left;//left wall all tiles
            if (dungeonData.VerifyDungeonTile(leftWallTile) == false)
            {
                dungeonData.AddDungeonLeftWallTiles(leftWallTile);
                leftWalls.SetTile((Vector3Int)leftWallTile, leftWall);

            }

            Vector2Int rightWallTile = posRoom + Vector2Int.right;//right wall all tiles
            if (dungeonData.VerifyDungeonTile(rightWallTile) == false)
            {
                dungeonData.AddDungeonRightWallTiles(rightWallTile);
                rightWalls.SetTile((Vector3Int)rightWallTile, rightWall);

            }

            Vector2Int upWallTile = posRoom + Vector2Int.up;//up wall all tiles
            if (dungeonData.VerifyDungeonTile(upWallTile) == false)
            {
                dungeonData.AddDungeonUpWallTiles(upWallTile);
                upWalls.SetTile((Vector3Int)upWallTile, upWall);

            }

            Vector2Int downWallTile = posRoom + Vector2Int.down;//down wall all tiles
            if (dungeonData.VerifyDungeonTile(downWallTile) == false)
            {
                dungeonData.AddDungeonDownWallTiles(downWallTile);
                downWalls.SetTile((Vector3Int)downWallTile, downWall);

            }
        }
    }

    private void GenerateRoomBoundaryWalls()
    {
        rightBoundary.ClearAllTiles();
        upBoundary.ClearAllTiles();

        foreach (Room room in dungeonData.Rooms)//pt fiecare camera adaug perete despartitor sus si la dreapta => avem camerele despartite in dungeon/ apartament.
        {
            //limitele camerei
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (Vector2Int tile in room.FloorTiles())
            {
                if (tile.x < minX) minX = tile.x;
                if (tile.x > maxX) maxX = tile.x;
                if (tile.y < minY) minY = tile.y;
                if (tile.y > maxY) maxY = tile.y;
            }

            //Punem peretii despartitori
            for (int y = minY; y <= maxY; y++)
            {
                //dreapta
                Vector3Int rightWallPosition = new Vector3Int(maxX + 1, y, 0);
                dungeonData.AddRightBoundaryWalls((Vector2Int)rightWallPosition);
                rightBoundary.SetTile(rightWallPosition, rightBoundaryWall);
            }

            for (int x = minX; x <= maxX; x++)
            {
                //sus
                Vector3Int upWallPosition = new Vector3Int(x, maxY + 1, 0);
                dungeonData.AddUpBoundaryWalls((Vector2Int)upWallPosition);
                upBoundary.SetTile(upWallPosition, upBoundarytWall);
            }
        }

        Debug.Log("Boundary walls generated for all rooms.");
    }



    /// <summary>
    /// For update user will draw one room ONE BY ONE, than will press S key (aka save). 
    /// </summary>
   /* private void Update()//Atunci cand desenez o camera am grija sa creez un nou obiect Room.
    {                    //Atunci cand sterg modific o camera am grija sa sterg/ modific obiectul de tip Room.

        HashSet<Vector2Int> newFloorRoom = new HashSet<Vector2Int>();//create a new room from hand drawing after press E key.

        if (CanEdit)
        {
            Vector3 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int cellPosition = roomMap.WorldToCell(worldPoint);

            if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
            {
                //editam peste ce e desenat
                if (dungeonData.VerifyColliderTile((Vector2Int)cellPosition) == true)
                {
                    dungeonData.RemoveColliderTiles((Vector2Int)cellPosition);
                    colliderMap.SetTile(cellPosition, null);
                }
                if (dungeonData.VerifyColliderTile((Vector2Int)cellPosition) == true)
                {
                    dungeonData.RemoveDungeonDownWallTiles((Vector2Int)cellPosition);
                    downWalls.SetTile(cellPosition, null);
                }
                if (dungeonData.VerifyColliderTile((Vector2Int)cellPosition) == true)
                {
                    dungeonData.RemoveDungeonUpWallTiles((Vector2Int)cellPosition);
                    upWalls.SetTile(cellPosition, null);
                }
                if (dungeonData.VerifyDungeonTile((Vector2Int)cellPosition) == true)
                {
                    dungeonData.RemoveDungeonRightWallTiles((Vector2Int)cellPosition);
                    rightWalls.SetTile(cellPosition, null);
                }
                if (dungeonData.VerifyDungeonTile((Vector2Int)cellPosition) == true)
                {
                    dungeonData.RemoveDungeonLeftWallTiles((Vector2Int)cellPosition);
                    leftWalls.SetTile(cellPosition, null);
                }
                //editam peste ce e desenat
            }


            if (Input.GetMouseButton(0))//Click stanga pentru a desena
            {
                dungeonData.AddDungeonTiles((Vector2Int)cellPosition);
                roomMap.SetTile(cellPosition, floorTile);

                newFloorRoom.Add((Vector2Int)cellPosition);//noile tiles pt noul Room
            }

            if (Input.GetMouseButton(1))//Click dreapta pentru a sterge
            {
                if (dungeonData.VerifyDungeonTile((Vector2Int)cellPosition) == true)
                {
                    dungeonData.RemoveRightBoundaryWalls((Vector2Int)cellPosition);
                    rightBoundary.SetTile(cellPosition, null);
                }
                if (dungeonData.VerifyDungeonTile((Vector2Int)cellPosition) == true)
                {
                    dungeonData.RemoveUpBoundaryWalls((Vector2Int)cellPosition);
                    upBoundary.SetTile(cellPosition, null);
                }
                dungeonData.RemoveDungeonTiles((Vector2Int)cellPosition);
                roomMap.SetTile(cellPosition, null);

                newFloorRoom.Remove((Vector2Int)cellPosition);//stergem din tiles la obiectul Room
            }

        }


        //Activeaza editarea la apasarea tastei E
        if (Input.GetKeyDown(KeyCode.E))
        {
            CanEdit = true;
            Debug.Log("Editare activată.");
        }
        //Dezactiveaza editarea la apasarea tastei S
        if (Input.GetKeyDown(KeyCode.S))
        {
            CanEdit = false;
            Debug.Log("Editare dezactivată. Actualizare collider...");

            Vector2Int roomCenter = GetCenterOfRoom(newFloorRoom);//noul Room pt ce am desenat
            Room newRoom = new Room(roomCenter, newFloorRoom);
            dungeonData.AddRoom(newRoom);//adaugam noul obiect Room la dungeonData


            AdjustCameraToDungeon();
            dungeonData.GenerateDungeonCollider();
            GenerateWalls();
            GenerateRoomBoundaryWalls();
        }
    }
   */
    private Vector2Int GetCenterOfRoom(HashSet<Vector2Int> roomTiles)
    {
        if (roomTiles == null || roomTiles.Count == 0)
        {
            Debug.LogWarning("Camera este goală sau null!");
            return Vector2Int.zero;
        }

        int sumX = 0, sumY = 0;

        foreach (Vector2Int tile in roomTiles)
        {
            sumX += tile.x;
            sumY += tile.y;
        }

        int centerX = Mathf.RoundToInt((float)sumX / roomTiles.Count);
        int centerY = Mathf.RoundToInt((float)sumY / roomTiles.Count);

        return new Vector2Int(centerX, centerY);
    }

    private void AdjustCameraToDungeon()
    {
        if (dungeonData == null || dungeonData.DungeonTiles.Count == 0)
        {
            Debug.LogWarning("Nu există tiles în dungeon pentru a ajusta camera.");
            return;
        }

        //limitele dungeon
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (Vector2Int tile in dungeonData.DungeonTiles)
        {
            if (tile.x < minX) minX = tile.x;
            if (tile.x > maxX) maxX = tile.x;
            if (tile.y < minY) minY = tile.y;
            if (tile.y > maxY) maxY = tile.y;
        }

        Vector2 center = new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);

        Camera.main.transform.position = new Vector3(center.x, center.y, Camera.main.transform.position.z);

        float distance = Mathf.Max(maxX - minX, maxY - minY) * 0.6f;
        Camera.main.transform.position = new Vector3(center.x, center.y, -distance);
    }
}
