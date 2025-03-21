using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;

public class SimpleDungeonGenerator : MonoBehaviour
{

    [SerializeField] private Tilemap roomMap, colliderMap, leftWalls, rightWalls, upWalls, downWalls, rightBoundary, upBoundary;

    [SerializeField] private TileBase floorTile, colliderTile, leftWall, rightWall, upWall, downWall, rightBoundaryWall, upBoundaryWall;

    [SerializeField] private InputActionReference generate;

    private bool CanEdit = false;

    [SerializeField] private UnityEvent OnFinishedRoomGenerator;

    [SerializeField] private DungeonData dungeonData;

    [SerializeField] private ApartmentConfig apartmentConfig;

    private void Awake()
    {
        dungeonData = DungeonData.Instance;
        if (dungeonData == null)
        {
            dungeonData = gameObject.AddComponent<DungeonData>(); // Create if necessary
        }

        if (apartmentConfig == null)
        {
            Debug.LogError("ApartmentConfig is missing! Please assign it in the Inspector.");
            enabled = false; // Disable the script to prevent further execution
            return;
        }

        if (generate == null || generate.action == null)
        {
            Debug.LogError("InputActionReference 'generate' is missing or not set up correctly!");
            enabled = false;
            return;
        }

        generate.action.started += Generate;
        generate.action.Enable();

    }

    private void OnDestroy()
    {
        if (generate != null && generate.action != null)
        {
            generate.action.started -= Generate;
        }
    }


    private void Generate(InputAction.CallbackContext obj)
    {
        if (dungeonData == null)
        {
            Debug.LogError("DungeonData is missing! Cannot generate dungeon.");
            return;
        }

        if (apartmentConfig == null)
        {
            Debug.LogError("ApartmentConfig is missing! Cannot generate an apartment.");
            return;
        }

        //Clear previous dungeon
        if (dungeonData.GetRooms().Count != 0)
        {
            dungeonData.ClearAll();
            ClearRoomTiles();
        }

        GenerateApartment();

        if (dungeonData != null)
        {
            dungeonData.GenerateDungeonTiles();
            dungeonData.GenerateDungeonCollider();
            CreateDungeonCollider(); // For Tilemap
            GenerateWalls();
        }

        AdjustCameraToDungeon();
        OnFinishedRoomGenerator?.Invoke();

        // Allow manual editing after generation
        CanEdit = true;
    }

    private void GenerateApartment()
    {
        if (apartmentConfig == null)
        {
            Debug.LogError("ApartmentConfig nu a fost atribuit!");
            return;
        }

        List<Room> localGeneratedRooms = new List<Room>();
        foreach (var item in apartmentConfig.GetRooms())
        {
            Room newRoom = new Room(Vector2Int.zero, item.GetRoomType(), item.GetRoomDimensions());
            localGeneratedRooms.Add(newRoom);
        }
        if (localGeneratedRooms.Count == 0)
        {
            Debug.LogError("No rooms generated! ApartmentConfig might be empty.");
            return;
        }

        List<List<Room>> listRooms = GenerateRoomGroups(localGeneratedRooms, false);
        if (listRooms.Count == 0)
        {
            Debug.LogError("Room grouping failed! Aborting room placement.");
            return;
        }

        List<int> directions = PlaceRoomsProcedurally(listRooms);
        foreach (Room room in localGeneratedRooms)
        {
            Room roomCreated = CreateRectangularRoomAt(room.RoomCenterPos(), room.GetRoomType(), room.GetDimensions());
            dungeonData.AddRoom(roomCreated);
        }
        PlaceHallsProceduraly(listRooms, directions);
    }

