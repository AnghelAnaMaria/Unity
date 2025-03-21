using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DungeonData : MonoBehaviour
{
    public static DungeonData Instance { get; private set; }//Singleton
    private List<Room> rooms = new List<Room>();
    private List<Hall> halls = new List<Hall>();

    private HashSet<Vector2Int> dungeonTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> colliderTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonLeftTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonRightTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonUpTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonDownTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> rightBoundaryWalls = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> upBoundaryWalls = new HashSet<Vector2Int>();

    private static List<Vector2Int> fourDirections = new()
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left
    };

    private static List<Vector2Int> diagonalDirections = new()
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
    public List<Vector2Int> GetDungeonTiles() => new List<Vector2Int>(dungeonTiles);
    public List<Vector2Int> GetColliderTiles() => new List<Vector2Int>(colliderTiles);
    public IReadOnlyCollection<Vector2Int> GetDungeonLeftWallTiles() => dungeonLeftTiles;
    public IReadOnlyCollection<Vector2Int> GetDungeonRightWallTiles() => dungeonRightTiles;
    public IReadOnlyCollection<Vector2Int> GetDungeonUpWallTiles() => dungeonUpTiles;
    public IReadOnlyCollection<Vector2Int> GetDungeonDownWallTiles() => dungeonDownTiles;
    public IReadOnlyCollection<Vector2Int> GetRightBoundaryWalls() => rightBoundaryWalls;
    public IReadOnlyCollection<Vector2Int> GetUpBoundaryWalls() => upBoundaryWalls;

    //Set methods:
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


    public bool AddDungeonTiles(Vector2Int pos) => AddToCollection(dungeonTiles, pos);
    public bool AddColliderTiles(Vector2Int pos) => AddToCollection(colliderTiles, pos);
    public bool AddDungeonDownWallTiles(Vector2Int pos) => AddToCollection(dungeonDownTiles, pos);
    public bool AddDungeonUpWallTiles(Vector2Int pos) => AddToCollection(dungeonUpTiles, pos);
    public bool AddDungeonLeftWallTiles(Vector2Int pos) => AddToCollection(dungeonLeftTiles, pos);
    public bool AddDungeonRightWallTiles(Vector2Int pos) => AddToCollection(dungeonRightTiles, pos);
    public bool AddRightBoundaryWalls(Vector2Int pos) => AddToCollection(rightBoundaryWalls, pos);
    public bool AddUpBoundaryWalls(Vector2Int pos) => AddToCollection(upBoundaryWalls, pos);

    public bool RemoveDungeonTiles(Vector2Int pos) => RemoveFromCollection(dungeonTiles, pos);
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
        rooms.Clear(); //acc. lucru cu "Rooms= new List<Room>()" adica alocarea de spatiu
        halls.Clear();
        dungeonTiles.Clear(); // Sau dungeonTiles = new HashSet<Vector2Int>();
        colliderTiles.Clear(); // Sau colliderTiles = new HashSet<Vector2Int>();
        dungeonDownTiles.Clear();
        dungeonUpTiles.Clear();
        dungeonLeftTiles.Clear();
        dungeonRightTiles.Clear();
        rightBoundaryWalls.Clear();
        upBoundaryWalls.Clear();
    }

    public void GenerateDungeonTiles()
    {
        dungeonTiles.Clear();

        foreach (Room room in this.GetRooms())
        {
            dungeonTiles.UnionWith(room.GetFloorTiles());
        }
        /*foreach (Hall hall in this.GetHalls())
        {
            dungeonTiles.UnionWith(hall.GetFloorTiles());
        }*/
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

        List<Vector2Int> neighborOffsets = new();
        neighborOffsets.AddRange(fourDirections);
        neighborOffsets.AddRange(diagonalDirections);

        foreach (Vector2Int tilePosition in dungeonTiles)
        {
            foreach (Vector2Int offset in neighborOffsets)
            {
                Vector2Int newPosition = tilePosition + offset;
                if (!dungeonTiles.Contains(newPosition))
                {
                    colliderTiles.Add(newPosition);
                }
            }
        }
    }

    public bool VerifyDungeonTile(Vector2Int tile)
    {
        return dungeonTiles.Contains(tile);
    }


    public IEnumerator TutorialCoroutine(Action code, float delay = 1f)
    {
        if (code == null) throw new ArgumentNullException(nameof(code));
        yield return new WaitForSeconds(delay);
        code();
    }
}
