using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem;
using System;
using Unity.VisualScripting;
using System.Linq;
using UnityEngine.UIElements;
using System.Text.RegularExpressions;
using UnityEditor.Experimental.GraphView;
using Unity.Burst.Intrinsics;
using System.Data;


public class SimpleDungeonGenerator : MonoBehaviour
{

    [SerializeField] private Tilemap roomMap, colliderMap, leftWalls, rightWalls, upWalls, downWalls, rightBoundary, upBoundary, leftBoundary, downBoundary, paths, objects, overObjects;

    [SerializeField] private TileBase floorTile, colliderTile, sandstone, leftWall, rightWall, upWall, downWall, rightBoundaryWall, upBoundaryWall, leftBoundaryWall, downBoundaryWall, counter, sink;

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
        Room.ResetRoomIds();

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
        ConsoleUtil.ClearConsole();


        GenerateApartment();

        if (dungeonData != null)
        {
            dungeonData.GenerateDungeonRoomTiles();
            //dungeonData.GenerateDungeonHallTiles();
            dungeonData.GenerateDungeonAllTiles();

            dungeonData.GenerateDungeonCollider();
            CreateDungeonCollider();
            GenerateWalls();
        }

        AdjustCameraToDungeon();
        OnFinishedRoomGenerator?.Invoke();

        //Allow manual editing after generation
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
            Vector2Int dimensions = item.GetRoomDimensions();

