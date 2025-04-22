using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Unity.VisualScripting;

public class DungeonData : MonoBehaviour
{
    public static DungeonData Instance { get; private set; }//Singleton
    private List<Room> rooms = new List<Room>();

    private List<Hall> halls = new List<Hall>();

    private List<List<Room>> listRoomGroups = new List<List<Room>>();

    private HashSet<Vector2Int> dungeonRoomTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonRoomTilesAndSpace = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonHallTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonAllTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> colliderTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonLeftTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonRightTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonUpTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonDownTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> rightBoundaryWalls = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> upBoundaryWalls = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> leftBoundaryWalls = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> downBoundaryWalls = new HashSet<Vector2Int>();

    private List<Room> roomsStartEnd = new List<Room>();

    private List<int> lenghtsStartEnd = new List<int>();

    private List<int> directions = new List<int>();

    public static List<Vector2Int> fourDirections = new()
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left
    };

    public static List<Vector2Int> diagonalDirections = new()
    {
        new Vector2Int(1, 1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
        new Vector2Int(-1, -1)
    };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple DungeonData instances detected! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    //Get methods:
    public List<Room> GetRooms() => new List<Room>(rooms);
    public List<Hall> GetHalls() => new List<Hall>(halls);
    public List<Room> GetRoomsStartEnd() => new List<Room>(roomsStartEnd);
    public List<int> GetLenghtsStartEnd() => new List<int>(lenghtsStartEnd);
    public List<int> GetDirections() => new List<int>(directions);
    public List<List<Room>> GetListRoomGroups() => new List<List<Room>>(listRoomGroups);
    public HashSet<Vector2Int> GetDungeonRoomTiles() => new HashSet<Vector2Int>(dungeonRoomTiles);
    public List<Vector2Int> GetDungeonRoomTilesAndSpace() => new List<Vector2Int>(dungeonRoomTilesAndSpace);
    public Room GetRoomById(int id)
    {
        return this.rooms.FirstOrDefault(room => room.RoomId == id);//returneaza camera sau null
    }
    public Room GetRoom(Room inputRoom)
    {
        Room foundRoom = rooms.FirstOrDefault(r => r.Equals(inputRoom));
        if (foundRoom != null)
        {
            Debug.Log("Am găsit camera!");
        }
        else
        {
            Debug.Log("Camera nu există în listă.");
        }
        return foundRoom;
    }
    public HashSet<Vector2Int> GetDungeonHalliles() => new HashSet<Vector2Int>(dungeonHallTiles);
    public HashSet<Vector2Int> GetDungeonAllTiles() => new HashSet<Vector2Int>(dungeonAllTiles);
    public List<Vector2Int> GetColliderTiles() => new List<Vector2Int>(colliderTiles);
    public IReadOnlyCollection<Vector2Int> GetDungeonLeftWallTiles() => dungeonLeftTiles;
    public IReadOnlyCollection<Vector2Int> GetDungeonRightWallTiles() => dungeonRightTiles;
    public IReadOnlyCollection<Vector2Int> GetDungeonUpWallTiles() => dungeonUpTiles;
    public IReadOnlyCollection<Vector2Int> GetDungeonDownWallTiles() => dungeonDownTiles;
    public IReadOnlyCollection<Vector2Int> GetRightBoundaryWalls() => rightBoundaryWalls;
    public IReadOnlyCollection<Vector2Int> GetUpBoundaryWalls() => upBoundaryWalls;

    //Set methods:
    public void SetListRoomGroups(List<List<Room>> listRoomGroups)
    {
        this.listRoomGroups = listRoomGroups;
    }
    private bool AddToCollection<T>(HashSet<T> collection, T item)//metoda generica
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (collection.Contains(item))
        {
            return false;
        }
        collection.Add(item);
        return true;

    }
    private bool RemoveFromCollection<T>(HashSet<T> collection, T item)//metoda generica
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (!collection.Contains(item))
        {
            return false;
        }
        collection.Remove(item);
        return true;
    }


    public bool AddDungeonRoomTiles(Vector2Int pos) => AddToCollection(dungeonRoomTiles, pos);
    public bool AddDungeonRoomTilesAndSpace(Vector2Int pos) => AddToCollection(dungeonRoomTilesAndSpace, pos);
    public bool AddDungeonHallTiles(Vector2Int pos) => AddToCollection(dungeonHallTiles, pos);
    public bool AddDungeonAllTiles(Vector2Int pos) => AddToCollection(dungeonAllTiles, pos);
    public bool AddColliderTiles(Vector2Int pos) => AddToCollection(colliderTiles, pos);
    public bool AddDungeonDownWallTiles(Vector2Int pos) => AddToCollection(dungeonDownTiles, pos);
    public bool AddDungeonUpWallTiles(Vector2Int pos) => AddToCollection(dungeonUpTiles, pos);
    public bool AddDungeonLeftWallTiles(Vector2Int pos) => AddToCollection(dungeonLeftTiles, pos);
    public bool AddDungeonRightWallTiles(Vector2Int pos) => AddToCollection(dungeonRightTiles, pos);
    public bool AddRightBoundaryWalls(Vector2Int pos) => AddToCollection(rightBoundaryWalls, pos);
    public bool AddUpBoundaryWalls(Vector2Int pos) => AddToCollection(upBoundaryWalls, pos);
    public bool AddLeftBoundaryWalls(Vector2Int pos) => AddToCollection(leftBoundaryWalls, pos);
    public bool AddDownBoundaryWalls(Vector2Int pos) => AddToCollection(downBoundaryWalls, pos);


    public bool RemoveDungeonRoomTiles(Vector2Int pos) => RemoveFromCollection(dungeonRoomTiles, pos);
    public bool RemoveDungeonHallTiles(Vector2Int pos) => RemoveFromCollection(dungeonHallTiles, pos);
    public bool RemoveDungeonAllTiles(Vector2Int pos) => RemoveFromCollection(dungeonAllTiles, pos);
    public bool RemoveColliderTiles(Vector2Int pos) => RemoveFromCollection(colliderTiles, pos);
    public bool RemoveDungeonDownWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonDownTiles, pos);
    public bool RemoveDungeonUpWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonUpTiles, pos);
    public bool RemoveDungeonLeftWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonLeftTiles, pos);
    public bool RemoveDungeonRightWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonRightTiles, pos);
    public bool RemoveRightBoundaryWalls(Vector2Int pos) => RemoveFromCollection(rightBoundaryWalls, pos);
    public bool RemoveUpBoundaryWalls(Vector2Int pos) => RemoveFromCollection(upBoundaryWalls, pos);

    public bool AddRoom(Room roomToAdd)
    {
        if (roomToAdd == null) throw new ArgumentNullException(nameof(roomToAdd));
        if (rooms.Contains(roomToAdd))
            return false;
        this.rooms.Add(roomToAdd);
        return true;
    }
    public bool RemoveRoom(Room roomToRemove)
    {
        if (roomToRemove == null) throw new ArgumentNullException(nameof(roomToRemove));
        if (!rooms.Contains(roomToRemove))
            return false;
        this.rooms.Remove(roomToRemove);
        return true;
    }

    public bool AddRoomStartEnd(Room room)
    {
        if (room == null) throw new ArgumentNullException(nameof(room));
        if (roomsStartEnd.Contains(room))
            return false;
        this.roomsStartEnd.Add(room);
        return true;
    }

    public bool AddLenghtStartEnd(int lenght)
    {
        if (lenghtsStartEnd.Contains(lenght))
            return false;
        this.lenghtsStartEnd.Add(lenght);
        return true;
    }

    public void AddDirection(int direction)
    {
        this.directions.Add(direction);//avem nevoie si de duplicate pt. ca ele reprezinta directii
    }

    public bool AddHall(Hall hallToAdd)
    {
        if (hallToAdd == null) throw new ArgumentNullException(nameof(hallToAdd));
        if (halls.Contains(hallToAdd))
            return false;
        this.halls.Add(hallToAdd);
        return true;
    }

    public bool RemoveHall(Hall hallToRemove)
    {
        if (hallToRemove == null) throw new ArgumentNullException(nameof(hallToRemove));
        if (!halls.Contains(hallToRemove))
            return false;
        this.halls.Remove(hallToRemove);
        return true;
    }

    public bool AddGroup(List<Room> group)
    {
        if (group == null) throw new ArgumentNullException(nameof(group));
        if (listRoomGroups.Contains(group))
            return false;
        this.listRoomGroups.Add(group);
        return true;
    }

    public bool RemoveGroup(List<Room> group)
    {
        if (group == null) throw new ArgumentNullException(nameof(group));
        if (!listRoomGroups.Contains(group))
            return false;
        this.listRoomGroups.Remove(group);
        return true;
    }


    //Reset function
    public void ClearAll()
    {
        foreach (Room room in rooms)
        {
            room.ClearAll();
        }
        foreach (Hall hall in halls)
        {
            hall.ClearAll();
        }
        foreach (List<Room> listRoom in this.listRoomGroups)
        {
            listRoom.Clear();
        }
        directions.Clear();
        listRoomGroups.Clear();
        dungeonRoomTilesAndSpace.Clear();
        rooms.Clear(); //acc. lucru cu "Rooms= new List<Room>()" adica alocarea de spatiu
        halls.Clear();
        dungeonRoomTiles.Clear(); // Sau dungeonTiles = new HashSet<Vector2Int>();
        dungeonHallTiles.Clear();
        dungeonAllTiles.Clear();
        colliderTiles.Clear(); // Sau colliderTiles = new HashSet<Vector2Int>();
        dungeonDownTiles.Clear();
        dungeonUpTiles.Clear();
        dungeonLeftTiles.Clear();
        dungeonRightTiles.Clear();
        rightBoundaryWalls.Clear();
        upBoundaryWalls.Clear();
        leftBoundaryWalls.Clear();
        downBoundaryWalls.Clear();
        roomsStartEnd.Clear();
        lenghtsStartEnd.Clear();
    }

    public void GenerateDungeonRoomTiles()
    {
        dungeonRoomTiles.Clear();

        foreach (Room room in this.GetRooms())
        {
            dungeonRoomTiles.UnionWith(room.GetFloorTiles());
        }
    }

    public void GenerateDungeonRoomAndSpaceTiles()
    {
        dungeonRoomTilesAndSpace.Clear();

        foreach (Room room in this.GetRooms())
        {
            dungeonRoomTilesAndSpace.UnionWith(room.FloorTilesAndSpaceAround(room.GetRoomCenterPos(), false));
        }
    }

    public void GenerateDungeonHallTiles()
    {
        dungeonHallTiles.Clear();

        foreach (Hall hall in this.GetHalls())
        {
            dungeonHallTiles.UnionWith(hall.GetFloorTiles());
        }
    }

    public void GenerateDungeonAllTiles()
    {
        dungeonAllTiles.Clear();
        dungeonAllTiles.UnionWith(dungeonRoomTiles);
        dungeonAllTiles.UnionWith(dungeonHallTiles);
    }

    public void GenerateDungeonCollider()
    {
        this.colliderTiles.Clear();////Pt a evita duplicarea
        this.dungeonDownTiles.Clear();
        this.dungeonUpTiles.Clear();
        this.dungeonRightTiles.Clear();
        this.dungeonLeftTiles.Clear();
        this.upBoundaryWalls.Clear();
        this.rightBoundaryWalls.Clear();
        this.leftBoundaryWalls.Clear();
        this.downBoundaryWalls.Clear();

        List<Vector2Int> neighborOffsets = new();
        neighborOffsets.AddRange(fourDirections);
        neighborOffsets.AddRange(diagonalDirections);

        foreach (Vector2Int tilePosition in dungeonAllTiles)
        {
            foreach (Vector2Int offset in neighborOffsets)
            {
                Vector2Int newPosition = tilePosition + offset;
                if (!dungeonAllTiles.Contains(newPosition))
                {
                    colliderTiles.Add(newPosition);
                }
            }
        }
    }

    public bool VerifyDungeonRoomTile(Vector2Int tile)
    {
        return dungeonRoomTiles.Contains(tile);
    }


    public IEnumerator TutorialCoroutine(Action code, float delay = 1f)
    {
        if (code == null) throw new ArgumentNullException(nameof(code));
        yield return new WaitForSeconds(delay);
        code();
    }
}