    private List<List<Room>> GenerateRoomGroups(List<Room> allRooms, bool openSpaceLivingKitchen)//merge bine
    {
        Debug.Log("Metoda GenerateRoomGroups a fost apelată!");
        List<List<Room>> roomGroups = new List<List<Room>>();

        List<Room> unplacedRooms = new List<Room>(allRooms);

        while (unplacedRooms.Count > 0)
        {
            int groupSize = UnityEngine.Random.Range(1, 5);
            groupSize = Mathf.Min(groupSize, unplacedRooms.Count);

            List<Room> newGroup = new List<Room>();

            for (int i = 0; i < groupSize; i++)
            {
                newGroup.Add(unplacedRooms[0]);
                unplacedRooms.RemoveAt(0);
            }

            roomGroups.Add(newGroup);
        }


        foreach (var group in roomGroups)
        {
            Debug.Log("Grup nou:");
            foreach (var room in group)
            {
                Debug.Log($" - {room.GetRoomType()}");
            }
        }
        return roomGroups;
    }

    private List<int> PlaceRoomsProcedurally(List<List<Room>> roomGroups)
    {
        Debug.Log("Metoda PlaceRoomsProcedurally a fost apelata!");
        Dictionary<Room, Vector2Int> roomPositions = new Dictionary<Room, Vector2Int>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

        List<int> groupDirections = new List<int>();
        Vector2Int currentPosition = Vector2Int.zero; //Poziția de start pentru primul grup
        int lastGroupDirection = -1; //Inițial, nu avem direcție

        foreach (var group in roomGroups)
        {
            if (group.Count == 0) continue;

            List<int> possibleDirections = new List<int> { 0, 1, 2, 3 };//sus, stanga, jos, dreapta

            currentPosition = currentPosition + group[0].AddToCurrentPosition(lastGroupDirection);

            if (lastGroupDirection != -1) //Dacă avem deja un grup plasat
            {
                int oppositeDirection = (lastGroupDirection + 2) % 4;
                Debug.Log("directie opusa eliminata: " + oppositeDirection);
                possibleDirections.Remove(oppositeDirection); //Eliminăm direcția inversă
                foreach (var variabila in possibleDirections)
                {
                    Debug.Log("directie ramasa in lista: " + variabila);
                }
            }

            //Alegem o direcție aleatorie dintre cele valide
            int groupDirection = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];
            Debug.Log("directie aleasa: " + groupDirection);

            //Setăm prima cameră din grup ca punct de referință
            Room firstRoom = group[0];
            firstRoom.SetRoomCenter(currentPosition);
            Debug.Log("primul centru din grup: " + currentPosition + ", directie " + lastGroupDirection);
            Debug.Log("dimensiunile camerei: " + firstRoom.GetDimensions());
            roomPositions[firstRoom] = currentPosition;
            occupiedPositions.Add(currentPosition);

            Room lastRoom = firstRoom; //Ținem minte ultima cameră plasată

            //Plasăm restul camerelor din grup în aceeași direcție
            for (int i = 1; i < group.Count; i++)
            {
                Room currentRoom = group[i];
                Room neighborRoom = group[i - 1];

                (Vector2Int position, int direction) = GetNeighborPosition(neighborRoom, currentRoom, groupDirection);

                currentRoom.SetRoomCenter(position);
                Debug.Log("al " + (i + 1) + "-lea centru din grup: " + position + ", cu directia: " + direction);
                Debug.Log("dimensiunile camerei: " + currentRoom.GetDimensions());

                roomPositions[currentRoom] = position;
                occupiedPositions.Add(position);

                lastRoom = currentRoom;
            }

            Vector2Int lastRoomSize = lastRoom.GetDimensions();

            switch (groupDirection)
            {
                case 0: // sus
                    if (lastRoomSize.y % 2 != 0)
                    {
                        currentPosition = lastRoom.RoomCenterPos() + new Vector2Int(0, (lastRoomSize.y - 1) / 2 + 1 + UnityEngine.Random.Range(3, 5));
                        break;
                    }
                    currentPosition = lastRoom.RoomCenterPos() + new Vector2Int(0, lastRoomSize.y / 2 + UnityEngine.Random.Range(3, 5));
                    break;
                case 1: // stanga
                    if (lastRoomSize.x % 2 != 0)
                    {
                        currentPosition = lastRoom.RoomCenterPos() - new Vector2Int((lastRoomSize.x - 1) / 2 + 1 + UnityEngine.Random.Range(3, 5), 0);
                        break;
                    }
                    currentPosition = lastRoom.RoomCenterPos() - new Vector2Int(lastRoomSize.x / 2 + UnityEngine.Random.Range(3, 5), 0);
                    break;
                case 2: // jos
                    if (lastRoomSize.y % 2 != 0)
                    {
                        currentPosition = lastRoom.RoomCenterPos() - new Vector2Int(0, (lastRoomSize.y - 1) / 2 + UnityEngine.Random.Range(3, 5));
                        break;
                    }
                    currentPosition = lastRoom.RoomCenterPos() - new Vector2Int(0, lastRoomSize.y / 2 + UnityEngine.Random.Range(3, 5));
                    break;
                case 3: // dreapta
                    if (lastRoomSize.x % 2 != 0)
                    {
                        currentPosition = lastRoom.RoomCenterPos() + new Vector2Int((lastRoomSize.x - 1) / 2 + UnityEngine.Random.Range(3, 5), 0);
                        break;
                    }
                    currentPosition = lastRoom.RoomCenterPos() + new Vector2Int(lastRoomSize.x / 2 + UnityEngine.Random.Range(3, 5), 0);
                    break;
            }

            lastGroupDirection = groupDirection;
            groupDirections.Add(lastGroupDirection);
        }
        return groupDirections;
    }
    private (Vector2Int, int) GetNeighborPosition(Room neighborRoom, Room currentRoom, int dir = -1)//merge bine
    {
        Debug.Log("Metoda GetNeighborPosition a fost apelata!");
        Vector2Int neighborCenter = neighborRoom.RoomCenterPos();
        Debug.Log("neighborCenter: " + neighborCenter);
        Vector2Int currentDimensions = currentRoom.GetDimensions();
        Debug.Log("currentDimensions: " + currentDimensions);
        Vector2Int neighborDimensions = neighborRoom.GetDimensions();
        Debug.Log("neighborDimensions: " + neighborDimensions);

        //Dacă nu se primește o direcție (dir == -1), alegem una aleatorie
        if (dir == -1)
        {
            dir = UnityEngine.Random.Range(0, 4); //Alege aleatoriu o direcție (0 = sus, 1 = stanga, 2 = jos, 3 = dreapta)
        }

        switch (dir)
        {
            case 0: //sus
                if (currentDimensions.y % 2 != 0)
                {
                    Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + (currentDimensions.y - 1) / 2 + 1), dir));
                    return (new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + (currentDimensions.y - 1) / 2 + 1), dir);
                }
                Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + currentDimensions.y / 2), dir));
                return (new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + currentDimensions.y / 2), dir);
            case 1: //stanga
                if (currentDimensions.x % 2 != 0)
                {
                    Debug.Log((new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - (currentDimensions.x - 1) / 2 - 1, neighborCenter.y), dir));
                    return (new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - (currentDimensions.x - 1) / 2 - 1, neighborCenter.y), dir);
                }
                Debug.Log((new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - currentDimensions.x / 2, neighborCenter.y), dir));
                return (new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - currentDimensions.x / 2, neighborCenter.y), dir);
            case 2: //jos
                if (neighborDimensions.y % 2 != 0)
                {
                    Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y - (neighborDimensions.y - 1) / 2 - 1 - currentDimensions.y / 2), dir));
                    return (new Vector2Int(neighborCenter.x, neighborCenter.y - (neighborDimensions.y - 1) / 2 - 1 - currentDimensions.y / 2), dir);
                }
                Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y - neighborDimensions.y / 2 - currentDimensions.y / 2), dir));
                return (new Vector2Int(neighborCenter.x, neighborCenter.y - neighborDimensions.y / 2 - currentDimensions.y / 2), dir);
            case 3: //dreapta
                if (neighborDimensions.x % 2 != 0)
                {
                    Debug.Log((new Vector2Int(neighborCenter.x + (neighborDimensions.x - 1) / 2 + 1 + currentDimensions.x / 2, neighborCenter.y), dir));
                    return (new Vector2Int(neighborCenter.x + (neighborDimensions.x - 1) / 2 + 1 + currentDimensions.x / 2, neighborCenter.y), dir);
                }
                Debug.Log((new Vector2Int(neighborCenter.x + neighborDimensions.x / 2 + currentDimensions.x / 2, neighborCenter.y), dir));
                return (new Vector2Int(neighborCenter.x + neighborDimensions.x / 2 + currentDimensions.x / 2, neighborCenter.y), dir);
            default:
                Debug.Log("nu a mers");
                return (Vector2Int.zero, -1);
        }
    }

    private Room CreateRectangularRoomAt(Vector2Int roomCenterPosition, RoomType roomType, Vector2Int roomSize)
    {
        Vector2Int half = roomSize / 2;
        bool isOddX = roomSize.x % 2 != 0;
        bool isOddY = roomSize.y % 2 != 0;

        if (isOddX) half.x = (roomSize.x - 1) / 2;
        if (isOddY) half.y = (roomSize.y - 1) / 2;

        Room room = new Room(roomCenterPosition, roomType, roomSize);

        for (var x = -half.x; x < half.x + (isOddX ? 1 : 0); x++)
        {
            for (var y = -half.y - (isOddY ? 1 : 0); y < half.y; y++)
            {
                Vector2Int position = roomCenterPosition + new Vector2Int(x, y);
                room.AddFloorTiles(position);
                dungeonData.AddDungeonTiles(position);

                Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
                roomMap.SetTile(tilePosition, floorTile);

                //Room tiles:
                if (x == half.x - 1 + (isOddX ? 1 : 0))//right tiles
                {
                    room.AddNearWallTilesRight(position);
                    dungeonData.AddDungeonRightWallTiles(position);
                }
                if (y == half.y - 1)//up tiles
                {
                    room.AddNearWallTilesUp(position);
                    dungeonData.AddDungeonUpWallTiles(position);
                }
                if (x == -half.x)//left tiles
                {
                    room.AddNearWallTilesLeft(position);
                    dungeonData.AddDungeonLeftWallTiles(position);
                }
                if (y == -half.y - (isOddY ? 1 : 0))//down tiles
                {
                    room.AddNearWallTilesDown(position);
                    dungeonData.AddDungeonDownWallTiles(position);
                }

            }
        }
        Debug.Log("avem " + room.GetNearWallTilesRight().Count + " tiles in NearWallTilesRight");
        Debug.Log("avem " + room.GetNearWallTilesUp().Count + " tiles in NearWallTilesUp");
        Debug.Log("avem " + room.GetNearWallTilesLeft().Count + " tiles in NearWallTilesLeft");
        Debug.Log("avem " + room.GetNearWallTilesDown().Count + " tiles in NearWallTilesDown");

        //Set boundary tiles:
        for (var y = -half.y - (isOddY ? 1 : 0); y < half.y; y++)//right boundary
        {
            Vector2Int position = roomCenterPosition + new Vector2Int(half.x + (isOddX ? 1 : 0), y);
            Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
            dungeonData.AddRightBoundaryWalls(position);
            rightBoundary.SetTile(tilePosition, rightBoundaryWall);
        }
        for (var x = -half.x; x < half.x + (isOddX ? 1 : 0); x++)//up boundary
        {
            Vector2Int position = roomCenterPosition + new Vector2Int(x, half.y);
            Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
            dungeonData.AddUpBoundaryWalls(position);
            upBoundary.SetTile(tilePosition, upBoundaryWall);

        }

        return room;
    }


    private void PlaceHallsProceduraly(List<List<Room>> roomGroups, List<int> directions)//merge bine
    {
        for (int i = 0; i < roomGroups.Count - 1; i++)
        {
            Room lastRoomFromGroup = roomGroups[i][roomGroups[i].Count - 1];
            Room firstRoomFromNextGroup = roomGroups[i + 1][0];

            CreateHallBetween(firstRoomFromNextGroup, lastRoomFromGroup, directions[i]);

        }
    }

    private void CreateHallNextGroupWall(List<Room> group, List<Vector2Int> startEndPos)
    {


    }

    private List<Vector2Int> CreateHallBetween(Room firstRoomFromNextGroup, Room lastRoomFromGroup, int direction = -1)
    {
        Debug.Log("Am apelat CreateHallBetween.");
        if (firstRoomFromNextGroup == null || lastRoomFromGroup == null)
        {
            Debug.LogWarning("Eroare: Una dintre camere este null!");
            return new List<Vector2Int>();
        }

        Vector2Int start;
        Vector2Int end;
        Vector2Int hallwayDirection = direction switch
        {
            0 => Vector2Int.up,   // sus
            1 => Vector2Int.left, // stânga
            2 => Vector2Int.down, // jos
            3 => Vector2Int.right,// dreapta
            _ => Vector2Int.zero
        };

        if (hallwayDirection == Vector2Int.zero)
        {
            Debug.LogWarning("Direcție nespecificată sau invalidă!");
            return new List<Vector2Int>();
        }

        start = GetMiddleTile(lastRoomFromGroup, direction);
        end = GetMiddleTile(firstRoomFromNextGroup, (direction + 2) % 4);

        Debug.Log($"Creare hol de la {start} la {end} în direcția {hallwayDirection}.");
        CreateHallway(start, end, hallwayDirection);

        return new List<Vector2Int>() { start, end };
    }

    private Vector2Int GetMiddleTile(Room room, int direction)
    {
        switch (direction)
        {
            case 0: // sus
                if (room.GetDimensions().y % 2 != 0)
                {
                    return new Vector2Int(room.RoomCenterPos().x, room.RoomCenterPos().y + (room.GetDimensions().y - 1) / 2);
                }
                return new Vector2Int(room.RoomCenterPos().x, room.RoomCenterPos().y + room.GetDimensions().y / 2);
            case 1: // stânga
                if (room.GetDimensions().x % 2 != 0)
                {
                    return new Vector2Int(room.RoomCenterPos().x - (room.GetDimensions().x - 1) / 2, room.RoomCenterPos().y);
                }
                return new Vector2Int(room.RoomCenterPos().x - room.GetDimensions().x / 2, room.RoomCenterPos().y);
            case 2: // jos
                if (room.GetDimensions().y % 2 != 0)
                {
                    return new Vector2Int(room.RoomCenterPos().x, room.RoomCenterPos().y - (room.GetDimensions().y - 1) / 2 - 1);
                }
                return new Vector2Int(room.RoomCenterPos().x, room.RoomCenterPos().y - room.GetDimensions().y / 2);
            case 3: // dreapta
                if (room.GetDimensions().x % 2 != 0)
                {
                    return new Vector2Int(room.RoomCenterPos().x + (room.GetDimensions().x - 1) / 2 + 1, room.RoomCenterPos().y);
                }
                return new Vector2Int(room.RoomCenterPos().x + room.GetDimensions().x / 2, room.RoomCenterPos().y);
            default:
                Debug.LogError("Direcție invalidă pentru selecția tile-ului din mijloc!");
                return Vector2Int.zero;
        }
    }


    private void CreateHallway(Vector2Int start, Vector2Int end, Vector2Int direction)
    {
        Vector2Int currentPos = start;
        int distanceOX = Math.Abs(end.x - start.x);
        int distanceOY = Math.Abs(end.y - start.y);

        Hall hall = new Hall(new Vector2Int(Math.Max(1, distanceOX), Math.Max(1, distanceOY)));

        while (currentPos != end + direction)
        {
            hall.AddFloorTiles(currentPos);
            Vector3Int tilePosition = new Vector3Int(currentPos.x, currentPos.y, 0);
            roomMap.SetTile(tilePosition, floorTile);

            if (currentPos.x != end.x + direction.x)
                currentPos.x += direction.x;
            if (currentPos.y != end.y + direction.y)
                currentPos.y += direction.y;
        }
        dungeonData.AddHall(hall);
    }

    private void ClearRoomTiles()
    {
        roomMap.ClearAllTiles();//pt a evita duplicarea
        rightBoundary.ClearAllTiles();
        upBoundary.ClearAllTiles();
    }

    private void CreateDungeonCollider()
    {
        if (dungeonData == null)
        {
            Debug.LogError("DungeonData is missing! Cannot create dungeon collider.");
            return;
        }
        colliderMap.ClearAllTiles();//Pt a evita duplicarea
        foreach (Vector2Int pos in dungeonData.GetColliderTiles())
        {
            Vector3Int scaledPosition = new Vector3Int(pos.x, pos.y, 0);
            colliderMap.SetTile(scaledPosition, colliderTile);
            //colliderMap.SetTile((Vector3Int)pos, colliderTile);
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

        foreach (Vector2Int posRoom in dungeonData.GetDungeonTiles())
        {
            Vector2Int leftWallTile = posRoom + Vector2Int.left;//left wall all tiles
            if (dungeonData.VerifyDungeonTile(leftWallTile) == false)
            {
                dungeonData.AddDungeonLeftWallTiles(leftWallTile);
                //leftWalls.SetTile((Vector3Int)leftWallTile, leftWall);
                leftWalls.SetTile(new Vector3Int(leftWallTile.x, leftWallTile.y, 0), leftWall);

            }

            Vector2Int rightWallTile = posRoom + Vector2Int.right;//right wall all tiles
            if (dungeonData.VerifyDungeonTile(rightWallTile) == false)
            {
                dungeonData.AddDungeonRightWallTiles(rightWallTile);
                //rightWalls.SetTile((Vector3Int)rightWallTile, rightWall);
                rightWalls.SetTile(new Vector3Int(rightWallTile.x, rightWallTile.y, 0), rightWall);

            }

            Vector2Int upWallTile = posRoom + Vector2Int.up;//up wall all tiles
            if (dungeonData.VerifyDungeonTile(upWallTile) == false)
            {
                dungeonData.AddDungeonUpWallTiles(upWallTile);
                //upWalls.SetTile((Vector3Int)upWallTile, upWall);
                upWalls.SetTile(new Vector3Int(upWallTile.x, upWallTile.y, 0), upWall);

            }

            Vector2Int downWallTile = posRoom + Vector2Int.down;//down wall all tiles
            if (dungeonData.VerifyDungeonTile(downWallTile) == false)
            {
                dungeonData.AddDungeonDownWallTiles(downWallTile);
                //downWalls.SetTile((Vector3Int)downWallTile, downWall);
                downWalls.SetTile(new Vector3Int(downWallTile.x, downWallTile.y, 0), downWall);

            }
        }
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

    private void AdjustCameraToDungeon()
    {
        if (dungeonData == null || dungeonData.GetRooms().Count == 0)
        {
            Debug.LogError("DungeonData nu conține camere!");
            return;
        }

        // Inițializăm limitele (Bounds) folosind prima cameră
        Vector2Int firstRoomCenter = dungeonData.GetRooms()[0].RoomCenterPos();
        Bounds bounds = new Bounds(new Vector3(firstRoomCenter.x, firstRoomCenter.y, 0), Vector3.zero);

        // Extindem limitele pentru toate camerele
        foreach (var room in dungeonData.GetRooms())
        {
            Vector2Int roomCenter = room.RoomCenterPos();
            Vector2 roomSize = room.GetDimensions(); // Vector2 pentru dimensiuni

            Vector3 roomPos = new Vector3(roomCenter.x, roomCenter.y, 0);
            Vector3 roomSize3D = new Vector3(roomSize.x, roomSize.y, 0);

            bounds.Encapsulate(roomPos + roomSize3D / 2);
            bounds.Encapsulate(roomPos - roomSize3D / 2);
        }

        // Setăm noua poziție a camerei
        Camera.main.transform.position = new Vector3(bounds.center.x, bounds.center.y, Camera.main.transform.position.z);

        // Calculăm dimensiunea optimă a camerei
        float verticalSize = bounds.extents.y + 2f; // Adăugăm o margine
        float horizontalSize = (bounds.extents.x + 2f) / Camera.main.aspect;

        float zoomFactor = 0.9f; // Reduce dimensiunea camerei cu 10%
        Camera.main.orthographicSize = Mathf.Max(verticalSize, horizontalSize) * zoomFactor;

        Debug.Log($"Camera ajustată la poziția {Camera.main.transform.position}, cu mărimea {Camera.main.orthographicSize}");
    }




}