            if (dimensions.x > 0 && dimensions.y > 0)
            {
                Room newRoom = new Room(Vector2Int.zero, item.GetRoomType(), dimensions);
                localGeneratedRooms.Add(newRoom);
            }
            else
            {
                Debug.LogWarning($"Camera cu tipul {item.GetRoomType()} are dimensiuni invalide: {dimensions}");
            }
        }

        if (localGeneratedRooms.Count == 0)
        {
            Debug.LogError("No rooms generated! ApartmentConfig might be empty.");
            return;
        }

        List<List<Room>> listRooms = RoomGroupGenerator.GenerateRoomGroups(localGeneratedRooms);//listRooms have Room objects without center position seted.
        if (listRooms.Count == 0)
        {
            Debug.LogError("Room grouping failed! Aborting room placement.");
            return;
        }
        bool markedKitchen = false;
        bool markedLivingRoom = false;
        foreach (List<Room> group in listRooms)
        {
            //Pentru bucătărie
            if (apartmentConfig.IncludeOpenSpaceKitchen && !markedKitchen)
            {
                Room kitchenRoom = group.Find(r => r.GetRoomType() == RoomType.Bucatarie);//referinta; modificam pe obiectul din lista
                if (kitchenRoom != null)
                {
                    kitchenRoom.SetSkipBoundaryWalls(true);
                    markedKitchen = true;
                }
            }
            //Pentru sufragerie
            if (apartmentConfig.IncludeOpenSpaceLivingRoom && !markedLivingRoom)
            {
                Room livingRoom = group.Find(r => r.GetRoomType() == RoomType.Sufragerie);//referinta; modificam pe obiectul din lista
                if (livingRoom != null)
                {
                    livingRoom.SetSkipBoundaryWalls(true);
                    markedLivingRoom = true;
                }
            }
        }

        List<bool> LShapeGroups = PlaceRoomsProcedurally(listRooms, 0, listRooms.Count);

        //ConnectFirstTwoGroupsWithAStar(dungeonData.GetListRoomGroups(), dungeonData.GetDirections(), LShapeGroups);//merge
        ConnectGroupCentersWithAStar(dungeonData.GetListRoomGroups());
        FillHallGaps();
        // ConnectBetweenGroupsWithAStar(dungeonData.GetListRoomGroups(), 0, dungeonData.GetListRoomGroups().Count, dungeonData.GetDirections());//merge
        // FillHallGaps();
        // ConnectDisjointHalls(dungeonData.GetHalls());
        // ConnectRoomsWithClosestHall(dungeonData.GetListRoomGroups(), 0, dungeonData.GetListRoomGroups().Count, dungeonData.GetHalls());
        // FillHallGaps();
        //  PruneIsolatedHallTiles();
        //  ExtendHallToMaxForAllRooms();
        //  FillHallGaps();




        foreach (Room room in dungeonData.GetRooms())
        {
            foreach (Vector2Int tile in room.GetFloorTiles())
            {
                dungeonData.AddDungeonAllTiles(tile);
            }
        }

        foreach (Vector2Int tile in dungeonData.GetDungeonHallTiles())
        {

            dungeonData.AddDungeonAllTiles(tile);

        }

        //FillInteriorHoles();
        //Kitchen:
        //  List<WallData> walls = FindExteriorWalls(dungeonData.GetRooms());
        //  DrawOnKitchenWalls(walls);

    }

    private List<bool> PlaceRoomsProcedurally(List<List<Room>> roomGroups, int contorFirst, int contorFinal)//apelam de mai multe ori
    {
        List<bool> groupIsLShape = new List<bool>();
        Dictionary<Room, Vector2Int> roomPositions = new Dictionary<Room, Vector2Int>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

        Vector2Int currentPosition = Vector2Int.zero;
        int lastGroupDirection = -1;
        List<int> possibleDirections = new List<int> { 0, 1, 2, 3 };//sus, stanga, jos, dreapta

        for (int i = contorFirst; i < contorFinal; i++)
        {
            List<Room> newGroup = new List<Room>();
            if (roomGroups[i].Count == 0)
            {
                groupIsLShape.Add(false);
                continue;
            }

            // inițial: nu e L‑shaped
            bool isLShape = false;
            int directionChangeCount = 0;

            currentPosition = currentPosition + roomGroups[i][0].AddToCurrentPosition(lastGroupDirection);

            // if (lastGroupDirection != -1) //Dacă avem deja un grup plasat
            // {
            //     int oppositeDirection = (lastGroupDirection + 2) % 4;
            //     possibleDirections.Remove(oppositeDirection); //Eliminăm direcția inversă
            // }

            //Alegem o direcție aleatorie dintre cele valide
            int groupDirection = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];

            int prevDir = groupDirection;

            //Setăm prima cameră din grup ca punct de referință
            Room firstRoom = roomGroups[i][0];
            Vector2Int validPosition = FindValidRoomPosition(currentPosition, firstRoom, occupiedPositions, false);
            firstRoom.SetRoomCenter(validPosition);
            roomGroups[i][0].SetRoomCenter(validPosition);

            Debug.Log("firstRoom.SkipBoundaryWalls: " + firstRoom.GetSkipBoundaryWalls());
            Room roomCreated = CreateRectangularRoomAt(firstRoom.GetRoomCenterPos(), firstRoom.GetRoomType(), firstRoom.GetDimensions(), firstRoom.GetSkipBoundaryWalls());
            newGroup.Add(roomCreated);


            roomPositions[firstRoom] = validPosition;
            occupiedPositions.UnionWith(roomCreated.GetFloorTiles()); //Track occupied tiles

            Room lastRoom = firstRoom; //Ținem minte ultima cameră plasată

            //Plasăm restul camerelor din grup
            for (int j = 1; j < roomGroups[i].Count; j++)
            {
                Room currentRoom = roomGroups[i][j];
                Room neighborRoom = roomGroups[i][j - 1];

                (Vector2Int positionInter, int direction) = GetNeighborPosition(neighborRoom, currentRoom, groupDirection);

                // detectează schimbarea de direcție
                if (direction != prevDir)
                {
                    directionChangeCount++;
                    prevDir = direction;
                }
                // c) marchează L‑shape dacă ai măcar un cot
                if (directionChangeCount >= 1)
                    isLShape = true;

                groupDirection = possibleDirections[(groupDirection + 1) % 4];

                // **Ensure no collision before placing**
                Vector2Int position = FindValidRoomPosition(positionInter, currentRoom, occupiedPositions, true);
                currentRoom.SetRoomCenter(position);
                roomGroups[i][j].SetRoomCenter(position);

                Debug.Log("currentRoom.SkipBoundaryWalls: " + currentRoom.GetSkipBoundaryWalls());
                Room groupRoomCreated = CreateRectangularRoomAt(currentRoom.GetRoomCenterPos(), currentRoom.GetRoomType(), currentRoom.GetDimensions(), currentRoom.GetSkipBoundaryWalls());
                newGroup.Add(groupRoomCreated);

                roomPositions[currentRoom] = position;
                occupiedPositions.UnionWith(groupRoomCreated.GetFloorTiles());
                lastRoom = currentRoom;

            }
            dungeonData.AddGroup(newGroup);
            groupIsLShape.Add(isLShape);

            foreach (Room room in newGroup)
            {
                List<Vector2Int> tiles = room.GetFloorTiles();
                List<Vector2Int> candidates = new List<Vector2Int>();
                candidates.AddRange(DungeonData.fourDirections);
                candidates.AddRange(DungeonData.diagonalDirections);
                foreach (var tile in tiles)
                {
                    foreach (var candidate in candidates)
                    {
                        Vector2Int position = tile + candidate;
                        if (!occupiedPositions.Contains(position) && !dungeonData.GetDungeonRoomTiles().Contains(position))
                        {
                            occupiedPositions.Add(position);
                        }
                    }
                }
            }

            lastGroupDirection = groupDirection;
            dungeonData.AddDirection(lastGroupDirection);

            // **Update next group position based on last room size**
            currentPosition = CalculateNextGroupStartPosition(lastRoom, groupDirection);
        }

        for (int k = 0; k < groupIsLShape.Count; k++)
        {
            Debug.Log($"Grup {k + 1} {(groupIsLShape[k] ? "are" : "nu are")} formă de L");
        }
        return groupIsLShape;
    }

    private Vector2Int CalculateNextGroupStartPosition(Room lastRoom, int groupDirection)
    {
        Vector2Int lastRoomSize = lastRoom.GetDimensions();
        Vector2Int lastRoomCenter = lastRoom.GetRoomCenterPos();
        int spacing = 0; //UnityEngine.Random.Range(1, 3);

        switch (groupDirection)
        {
            case 0: // sus
                if (lastRoomSize.y % 2 != 0)
                {
                    return lastRoomCenter + new Vector2Int(0, (lastRoomSize.y - 1) / 2 + 1 + spacing);
                }
                return lastRoomCenter + new Vector2Int(0, lastRoomSize.y / 2 + spacing);
            case 1: // stanga
                if (lastRoomSize.x % 2 != 0)
                {
                    return lastRoomCenter - new Vector2Int((lastRoomSize.x - 1) / 2 + 1 + spacing, 0);
                }
                return lastRoomCenter - new Vector2Int(lastRoomSize.x / 2 + spacing, 0);
            case 2: // jos
                if (lastRoomSize.y % 2 != 0)
                {
                    return lastRoomCenter - new Vector2Int(0, (lastRoomSize.y - 1) / 2 + spacing);
                }
                return lastRoomCenter - new Vector2Int(0, lastRoomSize.y / 2 + spacing);
            case 3: // dreapta
                if (lastRoomSize.x % 2 != 0)
                {
                    return lastRoomCenter + new Vector2Int((lastRoomSize.x - 1) / 2 + spacing, 0);
                }
                return lastRoomCenter + new Vector2Int(lastRoomSize.x / 2 + spacing, 0);
            default:
                return lastRoomCenter;
        }
    }


    private Vector2Int FindValidRoomPosition(Vector2Int proposedCenter, Room room, HashSet<Vector2Int> occupiedTiles, bool neighborOrNot)
    {
        bool initialCollision = false;
        foreach (Vector2Int tile in room.FloorTilesAndSpaceAround(proposedCenter, neighborOrNot))
        {
            if (occupiedTiles.Contains(tile))
            {
                initialCollision = true;
                break;
            }
        }
        if (!initialCollision)
        {
            return proposedCenter;
        }


        int maxAttempts = 49000;
        int attempt = 0;
        List<Vector2Int> directions = new List<Vector2Int>
        {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1)
        };

        int stepDistance = 1;
        while (attempt < maxAttempts)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int testCenter = proposedCenter + dir * stepDistance;

                bool collision = false;
                foreach (Vector2Int tile in room.FloorTilesAndSpaceAround(testCenter, neighborOrNot))
                {
                    if (occupiedTiles.Contains(tile))
                    {
                        collision = true;
                        break;
                    }
                }
                if (!collision)
                {
                    return testCenter;
                }
                attempt++;
                if (attempt >= maxAttempts)
                    break;
            }

            stepDistance++;
        }
        return proposedCenter;
    }


    private (Vector2Int, int) GetNeighborPosition(Room neighborRoom, Room currentRoom, int dir = -1)//merge bine
    {
        //Debug.Log("Metoda GetNeighborPosition a fost apelata!");
        Vector2Int neighborCenter = neighborRoom.GetRoomCenterPos();
        //Debug.Log("neighborCenter: " + neighborCenter);
        Vector2Int currentDimensions = currentRoom.GetDimensions();
        //Debug.Log("currentDimensions: " + currentDimensions);
        Vector2Int neighborDimensions = neighborRoom.GetDimensions();
        //Debug.Log("neighborDimensions: " + neighborDimensions);

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
                    //Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + (currentDimensions.y - 1) / 2 + 1), dir));
                    return (new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + (currentDimensions.y - 1) / 2 + 1), dir);
                }
                //Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + currentDimensions.y / 2), dir));
                return (new Vector2Int(neighborCenter.x, neighborCenter.y + neighborDimensions.y / 2 + currentDimensions.y / 2), dir);
            case 1: //stanga
                if (currentDimensions.x % 2 != 0)
                {
                    //Debug.Log((new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - (currentDimensions.x - 1) / 2 - 1, neighborCenter.y), dir));
                    return (new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - (currentDimensions.x - 1) / 2 - 1, neighborCenter.y), dir);
                }
                //Debug.Log((new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - currentDimensions.x / 2, neighborCenter.y), dir));
                return (new Vector2Int(neighborCenter.x - neighborDimensions.x / 2 - currentDimensions.x / 2, neighborCenter.y), dir);
            case 2: //jos
                if (neighborDimensions.y % 2 != 0)
                {
                    //Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y - (neighborDimensions.y - 1) / 2 - 1 - currentDimensions.y / 2), dir));
                    return (new Vector2Int(neighborCenter.x, neighborCenter.y - (neighborDimensions.y - 1) / 2 - 1 - currentDimensions.y / 2), dir);
                }
                //Debug.Log((new Vector2Int(neighborCenter.x, neighborCenter.y - neighborDimensions.y / 2 - currentDimensions.y / 2), dir));
                return (new Vector2Int(neighborCenter.x, neighborCenter.y - neighborDimensions.y / 2 - currentDimensions.y / 2), dir);
            case 3: //dreapta
                if (neighborDimensions.x % 2 != 0)
                {
                    //Debug.Log((new Vector2Int(neighborCenter.x + (neighborDimensions.x - 1) / 2 + 1 + currentDimensions.x / 2, neighborCenter.y), dir));
                    return (new Vector2Int(neighborCenter.x + (neighborDimensions.x - 1) / 2 + 1 + currentDimensions.x / 2, neighborCenter.y), dir);
                }
                //Debug.Log((new Vector2Int(neighborCenter.x + neighborDimensions.x / 2 + currentDimensions.x / 2, neighborCenter.y), dir));
                return (new Vector2Int(neighborCenter.x + neighborDimensions.x / 2 + currentDimensions.x / 2, neighborCenter.y), dir);
            default:
                Debug.Log("nu s-a putut gasi o pozitie valida");
                return (Vector2Int.zero, -1);
        }
    }

    private Room CreateRectangularRoomAt(Vector2Int roomCenterPosition, RoomType roomType, Vector2Int roomSize, bool skipBoundaryWalls)
    {
        if (roomSize.x <= 0 || roomSize.y <= 0)
        {
            Debug.LogWarning("Dimensiune invalidă a camerei: " + roomSize);
            return null;
        }

        Vector2Int half = roomSize / 2;
        bool isOddX = roomSize.x % 2 != 0;
        bool isOddY = roomSize.y % 2 != 0;

        if (isOddX) half.x = (roomSize.x - 1) / 2;
        if (isOddY) half.y = (roomSize.y - 1) / 2;

        Room room = new Room(roomCenterPosition, roomType, roomSize);
        room.SetSkipBoundaryWalls(skipBoundaryWalls);
        Debug.Log("Am creat camera noua! ");
        Debug.Log("SkipBoundaryWalls: " + room.GetSkipBoundaryWalls());

        for (var x = -half.x; x < half.x + (isOddX ? 1 : 0); x++)
        {
            for (var y = -half.y - (isOddY ? 1 : 0); y < half.y; y++)
            {
                Vector2Int position = roomCenterPosition + new Vector2Int(x, y);
                room.AddFloorTiles(position);
                dungeonData.AddDungeonRoomTiles(position);

                if (roomType == RoomType.Baie)
                {
                    Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
                    roomMap.SetTile(tilePosition, sandstone);
                }
                else
                {
                    Vector3Int tilePosition = new Vector3Int(position.x, position.y, 0);
                    roomMap.SetTile(tilePosition, floorTile);
                }

                //Room tiles:
                if (x == half.x - 1 + (isOddX ? 1 : 0))//right tiles
                {
                    room.AddRightTiles(position);
                    dungeonData.AddDungeonRightWallTiles(position);
                }
                if (x == -half.x)//left tiles
                {
                    room.AddLeftTiles(position);
                    dungeonData.AddDungeonLeftWallTiles(position);
                }
                if (y == half.y - 1)//up tiles
                {
                    room.AddUpTiles(position);
                    dungeonData.AddDungeonUpWallTiles(position);
                }
                if (y == -half.y - (isOddY ? 1 : 0))//down tiles
                {
                    room.AddDownTiles(position);
                    dungeonData.AddDungeonDownWallTiles(position);
                }
                if (x == half.x - 1 + (isOddX ? 1 : 0) && y == half.y - 1)
                {
                    room.AddCornerTiles(position);
                }
                if (x == half.x - 1 + (isOddX ? 1 : 0) && y == -half.y - (isOddY ? 1 : 0))
                {
                    room.AddCornerTiles(position);
                }
                if (x == -half.x && y == half.y - 1)
                {
                    room.AddCornerTiles(position);
                }
                if (x == -half.x && y == -half.y - (isOddY ? 1 : 0))
                {
                    room.AddCornerTiles(position);
                }

            }
        }


        // Verificăm dacă trebuie să setăm boundary walls.
        // Dacă camera este de tip Bucătărie și opțiunea de open space este bifată, nu mai adăugăm boundary walls.
        bool addBoundaryWalls = true;
        // Presupunem că ai o instanță a configurării apartamentului, de exemplu:
        // ApartmentConfig.Instance (sau altă modalitate de acces la ApartmentConfig)
        if (roomType == RoomType.Bucatarie && apartmentConfig.IncludeOpenSpaceKitchen)
        {
            if (room.GetSkipBoundaryWalls() == true)
            {
                addBoundaryWalls = false;
            }
        }
        if (roomType == RoomType.Sufragerie && apartmentConfig.IncludeOpenSpaceLivingRoom)
        {
            if (room.GetSkipBoundaryWalls() == true)
            {
                addBoundaryWalls = false;
            }
        }
        if (addBoundaryWalls)
        {
            //Set boundary tiles:
            for (var y = -half.y - (isOddY ? 1 : 0); y < half.y; y++)//right boundary
            {
                Vector2Int positionRight = roomCenterPosition + new Vector2Int(half.x + (isOddX ? 1 : 0), y);
                Vector3Int tilePosition = new Vector3Int(positionRight.x, positionRight.y, 0);
                dungeonData.AddRightBoundaryWalls(positionRight);
                rightBoundary.SetTile(tilePosition, rightBoundaryWall);

                Vector2Int positionLeft = roomCenterPosition + new Vector2Int(-half.x - 1, y);//left boundary
                Vector3Int tileLeft = new Vector3Int(positionLeft.x, positionLeft.y, 0);
                dungeonData.AddLeftBoundaryWalls(positionLeft);
                leftBoundary.SetTile(tileLeft, leftBoundaryWall);
            }
            for (var x = -half.x; x < half.x + (isOddX ? 1 : 0); x++)//up boundary
            {
                Vector2Int positionUp = roomCenterPosition + new Vector2Int(x, half.y);
                Vector3Int tilePosition = new Vector3Int(positionUp.x, positionUp.y, 0);
                dungeonData.AddUpBoundaryWalls(positionUp);
                upBoundary.SetTile(tilePosition, upBoundaryWall);

                Vector2Int positionDown = roomCenterPosition + new Vector2Int(x, -half.y - (isOddY ? 1 : 0) - 1);//down boundary
                Vector3Int tileDown = new Vector3Int(positionDown.x, positionDown.y, 0);
                dungeonData.AddDownBoundaryWalls(positionDown);
                downBoundary.SetTile(tileDown, downBoundaryWall);
            }
        }
        else
        {
            Debug.Log("Open Space activat.");
        }


        dungeonData.AddRoom(room);
        return room;
    }

    //Hall methods:
    private void ConnectFirstTwoGroupsWithAStar(List<List<Room>> roomGroups, List<int> directions, List<bool> IsLShape)
    {
        Debug.Log("Metoda ConnectGroupsWithAStar a fost apelata!");

        if (roomGroups.Count < 2)
            return;


        List<Room> currentGroup = roomGroups[0];
        List<Room> nextGroup = roomGroups[1];//2 grupuri consecutive

        Room roomA = currentGroup[currentGroup.Count - 1];
        Room closestRoom = null;
        int shortestDistance = int.MaxValue;
        foreach (Room roomB in nextGroup)
        {
            int distance = AStarPathfinder.ManhattanDistance(roomA.GetRoomCenterPos(), roomB.GetRoomCenterPos());
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                closestRoom = roomB;
            }
            dungeonData.AddRoomStartEnd(roomA);
            dungeonData.AddRoomStartEnd(closestRoom);
        }


        // // Room roomA = currentGroup[0];
        // if (IsLShape[0] == false)
        // {
        //     roomA = currentGroup[currentGroup.Count - 1];
        // }
        // dungeonData.AddRoomStartEnd(roomA);
        // //Room roomB = nextGroup[nextGroup.Count - 1];
        // dungeonData.AddRoomStartEnd(roomB);

        if (closestRoom != null)
        {
            Vector2Int start = GetRoomExitTile(roomA, closestRoom.GetRoomCenterPos());
            Vector2Int end = GetRoomExitTile(closestRoom, roomA.GetRoomCenterPos());
            Vector2Int median = (Vector2Int)(start + end) / 2;
            Debug.Log($"START: {start}, END: {end}");

            List<Vector2Int> corridor = AStarPathfinder.AStarPathfindingExtended(start, end);

            dungeonData.AddLenghtStartEnd(corridor.Count);

            if (corridor == null || corridor.Count == 0)
            {
                Debug.LogWarning($"Nu s-a putut genera coridor între {start} și {end}");
            }

            //Create Hall
            Hall hall = new Hall(Vector2Int.zero, Vector2Int.zero);
            List<Vector2Int> thickCorridor = ExpandCorridorThickness(corridor, dungeonData.GetDungeonRoomTiles(), dungeonData.GetDungeonHallTiles());
            foreach (Vector2Int pos in thickCorridor)
            {
                if (!dungeonData.GetDungeonHallTiles().Contains(pos))
                {
                    hall.AddFloorTiles(pos);
                    dungeonData.AddDungeonHallTiles(pos);

                    Vector3Int tilePosition = new Vector3Int(pos.x, pos.y, 0);
                    paths.SetTile(tilePosition, floorTile);
                }
            }

            hall.SetHallCenter(median);
            dungeonData.AddHall(hall);
        }
    }

    private void ConnectGroupCentersWithAStar(List<List<Room>> roomGroups)
    {
        if (roomGroups.Count < 2)
        {
            Debug.LogWarning("Sunt necesare cel puțin 2 grupuri pentru această funcție.");
            return;
        }

        Vector2Int centerA = CalculateGroupCenter(roomGroups[0]);
        Vector2Int centerB = CalculateGroupCenter(roomGroups[1]);
        Vector2Int start = GetNearestWalkableBorderTile(roomGroups[0], centerB);
        Vector2Int end = GetNearestWalkableBorderTile(roomGroups[1], centerA);
        List<Vector2Int> path = AStarPathfinder.AStarPathfindingExtended(start, end);
        Vector2Int median = (Vector2Int)(start + end) / 2;

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Nu s-a putut găsi un drum între cele două centre.");
            return;
        }

        //Create Hall
        Hall hall = new Hall(Vector2Int.zero, Vector2Int.zero);
        List<Vector2Int> thickCorridor = ExpandCorridorThickness(path, dungeonData.GetDungeonRoomTiles(), dungeonData.GetDungeonHallTiles());
        foreach (Vector2Int pos in thickCorridor)
        {
            if (!dungeonData.GetDungeonHallTiles().Contains(pos))
            {
                hall.AddFloorTiles(pos);
                dungeonData.AddDungeonHallTiles(pos);

                Vector3Int tilePosition = new Vector3Int(pos.x, pos.y, 0);
                paths.SetTile(tilePosition, floorTile);
            }
        }

        hall.SetHallCenter(median);
        dungeonData.AddHall(hall);
    }

    private Vector2Int CalculateGroupCenter(List<Room> group)
    {
        if (group == null || group.Count == 0)
            return Vector2Int.zero;

        int sumX = 0, sumY = 0;
        foreach (var room in group)
        {
            Vector2Int center = room.GetRoomCenterPos();
            sumX += center.x;
            sumY += center.y;
        }
        return new Vector2Int(sumX / group.Count, sumY / group.Count);
    }


    private Vector2Int GetNearestWalkableBorderTile(List<Room> group, Vector2Int center)
    {
        HashSet<Vector2Int> borderTiles = new HashSet<Vector2Int>();
        foreach (var room in group)
        {
            foreach (var tile in room.GetLeftTiles()) borderTiles.Add(tile + Vector2Int.left);
            foreach (var tile in room.GetRightTiles()) borderTiles.Add(tile + Vector2Int.right);
            foreach (var tile in room.GetUpTiles()) borderTiles.Add(tile + Vector2Int.up);
            foreach (var tile in room.GetDownTiles()) borderTiles.Add(tile + Vector2Int.down);
        }

        int shortestDistance = int.MaxValue;
        Vector2Int closestTile = Vector2Int.zero;

        foreach (var tile in borderTiles)
        {
            if (AStarPathfinder.IsWalkable(tile))
            {
                int dist = AStarPathfinder.ManhattanDistance(tile, center);
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    closestTile = tile;
                }
            }

        }
        if (shortestDistance == int.MaxValue)
        {
            Debug.LogWarning("No walkable border tile found! Check your room placement.");
            return center; // fallback
        }
        return closestTile;

    }



    private void FillHallGaps()
    {
        var hallTiles = new HashSet<Vector2Int>(dungeonData.GetDungeonHallTiles());
        var roomTiles = new HashSet<Vector2Int>(dungeonData.GetDungeonRoomTiles());

        var occupied = new HashSet<Vector2Int>(hallTiles);
        occupied.UnionWith(roomTiles);

        // 2. Toate direcțiile (4 cardinale + 4 diagonale)
        Vector2Int[] dirs = {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1,1),   new Vector2Int(1,-1),
                new Vector2Int(-1,1),  new Vector2Int(-1,-1)
            };

        // 3. Găsim candidații: neighborii hol‑urilor
        var candidates = new HashSet<Vector2Int>();
        foreach (var h in hallTiles)
            foreach (var d in dirs)
                candidates.Add(h + d);//candidati sunt tiles collider de langa Hall

        // 4. Pentru fiecare candidat, testăm sus+jos sau stânga+dreapta
        foreach (var t in candidates)
        {
            bool up = occupied.Contains(t + Vector2Int.up);
            bool down = occupied.Contains(t + Vector2Int.down);
            bool left = occupied.Contains(t + Vector2Int.left);
            bool right = occupied.Contains(t + Vector2Int.right);

            // dacă e „vertical gap” sau „horizontal gap”
            if ((up && down) || (left && right))
            {
                paths.SetTile(new Vector3Int(t.x, t.y, 0), floorTile);
                dungeonData.AddDungeonHallTiles(t);
                occupied.Add(t);
            }
        }
    }

    private void PruneIsolatedHallTiles()
    {
        // 1. Construim seturile de referință
        var roomTiles = new HashSet<Vector2Int>(dungeonData.GetDungeonRoomTiles());
        var hallTiles = new HashSet<Vector2Int>(dungeonData.GetDungeonHallTiles());

        // occupied = hol + camere
        var occupied = new HashSet<Vector2Int>(hallTiles);
        occupied.UnionWith(roomTiles);

        // 2. Găsim holurile care trebuie eliminate
        var toRemove = new List<Vector2Int>();
        foreach (var h in hallTiles)
        {
            bool hor = occupied.Contains(h + Vector2Int.left)
                    && occupied.Contains(h + Vector2Int.right);
            bool ver = occupied.Contains(h + Vector2Int.up)
                    && occupied.Contains(h + Vector2Int.down);

            // dacă nu e „sus+jos” sau „stânga+dreapta”, îl eliminăm
            if (!ver || !hor)
                toRemove.Add(h);
        }

        // 3. Înlăturăm efectiv tile-urile din Tilemap și din dungeonData
        foreach (var t in toRemove)
        {
            // pe paths Tilemap, ștergem tile-ul
            paths.SetTile(new Vector3Int(t.x, t.y, 0), null);

            // din lista de hol-uri din dungeonData
            dungeonData.RemoveDungeonHallTile(t);
        }

        Debug.Log($"Pruned {toRemove.Count} isolated hall tiles.");
    }

    private void ConnectRoomsWithClosestHall(List<List<Room>> groups, int contorFirst, int contorLast, List<Hall> halls)
    {
        if (groups.Count < 1)
            return;

        for (int i = contorFirst; i < contorLast; i++)
        {
            List<Room> currentGroup = groups[i];//pt fiecare grup

            for (int j = currentGroup.Count - 1; j >= 0; j--)
            {
                Room roomA = currentGroup[j];//pt fiecare camera
                if (!dungeonData.GetRoomsStartEnd().Contains(roomA) && !IsRoomConnected(roomA))
                {
                    Hall closestHall = null;
                    int shortestDistance = int.MaxValue;

                    foreach (Hall hallB in halls)//pt fiecare hol
                    {
                        int dist = AStarPathfinder.ManhattanDistance(roomA.GetRoomCenterPos(), hallB.GetHallCenter());
                        if (dist < shortestDistance)
                        {
                            shortestDistance = dist;
                            closestHall = hallB;
                        }
                    }

                    if (closestHall != null)
                    {
                        dungeonData.AddRoomStartEnd(roomA);
                        List<Vector2Int> hallBTiles = closestHall.GetFloorTiles();
                        List<Vector2Int> roomATiles = GetFreeTiles(roomA);

                        // verifică ambele seturi de „tile”-uri
                        if (roomATiles == null || roomATiles.Count == 0 ||
                            hallBTiles == null || hallBTiles.Count == 0)
                        {
                            Debug.LogWarning("Nu sunt puncte de conectare valide — sărim peste acest cuplaj.");
                            continue;
                        }

                        Vector2Int bestStart = roomATiles[0];
                        Vector2Int bestEnd = hallBTiles[0];
                        int bestDist = int.MaxValue;
                        foreach (var cTile in roomATiles)
                        {
                            foreach (var hTile in hallBTiles)
                            {
                                int dist = AStarPathfinder.ManhattanDistance(cTile, hTile);
                                if (dist < bestDist)
                                {
                                    bestDist = dist;
                                    bestStart = cTile;
                                    bestEnd = hTile;
                                }
                            }
                        }
                        Debug.Log("start: " + bestStart);
                        Debug.Log("end: " + bestEnd);

                        List<Vector2Int> corridor = AStarPathfinder.AStarPathfindingExtended(bestStart, bestEnd);

                        //if (IsCorridorOk(new List<List<Room>> { currentGroup }, corridor) == false)
                        {
                            // continue;
                        }
                        /*foreach (var tile in corridor)
                        {
                            Debug.Log("tile: " + tile);
                        }*/

                        if (corridor == null || corridor.Count == 0)
                        {
                            Debug.LogWarning($"Nu s-a putut genera coridor între {bestStart} și {bestEnd}");
                            continue;
                        }

                        var roomTiles = dungeonData.GetDungeonRoomTiles();
                        var hallTiles = dungeonData.GetDungeonHallTiles();
                        List<Vector2Int> thickCorridor = ExpandCorridorThickness(corridor, roomTiles, hallTiles);

                        //Create Hall
                        Hall newHall = new Hall(Vector2Int.zero, Vector2Int.zero);
                        Vector2Int median = (bestStart + bestEnd) / 2;
                        newHall.SetHallCenter(median);
                        foreach (var tile in thickCorridor)
                        {
                            if (!dungeonData.GetDungeonHallTiles().Contains(tile))
                            {
                                newHall.AddFloorTiles(tile);
                                dungeonData.AddDungeonHallTiles(tile);
                                Vector3Int tilePosition = new Vector3Int(tile.x, tile.y, 0);
                                paths.SetTile(tilePosition, floorTile);
                            }
                        }
                        dungeonData.AddHall(newHall);
                    }
                }
            }

        }

    }

    private bool IsCorridorOk(List<List<Room>> groups, List<Vector2Int> corridor)
    {
        // 1. Construim setul de hall‐tiles actualizat cu coridorul propus
        var hallSet = new HashSet<Vector2Int>(dungeonData.GetDungeonHallTiles());
        hallSet.UnionWith(corridor);

        // 2. Pentru fiecare grup verificăm dacă are cel puțin o margine deschisă
        foreach (var group in groups)
        {
            // Colecționăm pentru fiecare direcție lista de tile-uri de margine + offset
            var right = new List<Vector2Int>();
            var left = new List<Vector2Int>();
            var up = new List<Vector2Int>();
            var down = new List<Vector2Int>();

            foreach (var room in group)
            {
                right.AddRange(room.GetRightTiles().Select(t => t + Vector2Int.right));
                left.AddRange(room.GetLeftTiles().Select(t => t + Vector2Int.left));
                up.AddRange(room.GetUpTiles().Select(t => t + Vector2Int.up));
                down.AddRange(room.GetDownTiles().Select(t => t + Vector2Int.down));
            }

            // O margine e "deschisă" dacă **niciunul** dintre tile-urile ei nu se află în hallSet
            bool rightOpen = right.All(t => !hallSet.Contains(t));
            bool leftOpen = left.All(t => !hallSet.Contains(t));
            bool upOpen = up.All(t => !hallSet.Contains(t));
            bool downOpen = down.All(t => !hallSet.Contains(t));

            // Dacă grupul nu are nicio margine deschisă, coridorul nu e ok
            if (!(rightOpen && leftOpen && upOpen && downOpen))
                return false;
        }

        // Toate grupurile au cel puțin o margine neacoperită de hol
        return true;
    }

    public bool IsRoomConnected(Room room)
    {
        foreach (var tile in room.GetFloorTiles())
        {
            foreach (var dir in DungeonData.fourDirections)
            {
                var neighborPos = tile + dir;
                if (dungeonData.GetDungeonHallTiles().Contains(neighborPos))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private List<Vector2Int> GetFreeTiles(Room room)
    {
        List<Vector2Int> freePositions = new List<Vector2Int>();
        foreach (var tile in room.GetFloorTiles())
        {
            foreach (var dir in DungeonData.fourDirections)
            {
                Vector2Int pos = tile + dir;
                if (!dungeonData.GetDungeonRoomTiles().Contains(pos))
                {
                    freePositions.Add(pos);
                }
            }
        }
        return freePositions;
    }

    private Vector2Int GetRoomExitTile(Room fromRoom, Vector2Int targetCenter)
    {
        Vector2Int fromCenter = fromRoom.GetRoomCenterPos();

        // Build candidate lists with offset for each side.
        List<Vector2Int> leftCandidates = fromRoom.GetLeftTiles()
            .Select(t => t + Vector2Int.left)
            .OrderBy(t => t.y)
            .ToList();
        List<Vector2Int> rightCandidates = fromRoom.GetRightTiles()
            .Select(t => t + Vector2Int.right)
            .OrderBy(t => t.y)
            .ToList();
        List<Vector2Int> upCandidates = fromRoom.GetUpTiles()
            .Select(t => t + Vector2Int.up)
            .OrderBy(t => t.x)
            .ToList();
        List<Vector2Int> downCandidates = fromRoom.GetDownTiles()
            .Select(t => t + Vector2Int.down)
            .OrderBy(t => t.x)
            .ToList();

        // Remove the extreme candidates from each side if there are more than 2 elements.
        if (leftCandidates.Count > 4)
            leftCandidates = leftCandidates.Skip(2).Take(leftCandidates.Count - 4).ToList();
        if (leftCandidates.Count > 2)
            leftCandidates = leftCandidates.Skip(1).Take(leftCandidates.Count - 2).ToList();
        if (rightCandidates.Count > 4)
            rightCandidates = rightCandidates.Skip(2).Take(rightCandidates.Count - 4).ToList();
        if (rightCandidates.Count > 2)
            rightCandidates = rightCandidates.Skip(1).Take(rightCandidates.Count - 2).ToList();
        if (upCandidates.Count > 4)
            upCandidates = upCandidates.Skip(2).Take(upCandidates.Count - 4).ToList();
        if (upCandidates.Count > 2)
            upCandidates = upCandidates.Skip(1).Take(upCandidates.Count - 2).ToList();
        if (downCandidates.Count > 4)
            downCandidates = downCandidates.Skip(2).Take(downCandidates.Count - 4).ToList();
        if (downCandidates.Count > 2)
            downCandidates = downCandidates.Skip(1).Take(downCandidates.Count - 2).ToList();

        // Combine the candidate lists.
        List<Vector2Int> fallbackCandidates = new List<Vector2Int>();
        fallbackCandidates.AddRange(leftCandidates);
        fallbackCandidates.AddRange(rightCandidates);
        fallbackCandidates.AddRange(upCandidates);
        fallbackCandidates.AddRange(downCandidates);

        // Filter only walkable tiles.
        List<Vector2Int> walkableCandidates = fallbackCandidates.Where(AStarPathfinder.IsWalkable).ToList();
        if (walkableCandidates.Count == 0)
        {
            Debug.LogWarning("No walkable exit points found, falling back to center.");
            return fromCenter;
        }

        // Choose the candidate that is closest to targetCenter.
        Vector2Int bestExit = walkableCandidates[0];
        int bestDist = AStarPathfinder.ManhattanDistance(bestExit, targetCenter);
        foreach (var tile in walkableCandidates)
        {
            int dist = AStarPathfinder.ManhattanDistance(tile, targetCenter);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestExit = tile;
            }
        }
        return bestExit;
    }


    private void ConnectBetweenGroupsWithAStar(List<List<Room>> roomGroups, int contorFirst, int contorLast, List<int> directions)
    {
        Debug.Log("Metoda ConnectGroupsWithAStar a fost apelata!");

        if (roomGroups.Count < 2)
            return;
        //int ran = UnityEngine.Random.Range(2, roomGroups.Count - 1);
        for (int i = contorFirst; i < contorLast - 1; i++)
        {
            List<Room> currentGroup = roomGroups[i];
            List<Room> nextGroup = roomGroups[i + 1];//2 grupuri consecutive

            // Room roomA = currentGroup[currentGroup.Count - 1];
            // dungeonData.AddRoomStartEnd(roomA);
            Room initRoom = null;
            Room closestRoom = null;
            int shortestDistance = int.MaxValue;

            foreach (Room roomA in currentGroup)
            {
                foreach (Room roomB in nextGroup)
                {
                    int dist = AStarPathfinder.ManhattanDistance(roomA.GetRoomCenterPos(), roomB.GetRoomCenterPos());
                    if (dist < shortestDistance)
                    {
                        shortestDistance = dist;
                        closestRoom = roomB;
                        initRoom = roomA;
                    }
                }
            }

            dungeonData.AddRoomStartEnd(initRoom);
            dungeonData.AddRoomStartEnd(closestRoom);

            if (closestRoom != null)
            {
                int widthStart = GetSideLength(initRoom, directions[i]);
                int widthEnd = GetSideLength(closestRoom, (directions[i] + 2) % 4);

                if (widthStart <= widthEnd)
                {
                    Vector2Int proposedStart = GetRoomMiddleTile(initRoom, directions[i]);
                    Vector2Int start = proposedStart;
                    Vector2Int end = GetRoomExitTile(closestRoom, proposedStart);
                    Vector2Int median = (start + end) / 2;
                    //  Debug.Log($"(Normala) START: {start}, END: {end}");

                    int corridorWidth = widthStart;

                    List<Vector2Int> corridor = AStarPathfinder.AStarPathfindingExtended(start, end);

                    dungeonData.AddLenghtStartEnd(corridor.Count);

                    if (corridor == null || corridor.Count == 0)
                    {
                        Debug.LogWarning($"Nu s-a putut genera coridor între {start} și {end}");
                    }

                    if (start.x == end.x || start.y == end.y)
                    {
                        CreateNewHall(corridor, corridorWidth, directions[i], median);
                    }
                    else
                    {
                        Debug.LogWarning($"Start {start} și End {end} nu sunt pe aceeași axă, sar peste CreateNewHall");
                    }

                }
                else
                {
                    int reverseDir = (directions[i] + 2) % 4;

                    Vector2Int proposedStart = GetRoomMiddleTile(closestRoom, reverseDir);
                    Vector2Int start = proposedStart;
                    Vector2Int end = GetRoomExitTile(initRoom, proposedStart);
                    Vector2Int median = (start + end) / 2;
                    //  Debug.Log($"(Invers) START: {start}, END: {end}");

                    int corridorWidth = widthEnd;

                    List<Vector2Int> corridor = AStarPathfinder.AStarPathfindingExtended(start, end);

                    dungeonData.AddLenghtStartEnd(corridor.Count);

                    if (corridor == null || corridor.Count == 0)
                    {
                        Debug.LogWarning($"Nu s-a putut genera coridor între {start} și {end}");
                    }

                    if (start.x == end.x || start.y == end.y)
                    {
                        CreateNewHall(corridor, corridorWidth, reverseDir, median);
                    }
                    else
                    {
                        Debug.LogWarning($"Start {start} și End {end} nu sunt pe aceeași axă, sar peste CreateNewHall");
                    }

                }
            }
        }
    }

    private Vector2Int GetRoomMiddleTile(Room fromRoom, int direction)
    {
        Vector2Int dimensions = fromRoom.GetDimensions();
        Vector2Int fromCenter = fromRoom.GetRoomCenterPos();
        switch (direction)
        {
            case 0: // UP
                if (dimensions.y % 2 != 0)
                {
                    return new Vector2Int(fromCenter.x, fromCenter.y + (dimensions.y - 1) / 2);
                }
                return new Vector2Int(fromCenter.x, fromCenter.y + dimensions.y / 2);
            case 1: // LEFT
                if (dimensions.x % 2 != 0)
                {
                    return new Vector2Int(fromCenter.x - (dimensions.x - 1) / 2, fromCenter.y);
                }
                return new Vector2Int(fromCenter.x - dimensions.x / 2, fromCenter.y);
            case 2: // DOWN
                if (dimensions.y % 2 != 0)
                {
                    return new Vector2Int(fromCenter.x, fromCenter.y - (dimensions.y - 1) / 2 - 1);
                }
                return new Vector2Int(fromCenter.x, fromCenter.y - dimensions.y / 2);
            case 3: // RIGHT
                if (dimensions.x % 2 != 0)
                {
                    return new Vector2Int(fromCenter.x + (dimensions.x - 1) / 2 + 1, fromCenter.y);
                }
                return new Vector2Int(fromCenter.x + dimensions.x / 2, fromCenter.y);
        }

        return fromCenter;
    }

    private void CreateNewHall(List<Vector2Int> corridor, int corridorWidth, int direction, Vector2Int hallCenter)
    {
        Hall hall = new Hall(Vector2Int.zero, Vector2Int.zero); //nu am nevoie de dimensiunea holului
        int half;
        bool isOdd = corridorWidth % 2 != 0;

        if (isOdd)
        {
            half = (corridorWidth - 1) / 2;
        }
        else
        {
            half = corridorWidth / 2;
        }

        if (isOdd) half = (corridorWidth - 1) / 2;

        if (direction == 1 || direction == 3) //LEFT-RIGHT hall
        {
            foreach (Vector2Int pos in corridor)
            {
                for (int y = -half - (isOdd ? 1 : 0); y < half; y++)
                {
                    Vector2Int newPosition = pos + new Vector2Int(0, y);
                    if (!dungeonData.GetDungeonRoomTiles().Contains(newPosition) && !dungeonData.GetDungeonHallTiles().Contains(newPosition))
                    {
                        hall.AddFloorTiles(newPosition);
                        dungeonData.AddDungeonHallTiles(newPosition);

                        Vector3Int tilePosition = new Vector3Int(newPosition.x, newPosition.y, 0);
                        paths.SetTile(tilePosition, floorTile);
                    }
                }
            }
        }
        else //UP-DOWN hall
        {
            foreach (Vector2Int pos in corridor)
            {
                for (int x = -half; x < half + (isOdd ? 1 : 0); x++)
                {
                    Vector2Int newPosition = pos + new Vector2Int(x, 0);
                    if (!dungeonData.GetDungeonRoomTiles().Contains(newPosition) && !dungeonData.GetDungeonHallTiles().Contains(newPosition))
                    {
                        hall.AddFloorTiles(newPosition);
                        dungeonData.AddDungeonHallTiles(newPosition);

                        Vector3Int tilePosition = new Vector3Int(newPosition.x, newPosition.y, 0);
                        paths.SetTile(tilePosition, floorTile);
                    }
                }
            }
        }

        hall.SetHallCenter(hallCenter);
        dungeonData.AddHall(hall);
    }


    public int GetSideLength(Room room, int dir)
    {
        return dir switch
        {
            0 => room.GetUpTiles().Count,
            1 => room.GetRightTiles().Count,
            2 => room.GetDownTiles().Count,
            3 => room.GetLeftTiles().Count,
            _ => 1
        };
    }

    private void ConnectDisjointHalls(List<Hall> halls)
    {
        Debug.Log("Metoda ConnectDisjointHalls a fost apelata!");

        if (halls == null || halls.Count == 0)
            return;

        while (halls.Count > 1)
        {
            Hall hallA = halls[0];
            halls.RemoveAt(0);

            Hall closestHall = null;
            int shortestDistance = int.MaxValue;

            for (int i = 0; i < halls.Count; i++)
            {
                Hall hallB = halls[i];
                int dist = AStarPathfinder.ManhattanDistance(hallA.GetHallCenter(), hallB.GetHallCenter());
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    closestHall = hallB;
                }
            }

            if (closestHall != null)
            {
                halls.Remove(closestHall);

                List<Vector2Int> hallATiles = hallA.GetFloorTiles();
                List<Vector2Int> hallBTiles = closestHall.GetFloorTiles();

                // Verificăm că ambele liste au cel puțin un element
                if (hallATiles.Count == 0 || hallBTiles.Count == 0)
                {
                    Debug.LogWarning("Unul dintre holuri nu are tile-uri, se sare peste conectare.");
                    continue;
                }

                Vector2Int bestStart = hallATiles[0];
                Vector2Int bestEnd = hallBTiles[0];
                int bestDist = int.MaxValue;
                foreach (var cTile in hallATiles)
                {
                    foreach (var hTile in hallBTiles)
                    {
                        int dist = AStarPathfinder.ManhattanDistance(cTile, hTile);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestStart = cTile;
                            bestEnd = hTile;
                        }
                    }
                }
                Debug.Log("start: " + bestStart);
                Debug.Log("end: " + bestEnd);

                List<Vector2Int> corridor = AStarPathfinder.AStarPathfindingExtended(bestStart, bestEnd);
                /*foreach (var tile in corridor)
                {
                    Debug.Log("tile: " + tile);
                }*/

                if (corridor == null || corridor.Count == 0)
                {
                    Debug.LogWarning($"Nu s-a putut genera coridor între {bestStart} și {bestEnd}");
                    continue;
                }


                List<Vector2Int> thickCorridor = ExpandCorridorThickness(corridor, dungeonData.GetDungeonRoomTiles(), dungeonData.GetDungeonHallTiles());

                //Create Hall
                Hall newHall = new Hall(Vector2Int.zero, Vector2Int.zero);
                Vector2Int median = (bestStart + bestEnd) / 2;
                newHall.SetHallCenter(median);
                foreach (var tile in thickCorridor)
                {
                    if (!dungeonData.GetDungeonHallTiles().Contains(tile))
                    {
                        newHall.AddFloorTiles(tile);
                        dungeonData.AddDungeonHallTiles(tile);
                        Vector3Int tilePosition = new Vector3Int(tile.x, tile.y, 0);
                        paths.SetTile(tilePosition, floorTile);
                    }
                }
                dungeonData.AddHall(newHall);


                //Adăugăm newHall la începutul listei pentru ca la următoarea iterație, hallA să fie newHall (adică, cel ce a fost înainte closestHall).
                halls.Insert(0, closestHall);
            }
        }
    }


    public static List<Vector2Int> ExpandCorridorThickness(
    List<Vector2Int> corridor,
    IEnumerable<Vector2Int> roomTilesEnumerable,
    IEnumerable<Vector2Int> hallTilesEnumerable)
    {
        var roomTiles = new HashSet<Vector2Int>(roomTilesEnumerable);
        var hallTiles = new HashSet<Vector2Int>(hallTilesEnumerable);
        var thickCorridor = new HashSet<Vector2Int>(corridor);

        // cele 4 direcții cardinale
        var directions = new[]
        {
        Vector2Int.up,
        Vector2Int.right,
        Vector2Int.down,
        Vector2Int.left
    };

        // pentru fiecare tile existent în coridor
        foreach (var tile in corridor)
        {
            // încercăm să adăugăm câte un tile în fiecare direcție
            foreach (var dir in directions)
            {
                var neighbor = tile + dir;

                // doar dacă nu ajunge în camere sau holuri și nu e deja în coridor
                if (!roomTiles.Contains(neighbor)
                 && !hallTiles.Contains(neighbor)
                 && !thickCorridor.Contains(neighbor))
                {
                    thickCorridor.Add(neighbor);
                }
            }
        }

        return thickCorridor.ToList();
    }


    private void ExtendHallToMaxForAllRooms()
    {
        var hallSet = new HashSet<Vector2Int>(dungeonData.GetDungeonHallTiles());

        foreach (var room in dungeonData.GetRooms())
        {
            // calculează distanțele per margine
            var (leftDistances, rightDistances, upDistances, downDistances)
                = ComputeHallDistances(room, hallSet);

            // listările de tile‑uri de margine
            var leftTiles = room.GetLeftTiles();
            var rightTiles = room.GetRightTiles();
            var upTiles = room.GetUpTiles();
            var downTiles = room.GetDownTiles();

            // află maximul pe fiecare direcție
            int maxLeft = leftDistances.Any() ? leftDistances.Max() : 0;
            int maxRight = rightDistances.Any() ? rightDistances.Max() : 0;
            int maxUp = upDistances.Any() ? upDistances.Max() : 0;
            int maxDown = downDistances.Any() ? downDistances.Max() : 0;

            // extinde holul la stânga
            for (int i = 0; i < leftTiles.Count; i++)
            {
                if (leftDistances[i] > 0)
                {
                    var start = leftTiles[i];
                    for (int step = 1; step <= maxLeft; step++)
                    {
                        var t = start + Vector2Int.left * step;
                        if (dungeonData.GetDungeonRoomTiles().Contains(t))
                        {
                            break;
                        }
                        if (!dungeonData.GetDungeonHallTiles().Contains(t))
                        {
                            paths.SetTile((Vector3Int)t, floorTile);
                            dungeonData.AddDungeonHallTiles(t);
                        }
                    }
                }
            }

            // extinde holul la dreapta
            for (int i = 0; i < rightTiles.Count; i++)
            {
                if (rightDistances[i] > 0)
                {
                    var start = rightTiles[i];
                    for (int step = 1; step <= maxRight; step++)
                    {
                        var t = start + Vector2Int.right * step;
                        if (dungeonData.GetDungeonRoomTiles().Contains(t))
                        {
                            break;
                        }
                        if (!dungeonData.GetDungeonHallTiles().Contains(t))
                        {
                            paths.SetTile((Vector3Int)t, floorTile);
                            dungeonData.AddDungeonHallTiles(t);
                        }
                    }
                }
            }

            // extinde holul în sus
            for (int i = 0; i < upTiles.Count; i++)
            {
                if (upDistances[i] > 0)
                {
                    var start = upTiles[i];
                    for (int step = 1; step <= maxUp; step++)
                    {
                        var t = start + Vector2Int.up * step;
                        if (dungeonData.GetDungeonRoomTiles().Contains(t))
                        {
                            break;
                        }
                        if (!dungeonData.GetDungeonHallTiles().Contains(t))
                        {
                            paths.SetTile((Vector3Int)t, floorTile);
                            dungeonData.AddDungeonHallTiles(t);
                        }
                    }
                }
            }

            // extinde holul în jos
            for (int i = 0; i < downTiles.Count; i++)
            {
                if (downDistances[i] > 0)
                {
                    var start = downTiles[i];
                    for (int step = 1; step <= maxDown; step++)
                    {
                        var t = start + Vector2Int.down * step;
                        if (dungeonData.GetDungeonRoomTiles().Contains(t))
                        {
                            break;
                        }
                        if (!dungeonData.GetDungeonHallTiles().Contains(t))
                        {
                            paths.SetTile((Vector3Int)t, floorTile);
                            dungeonData.AddDungeonHallTiles(t);
                        }
                    }
                }
            }
        }
    }

    private (List<int> Left, List<int> Right, List<int> Up, List<int> Down) ComputeHallDistances(Room room, HashSet<Vector2Int> hallTiles)
    {
        var leftDistances = new List<int>();
        var rightDistances = new List<int>();
        var upDistances = new List<int>();
        var downDistances = new List<int>();

        // pentru fiecare tile de pe marginea dreaptă
        foreach (var t in room.GetRightTiles())
            rightDistances.Add(CountStepsInDirection(t, Vector2Int.right, hallTiles));

        // pentru marginea stângă
        foreach (var t in room.GetLeftTiles())
            leftDistances.Add(CountStepsInDirection(t, Vector2Int.left, hallTiles));

        // marginea de sus
        foreach (var t in room.GetUpTiles())
            upDistances.Add(CountStepsInDirection(t, Vector2Int.up, hallTiles));

        // marginea de jos
        foreach (var t in room.GetDownTiles())
            downDistances.Add(CountStepsInDirection(t, Vector2Int.down, hallTiles));

        return (leftDistances, rightDistances, upDistances, downDistances);
    }

    private int CountStepsInDirection(Vector2Int start, Vector2Int dir, HashSet<Vector2Int> hallTiles)
    {
        int count = 0;
        var cursor = start + dir;
        while (hallTiles.Contains(cursor))
        {
            count++;
            cursor += dir;
        }
        return count;
    }

    //Place kitchen counter:
    public struct WallData
    {
        public Vector2Int Side; //Direcția peretelui (de ex.: Vector2Int.up, Vector2Int.left, etc.)
        public List<Vector2Int> Tiles; //Tile‑urile aferente peretelui
    }

    private int CountFreeNeighbors(List<Vector2Int> list)
    {
        List<Vector2Int> candidates = new List<Vector2Int>() { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };
        int freeTiles = 0;

        foreach (var tile in list)
        {
            foreach (var candidate in candidates)
            {
                Vector2Int neighbor = tile + candidate;
                if (!dungeonData.GetDungeonAllTiles().Contains(neighbor))
                {
                    freeTiles += 1;
                }
            }
        }
        return freeTiles;
    }

    private List<WallData> FindExteriorWalls(List<Room> rooms)
    {
        List<WallData> walls = new List<WallData>();

        foreach (Room room in rooms)
        {
            if (room.GetRoomType() != RoomType.Bucatarie)
                continue;

            List<WallData> wallSides = new List<WallData>()
            {
                new WallData { Side = Vector2Int.up,    Tiles = room.GetUpTiles() },
                new WallData { Side = Vector2Int.left,  Tiles = room.GetLeftTiles() },
                new WallData { Side = Vector2Int.down,  Tiles = room.GetDownTiles() },
                new WallData { Side = Vector2Int.right, Tiles = room.GetRightTiles() }
            };

            int maxFreeNeighbors = -1;
            WallData bestWall = new WallData();

            foreach (var wallData in wallSides)
            {
                int freeCount = CountFreeNeighbors(wallData.Tiles);
                if (freeCount > maxFreeNeighbors)
                {
                    maxFreeNeighbors = freeCount;
                    bestWall = wallData;
                }
            }
            walls.Add(bestWall);
        }
        return walls;
    }


    private void DrawOnKitchenWalls(List<WallData> wallDatas)
    {
        for (int i = 0; i < wallDatas.Count; i++)
        {
            WallData wallData = wallDatas[i];
            float angle = 0f;
            if (wallData.Side == Vector2Int.up)
                angle = 0f;
            if (wallData.Side == Vector2Int.right)
                angle = -90f;
            if (wallData.Side == Vector2Int.left)
                angle = 90f;
            if (wallData.Side == Vector2Int.down)
                angle = 180f;

            Quaternion rotation = Quaternion.Euler(0, 0, angle);
            Matrix4x4 transformMatrix = Matrix4x4.TRS(Vector3.zero, rotation, Vector3.one);

            foreach (Vector2Int tile in wallData.Tiles)
            {
                Vector3Int tilePosition = new Vector3Int(tile.x, tile.y, 0);
                objects.SetTile(tilePosition, counter);
                // Aplicăm transformarea pentru rotație
                objects.SetTransformMatrix(tilePosition, transformMatrix);
            }

            Vector2Int sinkPosition = new Vector2Int(wallData.Tiles[wallData.Tiles.Count - 2].x, wallData.Tiles[wallData.Tiles.Count - 2].y);
            Vector3Int pos = new Vector3Int(sinkPosition.x, sinkPosition.y, 0);
            overObjects.SetTile(pos, sink);
            // Aplicăm transformarea pentru rotație
            overObjects.SetTransformMatrix(pos, transformMatrix);
        }
    }


    private void ClearRoomTiles()
    {
        objects.ClearAllTiles();
        overObjects.ClearAllTiles();
        roomMap.ClearAllTiles();//pt a evita duplicarea
        paths.ClearAllTiles();
        rightBoundary.ClearAllTiles();
        upBoundary.ClearAllTiles();
        leftBoundary.ClearAllTiles();
        downBoundary.ClearAllTiles();
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

        foreach (Vector2Int posRoom in dungeonData.GetDungeonRoomTiles())
        {
            Vector2Int leftWallTile = posRoom + Vector2Int.left;//left wall all tiles
            if (dungeonData.VerifyDungeonRoomTile(leftWallTile) == false)
            {
                dungeonData.AddDungeonLeftWallTiles(leftWallTile);
                //leftWalls.SetTile((Vector3Int)leftWallTile, leftWall);
                if (IsRoomTileWithoutHallNeighbor(leftWallTile))
                    leftWalls.SetTile(new Vector3Int(leftWallTile.x, leftWallTile.y, 0), leftWall);

            }

            Vector2Int rightWallTile = posRoom + Vector2Int.right;//right wall all tiles
            if (dungeonData.VerifyDungeonRoomTile(rightWallTile) == false)
            {
                dungeonData.AddDungeonRightWallTiles(rightWallTile);
                //rightWalls.SetTile((Vector3Int)rightWallTile, rightWall);
                if (IsRoomTileWithoutHallNeighbor(rightWallTile))
                    rightWalls.SetTile(new Vector3Int(rightWallTile.x, rightWallTile.y, 0), rightWall);

            }

            Vector2Int upWallTile = posRoom + Vector2Int.up;//up wall all tiles
            if (dungeonData.VerifyDungeonRoomTile(upWallTile) == false)
            {
                dungeonData.AddDungeonUpWallTiles(upWallTile);
                //upWalls.SetTile((Vector3Int)upWallTile, upWall);
                if (IsRoomTileWithoutHallNeighbor(upWallTile))
                    upWalls.SetTile(new Vector3Int(upWallTile.x, upWallTile.y, 0), upWall);

            }

            Vector2Int downWallTile = posRoom + Vector2Int.down;//down wall all tiles
            if (dungeonData.VerifyDungeonRoomTile(downWallTile) == false)
            {
                dungeonData.AddDungeonDownWallTiles(downWallTile);
                //downWalls.SetTile((Vector3Int)downWallTile, downWall);
                if (IsRoomTileWithoutHallNeighbor(downWallTile))
                    downWalls.SetTile(new Vector3Int(downWallTile.x, downWallTile.y, 0), downWall);

            }
        }
    }

    public bool IsRoomTileWithoutHallNeighbor(Vector2Int roomTile)
    {
        HashSet<Vector2Int> hallTiles = dungeonData.GetDungeonHallTiles();

        if (hallTiles.Contains(roomTile))
        {
            //Dacă unul dintre vecini este un hall tile, returnăm false
            return false;
        }


        //Dacă niciun vecin nu este hall tile, returnăm true
        return true;
    }

    private void AdjustCameraToDungeon()
    {
        if (dungeonData == null || dungeonData.GetRooms().Count == 0)
        {
            Debug.LogError("DungeonData nu conține camere!");
            return;
        }

        // Inițializăm limitele (Bounds) folosind prima cameră
        Vector2Int firstRoomCenter = dungeonData.GetRooms()[0].GetRoomCenterPos();
        Bounds bounds = new Bounds(new Vector3(firstRoomCenter.x, firstRoomCenter.y, 0), Vector3.zero);

        // Extindem limitele pentru toate camerele
        foreach (var room in dungeonData.GetRooms())
        {
            Vector2Int roomCenter = room.GetRoomCenterPos();
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
