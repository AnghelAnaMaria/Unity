using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DungeonData : MonoBehaviour
{
    private List<Room> rooms = new List<Room>();
    private List<Hall> halls = new List<Hall>();

    private HashSet<Vector2Int> dungeonTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> colliderTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonLeftWallTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonRightWallTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonUpWallTiles = new HashSet<Vector2Int>();

    private HashSet<Vector2Int> dungeonDownWallTiles = new HashSet<Vector2Int>();

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

    //Get methods:
    public IReadOnlyCollection<Room> Rooms => rooms;
    public IReadOnlyCollection<Hall> Halls => halls;
    public IReadOnlyCollection<Vector2Int> DungeonTiles => dungeonTiles;
    public IReadOnlyCollection<Vector2Int> ColliderTiles => colliderTiles;
    public IReadOnlyCollection<Vector2Int> DungeonLeftWallTiles => dungeonLeftWallTiles;
    public IReadOnlyCollection<Vector2Int> DungeonRightWallTiles => dungeonRightWallTiles;
    public IReadOnlyCollection<Vector2Int> DungeonUpWallTiles => dungeonUpWallTiles;
    public IReadOnlyCollection<Vector2Int> DungeonDownWallTiles => dungeonDownWallTiles;
    public IReadOnlyCollection<Vector2Int> RightBoundaryWalls => rightBoundaryWalls;
    public IReadOnlyCollection<Vector2Int> UpBoundaryWalls => upBoundaryWalls;

    //Set methods:
    private bool AddToCollection<T>(HashSet<T> collection, T item)//metoda generica
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        return collection.Add(item);

    }
    private bool RemoveFromCollection<T>(HashSet<T> collection, T item)//metoda generica
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        return collection.Remove(item);
    }


    public bool AddDungeonTiles(Vector2Int pos) => AddToCollection(dungeonTiles, pos);
    public bool AddColliderTiles(Vector2Int pos) => AddToCollection(colliderTiles, pos);
    public bool AddDungeonDownWallTiles(Vector2Int pos) => AddToCollection(dungeonDownWallTiles, pos);
    public bool AddDungeonUpWallTiles(Vector2Int pos) => AddToCollection(dungeonUpWallTiles, pos);
    public bool AddDungeonLeftWallTiles(Vector2Int pos) => AddToCollection(dungeonLeftWallTiles, pos);
    public bool AddDungeonRightWallTiles(Vector2Int pos) => AddToCollection(dungeonRightWallTiles, pos);
    public bool AddRightBoundaryWalls(Vector2Int pos) => AddToCollection(rightBoundaryWalls, pos);
    public bool AddUpBoundaryWalls(Vector2Int pos) => AddToCollection(upBoundaryWalls, pos);

    public bool RemoveDungeonTiles(Vector2Int pos) => RemoveFromCollection(dungeonTiles, pos);
    public bool RemoveColliderTiles(Vector2Int pos) => RemoveFromCollection(colliderTiles, pos);
    public bool RemoveDungeonDownWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonDownWallTiles, pos);
    public bool RemoveDungeonUpWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonUpWallTiles, pos);
    public bool RemoveDungeonLeftWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonLeftWallTiles, pos);
    public bool RemoveDungeonRightWallTiles(Vector2Int pos) => RemoveFromCollection(dungeonRightWallTiles, pos);
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
        dungeonDownWallTiles.Clear();
        dungeonUpWallTiles.Clear();
        dungeonLeftWallTiles.Clear();
        dungeonRightWallTiles.Clear();
        rightBoundaryWalls.Clear();
        upBoundaryWalls.Clear();
    }

    public void GenerateDungeonTiles()
    {
        dungeonTiles.Clear();

        foreach (Room room in this.Rooms)
        {
            dungeonTiles.UnionWith(room.FloorTiles());
        }
        foreach (Hall hall in this.halls)
        {
            dungeonTiles.UnionWith(hall.FloorTiles());
        }
    }

    public void GenerateDungeonCollider()
    {
        this.colliderTiles.Clear();////Pt a evita duplicarea
        this.dungeonDownWallTiles.Clear();
        this.dungeonUpWallTiles.Clear();
        this.dungeonRightWallTiles.Clear();
        this.dungeonLeftWallTiles.Clear();
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
                colliderTiles.Add(newPosition);
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
