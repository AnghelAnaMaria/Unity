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
            dungeonData.GenerateDungeonHallTiles();
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

        PlaceRoomsProcedurally(listRooms);

        //ConnectFirstGroupWithAStar(dungeonData.GetListRoomGroups(), dungeonData.GetDirections());//merge

        // ConnectBetweenGroupsWithAStar(dungeonData.GetListRoomGroups(), dungeonData.GetDirections());//merge
        //ConnectRoomsWithClosestHall(dungeonData.GetListRoomGroups(), dungeonData.GetHalls());
        // ConnectDisjointHalls(dungeonData.GetHalls());


        foreach (Room room in dungeonData.GetRooms())
        {
            foreach (Vector2Int tile in room.GetFloorTiles())
            {
                dungeonData.AddDungeonAllTiles(tile);
            }
        }

        foreach (Hall hall in dungeonData.GetHalls())
        {
            foreach (Vector2Int tile in hall.GetFloorTiles())
            {
                dungeonData.AddDungeonAllTiles(tile);
            }
        }

        //FillInteriorHoles();

        //Kitchen:
        //  List<WallData> walls = FindExteriorWalls(dungeonData.GetRooms());
        //  DrawOnKitchenWalls(walls);


    }

    private void PlaceRoomsProcedurally(List<List<Room>> roomGroups)
    {
        Dictionary<Room, Vector2Int> roomPositions = new Dictionary<Room, Vector2Int>();
        HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

        Vector2Int currentPosition = Vector2Int.zero;
        int lastGroupDirection = -1;
        List<int> possibleDirections = new List<int> { 0, 1, 2, 3 };//sus, stanga, jos, dreapta
        int groupDirection = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];

        for (int i = 0; i < roomGroups.Count; i++)
        {
            // groupDirection = (groupDirection + 1) % 4;
            List<Room> newGroup = new List<Room>();
            if (roomGroups[i].Count == 0) continue;

            // List<int> possibleDirections = new List<int> { 0, 1, 2, 3 };//sus, stanga, jos, dreapta

            currentPosition = currentPosition + roomGroups[i][0].AddToCurrentPosition(lastGroupDirection);

            if (lastGroupDirection != -1) //Dacă avem deja un grup plasat
            {
                int oppositeDirection = (lastGroupDirection + 2) % 4;
                possibleDirections.Remove(oppositeDirection); //Eliminăm direcția inversă
            }

            //Alegem o direcție aleatorie dintre cele valide
            groupDirection = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];

            //Setăm prima cameră din grup ca punct de referință
            Room firstRoom = roomGroups[i][0];
            Vector2Int validPosition = FindValidRoomPosition(currentPosition, firstRoom, occupiedPositions, false);
            firstRoom.SetRoomCenter(validPosition);
            roomGroups[i][0].SetRoomCenter(validPosition);

            Debug.Log("firstRoom.SkipBoundaryWalls: " + firstRoom.GetSkipBoundaryWalls());
            Room roomCreated = CreateRectangularRoomAt(firstRoom.GetRoomCenterPos(), firstRoom.GetRoomType(), firstRoom.GetDimensions(), firstRoom.GetSkipBoundaryWalls());
            //roomCreated.SkipBoundaryWalls = firstRoom.SkipBoundaryWalls;
            // Debug.Log("roomCreated.SkipBoundaryWalls: " + roomCreated.SkipBoundaryWalls);
            newGroup.Add(roomCreated);


            roomPositions[firstRoom] = validPosition;
            occupiedPositions.UnionWith(roomCreated.GetFloorTiles()); //Track occupied tiles

            Room lastRoom = firstRoom; //Ținem minte ultima cameră plasată

            //Plasăm restul camerelor din grup în aceeași direcție
            for (int j = 1; j < roomGroups[i].Count; j++)
            {
                Room currentRoom = roomGroups[i][j];
                Room neighborRoom = roomGroups[i][j - 1];

                (Vector2Int positionInter, int direction) = GetNeighborPosition(neighborRoom, currentRoom, groupDirection);
                groupDirection = possibleDirections[UnityEngine.Random.Range(0, possibleDirections.Count)];
                // groupDirection = (groupDirection + 1) % 4;
                /*if (lastGroupDirection != -1) //Dacă avem deja un grup plasat
                {
                    int oppositeDirection = (lastGroupDirection + 2) % 4;
                    possibleDirections.Remove(oppositeDirection); //Eliminăm direcția inversă
                }*/

                // **Ensure no collision before placing**
                Vector2Int position = FindValidRoomPosition(positionInter, currentRoom, occupiedPositions, true);
                currentRoom.SetRoomCenter(position);
                roomGroups[i][j].SetRoomCenter(position);

                Debug.Log("currentRoom.SkipBoundaryWalls: " + currentRoom.GetSkipBoundaryWalls());
                Room groupRoomCreated = CreateRectangularRoomAt(currentRoom.GetRoomCenterPos(), currentRoom.GetRoomType(), currentRoom.GetDimensions(), currentRoom.GetSkipBoundaryWalls());
                // groupRoomCreated.SkipBoundaryWalls = currentRoom.SkipBoundaryWalls;
                // Debug.Log("groupRoomCreated.SkipBoundaryWalls: " + groupRoomCreated.SkipBoundaryWalls);
                newGroup.Add(groupRoomCreated);

                roomPositions[currentRoom] = position;
                occupiedPositions.UnionWith(groupRoomCreated.GetFloorTiles());
                lastRoom = currentRoom;

            }
            dungeonData.AddGroup(newGroup);

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
                        // Vector2Int nextPosition = position + candidate;
                        // if (!occupiedPositions.Contains(nextPosition) && !dungeonData.GetDungeonRoomTiles().Contains(nextPosition))
                        {
                            // occupiedPositions.Add(nextPosition);
                        }
                    }
                }
                /* var extra = new[]
                 {
                 new Vector2Int( 2, 1), new Vector2Int( 1, 2),
                 new Vector2Int(-2, 1), new Vector2Int(-1, 2),
                 new Vector2Int( 2,-1), new Vector2Int( 1,-2),
                 new Vector2Int(-2,-1), new Vector2Int(-1,-2),
                 };
                 foreach (var tile in tiles)
                 {
                     foreach (var e in extra)
                         if (!occupiedPositions.Contains(tile + e) && !dungeonData.GetDungeonRoomTiles().Contains(tile + e))
                             occupiedPositions.Add(tile + e);
                 }*/
            }

            lastGroupDirection = groupDirection;
            dungeonData.AddDirection(lastGroupDirection);

            // **Update next group position based on last room size**
            currentPosition = CalculateNextGroupStartPosition(lastRoom, groupDirection);
        }
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
    private void ConnectFirstGroupWithAStar(List<List<Room>> roomGroups, List<int> directions)
    {
        Debug.Log("Metoda ConnectGroupsWithAStar a fost apelata!");

        if (roomGroups.Count < 2)
            return;

        for (int i = 0; i < roomGroups.Count - 1; i++)
        {
            List<Room> currentGroup = roomGroups[0];
            List<Room> nextGroup = roomGroups[i + 1];//2 grupuri consecutive

            Room roomA = currentGroup[0];
            dungeonData.AddRoomStartEnd(roomA);
            Room closestRoom = null;
            int shortestDistance = int.MaxValue;

            foreach (Room roomB in nextGroup)
            {
                int dist = AStarPathfinder.ManhattanDistance(roomA.GetRoomCenterPos(), roomB.GetRoomCenterPos());
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    closestRoom = roomB;
                }
            }
            dungeonData.AddRoomStartEnd(closestRoom);

            if (closestRoom != null)
            {
                Vector2Int start = GetRoomExitTile(roomA, closestRoom.GetRoomCenterPos());
                Vector2Int end = GetRoomExitTile(closestRoom, roomA.GetRoomCenterPos());
                Vector2Int median = (Vector2Int)(start + end) / 2;
                Debug.Log($"START: {start}, END: {end}");

                //int corridorWidth = UnityEngine.Random.Range(2, 4);

                List<Vector2Int> corridor = AStarPathfinder.AStarPathfindingExtended(start, end);

                dungeonData.AddLenghtStartEnd(corridor.Count);

                if (corridor == null || corridor.Count == 0)
                {
                    Debug.LogWarning($"Nu s-a putut genera coridor între {start} și {end}");
                }

                //Create Hall
                Hall hall = new Hall(Vector2Int.zero, Vector2Int.zero);
                // List<Vector2Int> thickCorridor = ExpandCorridorThickness(corridor, dungeonData.GetDungeonRoomTiles(), dungeonData.GetDungeonHalliles());
                foreach (Vector2Int pos in corridor)
                {
                    if (!dungeonData.GetDungeonHalliles().Contains(pos))
                    {
                        hall.AddFloorTiles(pos);
                        dungeonData.AddDungeonHallTiles(pos);

                        Vector3Int tilePosition = new Vector3Int(pos.x, pos.y, 0);
                        paths.SetTile(tilePosition, floorTile);
                    }
                }

                hall.SetHallCenter(median);
                dungeonData.AddHall(hall);
                RectangularizeHall(hall);

            }
        }

        /*
        List<Room> lastGroup = roomGroups[roomGroups.Count - 1];
        List<Room> secondLastGroup = roomGroups[roomGroups.Count - 2];
        if (lastGroup.Count > 1)//daca ultimul grup are mai multe camere
        {
            Room lastRoom = lastGroup[lastGroup.Count - 1];
            dungeonData.AddRoomStartEnd(lastRoom);

            Room closestRoom = null;
            int shortestDistance = int.MaxValue;

            foreach (Room roomB in secondLastGroup)
            {
                int dist = AStarPathfinder.ManhattanDistance(lastRoom.GetRoomCenterPos(), roomB.GetRoomCenterPos());
                if (dist < shortestDistance)
                {
                    shortestDistance = dist;
                    closestRoom = roomB;
                }
            }
            dungeonData.AddRoomStartEnd(closestRoom);

            if (closestRoom != null)
            {
                Vector2Int start = GetRoomExitTile(lastRoom, closestRoom.GetRoomCenterPos());
                Vector2Int end = GetRoomExitTile(closestRoom, lastRoom.GetRoomCenterPos());
                Vector2Int median = (Vector2Int)(start + end) / 2;
                Debug.Log($"START: {start}, END: {end}");

                //int corridorWidth = UnityEngine.Random.Range(2, 4);

                List<Vector2Int> corridor = AStarPathfinder.AStarPathfindingExtended(start, end);

                dungeonData.AddLenghtStartEnd(corridor.Count);

                if (corridor == null || corridor.Count == 0)
                {
                    Debug.LogWarning($"Nu s-a putut genera coridor între {start} și {end}");
                }

                //Create Hall
                Hall hall = new Hall(Vector2Int.zero, Vector2Int.zero);
                // List<Vector2Int> thickCorridor = ExpandCorridorThickness(corridor, dungeonData.GetDungeonRoomTiles());
                foreach (Vector2Int pos in corridor)
                {
                    if (!dungeonData.GetDungeonHalliles().Contains(pos))
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

        }*/
    }

    private void ConnectRoomsWithClosestHall(List<List<Room>> groups, List<Hall> halls)
    {
        if (groups.Count < 1)
            return;

        for (int i = 0; i < groups.Count - 1; i++)
        {
            List<Room> currentGroup = groups[i];

            for (int j = currentGroup.Count - 1; j >= 0; j--)
            {
                Room roomA = currentGroup[j];
                if (!dungeonData.GetRoomsStartEnd().Contains(roomA) && !IsRoomConnected(roomA))
                {
                    Hall closestHall = null;
                    int shortestDistance = int.MaxValue;

                    foreach (Hall hallB in halls)
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
                        var hallTiles = dungeonData.GetDungeonHalliles();
                        List<Vector2Int> thickCorridor = ExpandCorridorThickness(corridor, roomTiles, hallTiles);

                        //Create Hall
                        Hall newHall = new Hall(Vector2Int.zero, Vector2Int.zero);
                        Vector2Int median = (bestStart + bestEnd) / 2;
                        newHall.SetHallCenter(median);
                        foreach (var tile in thickCorridor)
                        {
                            if (!dungeonData.GetDungeonHalliles().Contains(tile))
                            {
                                newHall.AddFloorTiles(tile);
                                dungeonData.AddDungeonHallTiles(tile);
                                Vector3Int tilePosition = new Vector3Int(tile.x, tile.y, 0);
                                paths.SetTile(tilePosition, floorTile);
                            }
                        }
                        dungeonData.AddHall(newHall);
                        //RectangularizeHall(newHall);

                    }
                }
            }

        }

    }

    public bool IsRoomConnected(Room room)
    {
        foreach (var tile in room.GetFloorTiles())
        {
            foreach (var dir in DungeonData.fourDirections)
            {
                var neighborPos = tile + dir;
                if (dungeonData.GetDungeonHalliles().Contains(neighborPos))
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
        if (leftCandidates.Count > 2)
            leftCandidates = leftCandidates.Skip(1).Take(leftCandidates.Count - 2).ToList();
        if (rightCandidates.Count > 2)
            rightCandidates = rightCandidates.Skip(1).Take(rightCandidates.Count - 2).ToList();
        if (upCandidates.Count > 2)
            upCandidates = upCandidates.Skip(1).Take(upCandidates.Count - 2).ToList();
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


    private void ConnectBetweenGroupsWithAStar(List<List<Room>> roomGroups, List<int> directions)
    {
        Debug.Log("Metoda ConnectGroupsWithAStar a fost apelata!");

        if (roomGroups.Count < 2)
            return;
        //int ran = UnityEngine.Random.Range(2, roomGroups.Count - 1);
        for (int i = 0; i < roomGroups.Count - 1; i++)
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
                    if (!dungeonData.GetDungeonRoomTiles().Contains(newPosition) && !dungeonData.GetDungeonHalliles().Contains(newPosition))
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
                    if (!dungeonData.GetDungeonRoomTiles().Contains(newPosition) && !dungeonData.GetDungeonHalliles().Contains(newPosition))
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

                var roomTiles = dungeonData.GetDungeonRoomTiles();
                // List<Vector2Int> thickCorridor = ExpandCorridorThickness(corridor, roomTiles);

                //Create Hall
                Hall newHall = new Hall(Vector2Int.zero, Vector2Int.zero);
                Vector2Int median = (bestStart + bestEnd) / 2;
                newHall.SetHallCenter(median);
                foreach (var tile in corridor)
                {
                    if (!dungeonData.GetDungeonHalliles().Contains(tile))
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


    public static List<Vector2Int> ExpandCorridorThickness(List<Vector2Int> corridor, IEnumerable<Vector2Int> roomTilesEnumerable, IEnumerable<Vector2Int> hallTilesEnumerable)
    {
        // Convertim roomTiles într-un HashSet pentru verificări rapide.
        HashSet<Vector2Int> roomTiles = new HashSet<Vector2Int>(roomTilesEnumerable);
        HashSet<Vector2Int> hallTiles = new HashSet<Vector2Int>(hallTilesEnumerable);
        // Începem cu drumul original (grosime 1)
        HashSet<Vector2Int> thickCorridor = new HashSet<Vector2Int>(corridor);

        // 1. Definim cele 4 direcții posibile: 
        // index 0 -> up, 1 -> left, 2 -> down, 3 -> right.
        List<Vector2Int> allCandidates = new List<Vector2Int>()
        {
            Vector2Int.up,    // index 0
            Vector2Int.left,  // index 1
            Vector2Int.down,  // index 2
            Vector2Int.right, // index 3
        };

        // 2. Calculăm numărul de tile-uri libere pentru fiecare direcție candidat
        Dictionary<Vector2Int, int> candidateCounts = new Dictionary<Vector2Int, int>();
        foreach (var candidate in allCandidates)
        {
            int count = corridor.Count(tile => !roomTiles.Contains(tile + candidate));
            candidateCounts[candidate] = count;
        }

        // 3. Alegem primary candidate - direcția cu cel mai mare count.
        Vector2Int primaryCandidate = candidateCounts.Aggregate((maxPair, nextPair) =>
            nextPair.Value > maxPair.Value ? nextPair : maxPair).Key;

        // 4. Eliminăm primary candidate și opusul său din lista de candidați.
        int primaryIndex = allCandidates.IndexOf(primaryCandidate);
        Vector2Int oppositeCandidate = allCandidates[(primaryIndex + 2) % 4];
        allCandidates.Remove(primaryCandidate);
        allCandidates.Remove(oppositeCandidate);

        // 5. Din cei doi candidați rămași, alegem pe cel cu free count maxim ca secondary candidate.
        Dictionary<Vector2Int, int> secondaryCounts = new Dictionary<Vector2Int, int>();
        foreach (var candidate in allCandidates)
        {
            int count = corridor.Count(tile => !roomTiles.Contains(tile + candidate));
            secondaryCounts[candidate] = count;
        }
        Vector2Int secondaryCandidate = secondaryCounts.Aggregate((maxPair, nextPair) =>
            nextPair.Value > maxPair.Value ? nextPair : maxPair).Key;

        // 6. Extindem drumul:
        foreach (var tile in corridor)
        {
            // Adăugăm tile-uri în direcția primaryCandidate.
            Vector2Int extraTile1 = tile + primaryCandidate;
            if (!thickCorridor.Contains(extraTile1) && !roomTiles.Contains(extraTile1) && !hallTiles.Contains(extraTile1))
            {
                thickCorridor.Add(extraTile1);
            }

            // Adăugăm tile-uri în direcția secondaryCandidate.
            Vector2Int extraTile2 = tile + secondaryCandidate;
            if (!thickCorridor.Contains(extraTile2) && !roomTiles.Contains(extraTile2) && !hallTiles.Contains(extraTile1))
            {
                thickCorridor.Add(extraTile2);
            }
        }

        return thickCorridor.ToList();
    }

    public void RectangularizeHall(Hall hall)
    {
        var existing = new HashSet<Vector2Int>(hall.GetFloorTiles());
        if (existing.Count == 0) return;

        // 2. Găsim limitele dreptunghiului.
        int minX = existing.Min(t => t.x);
        int maxX = existing.Max(t => t.x);
        int minY = existing.Min(t => t.y);
        int maxY = existing.Max(t => t.y);

        // 3. Iterăm fiecare rând (Y) și coloană (X) din bounding box.
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                var pos = new Vector2Int(x, y);

                // Dacă poziția nu exista deja în hall, o adăugăm.
                if (!existing.Contains(pos) && !dungeonData.GetDungeonHalliles().Contains(pos) && !dungeonData.GetDungeonRoomTiles().Contains(pos))
                {
                    // 3.1. Actualizăm obiectul Hall
                    hall.AddFloorTiles(pos);

                    // 3.2. Actualizăm structura globală (ca să nu fie recunoscute ca libere ulterior)
                    dungeonData.AddDungeonHallTiles(pos);

                    // 3.3. Punem efectiv tile-ul în Tilemap
                    paths.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
            }
        }
    }

    public static void GetHallMaxRuns(Hall hall, out int maxRunWidth, out int maxRunHeight)
    {
        var tiles = hall.GetFloorTiles();
        if (tiles == null || tiles.Count == 0)
        {
            maxRunWidth = maxRunHeight = 0;
            return;
        }

        // 1) Grupăm orizontal: după y, strângem lista de x-uri
        var rows = tiles
            .GroupBy(t => t.y)
            .Select(g => g.Select(t => t.x).OrderBy(x => x).ToList())
            .ToList();

        // 2) Pentru fiecare rând calculăm lungimea maximă de segment consecutiv
        maxRunWidth = 0;
        foreach (var xs in rows)
        {
            int currentRun = 1;
            for (int i = 1; i < xs.Count; i++)
            {
                if (xs[i] == xs[i - 1] + 1)
                    currentRun++;
                else
                    currentRun = 1;

                if (currentRun > maxRunWidth)
                    maxRunWidth = currentRun;
            }
            // dacă rândul are un singur tile
            if (xs.Count == 1)
                maxRunWidth = Math.Max(maxRunWidth, 1);
        }

        // 3) Grupăm vertical: după x, strângem lista de y-uri
        var cols = tiles
            .GroupBy(t => t.x)
            .Select(g => g.Select(t => t.y).OrderBy(y => y).ToList())
            .ToList();

        // 4) Pentru fiecare coloană calculăm lungimea maximă de segment consecutiv
        maxRunHeight = 0;
        foreach (var ys in cols)
        {
            int currentRun = 1;
            for (int i = 1; i < ys.Count; i++)
            {
                if (ys[i] == ys[i - 1] + 1)
                    currentRun++;
                else
                    currentRun = 1;

                if (currentRun > maxRunHeight)
                    maxRunHeight = currentRun;
            }
            if (ys.Count == 1)
                maxRunHeight = Math.Max(maxRunHeight, 1);
        }
    }

    /// <summary>
    /// Umple holul pe orizontală şi pe verticală
    /// până la dimensiunile dinamice calculate.
    /// </summary>
    public void RectangularizeHallDynamic(
        Hall hall,
        DungeonData dungeonData,
        Tilemap paths,
        TileBase floorTile
    )
    {
        // 1) Calculează dimensiunile target
        GetHallMaxRuns(hall, out int targetWidth, out int targetHeight);
        if (targetWidth == 0 || targetHeight == 0)
            return;

        // 2) Creează un set modificabil cu tile-urile curente
        var existing = new HashSet<Vector2Int>(hall.GetFloorTiles());

        //
        // 3) Extindere pe fiecare rând
        //
        // Grupăm tile-urile după y
        var rows = existing
            .GroupBy(p => p.y)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => p.x).OrderBy(x => x).ToList()
            );

        foreach (var kv in rows)
        {
            int y = kv.Key;
            var xs = kv.Value;

            // Găsim segmentul cel mai lung (start şi lungime)
            int bestStartX = xs[0], bestLen = 1, currStart = xs[0], currLen = 1;
            for (int i = 1; i < xs.Count; i++)
            {
                if (xs[i] == xs[i - 1] + 1)
                {
                    currLen++;
                }
                else
                {
                    if (currLen > bestLen)
                    {
                        bestLen = currLen;
                        bestStartX = currStart;
                    }
                    currStart = xs[i];
                    currLen = 1;
                }
            }
            if (currLen > bestLen)
            {
                bestLen = currLen;
                bestStartX = currStart;
            }

            // Câţi tile-uri lipsesc
            int missing = targetWidth - bestLen;
            if (missing <= 0)
                continue;

            // Împărţim pad-ul în stânga / dreapta
            int padLeft = missing / 2;
            int padRight = missing - padLeft;
            int fillStartX = bestStartX - padLeft;
            int fillEndX = bestStartX + bestLen + padRight - 1;

            // Adăugăm efectiv toate poziţiile
            for (int x = fillStartX; x <= fillEndX; x++)
            {
                var pos = new Vector2Int(x, y);
                if (!existing.Contains(pos)
                    && !dungeonData.GetDungeonHalliles().Contains(pos)
                    && !dungeonData.GetDungeonRoomTiles().Contains(pos))
                {
                    hall.AddFloorTiles(pos);
                    dungeonData.AddDungeonHallTiles(pos);
                    paths.SetTile(new Vector3Int(x, y, 0), floorTile);
                    existing.Add(pos);
                }
            }
        }

        //
        // 4) Extindere pe fiecare coloană
        //
        var cols = existing
            .GroupBy(p => p.x)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => p.y).OrderBy(y => y).ToList()
            );

        foreach (var kv in cols)
        {
            int x = kv.Key;
            var ys = kv.Value;

            int bestStartY = ys[0], bestLen = 1, currStart = ys[0], currLen = 1;
            for (int i = 1; i < ys.Count; i++)
            {
                if (ys[i] == ys[i - 1] + 1)
                    currLen++;
                else
                {
                    if (currLen > bestLen)
                    {
                        bestLen = currLen;
                        bestStartY = currStart;
                    }
                    currStart = ys[i];
                    currLen = 1;
                }
            }
            if (currLen > bestLen)
            {
                bestLen = currLen;
                bestStartY = currStart;
            }

            int missing = targetHeight - bestLen;
            if (missing <= 0)
                continue;

            int padDown = missing / 2;
            int padUp = missing - padDown;
            int fillStartY = bestStartY - padDown;
            int fillEndY = bestStartY + bestLen + padUp - 1;

            for (int y = fillStartY; y <= fillEndY; y++)
            {
                var pos = new Vector2Int(x, y);
                if (!existing.Contains(pos)
                    && !dungeonData.GetDungeonHalliles().Contains(pos)
                    && !dungeonData.GetDungeonRoomTiles().Contains(pos))
                {
                    hall.AddFloorTiles(pos);
                    dungeonData.AddDungeonHallTiles(pos);
                    paths.SetTile(new Vector3Int(x, y, 0), floorTile);
                    existing.Add(pos);
                }
            }
        }
    }


    private void FillInteriorHoles()
    {
        // 1) Determinăm bounding box-ul pe baza tile-urilor ocupate
        HashSet<Vector2Int> allTiles = dungeonData.GetDungeonAllTiles(); // presupunem că aici ai toate tile‑urile deja ocupate
        int minX = allTiles.Min(pos => pos.x);
        int maxX = allTiles.Max(pos => pos.x);
        int minY = allTiles.Min(pos => pos.y);
        int maxY = allTiles.Max(pos => pos.y);

        // 2) Marchez tile‑urile exterioare (accesibile din afară) folosind BFS
        HashSet<Vector2Int> visitedOutside = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Adăugăm în coadă tile‑urile de pe marginea bounding box-ului (external)
        for (int x = minX; x <= maxX; x++)
        {
            // Marginea de jos (un pic sub) și de sus (puțin peste)
            TryAddExterior(new Vector2Int(x, minY - 1), visitedOutside, queue);
            TryAddExterior(new Vector2Int(x, maxY + 1), visitedOutside, queue);
        }
        for (int y = minY; y <= maxY; y++)
        {
            // Marginea din stânga (puțin la stânga) și din dreapta (puțin la dreapta)
            TryAddExterior(new Vector2Int(minX - 1, y), visitedOutside, queue);
            TryAddExterior(new Vector2Int(maxX + 1, y), visitedOutside, queue);
        }

        // Efectuăm BFS: explorăm toți vecinii liberi din aceste puncte
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            // Ne uităm la cei patru vecini (up, down, left, right)
            foreach (Vector2Int dir in new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int neighbor = current + dir;

                // Ne asigurăm că ne aflăm în interiorul bounding box-ului
                if (neighbor.x >= minX && neighbor.x <= maxX &&
                    neighbor.y >= minY && neighbor.y <= maxY)
                {
                    TryAddExterior(neighbor, visitedOutside, queue);
                }
            }
        }

        // 3) Tile-urile care rămân libere în interiorul bounding box-ului și NU au fost marcate ca exterior
        // sunt considerate "hole". Umplem aceste zone cu floorTile.
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!allTiles.Contains(pos) && !visitedOutside.Contains(pos))
                {
                    // Aici "umplem" cu tile-ul de podea
                    dungeonData.AddDungeonRoomTiles(pos);
                    roomMap.SetTile(new Vector3Int(pos.x, pos.y, 0), floorTile);
                }
            }
        }
    }

    /// <summary>
    /// Funcție helper care adaugă tile-ul la exterior (marcat ca accesibil) dacă nu este ocupat și nu a fost deja marcat.
    /// </summary>
    private void TryAddExterior(Vector2Int pos, HashSet<Vector2Int> visitedOutside, Queue<Vector2Int> queue)
    {
        // Dacă tile-ul nu e ocupat și încă nu a fost vizitat, îl adăugăm la exterior
        if (!dungeonData.GetDungeonAllTiles().Contains(pos) && !visitedOutside.Contains(pos))
        {
            visitedOutside.Add(pos);
            queue.Enqueue(pos);
        }
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
        HashSet<Vector2Int> hallTiles = dungeonData.GetDungeonHalliles();

        if (hallTiles.Contains(roomTile))
        {
            //Dacă unul dintre vecini este un hall tile, returnăm false
            return false;
        }


        //Dacă niciun vecin nu este hall tile, returnăm true
        return true;
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
