using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public class Room : Space
{
    private bool skipBoundaryWalls;//Pt. OpenSpace
    private static int nextId = 0;//Camp static
    public int RoomId { get; private set; }//Id unic
    private Vector2Int roomCenterPos;//daca camera e de 7m atunci facem (7-1)/2 si adaugam ultima parcela la final
    private string roomName;
    private RoomType roomType;

    private Dictionary<string, List<string>> neighbors = new Dictionary<string, List<string>>()
    {
    { "Up", new List<string>() },
    { "Down", new List<string>() },
    { "Left", new List<string>() },
    { "Right", new List<string>() }
    };


    //Get methods:
    public bool GetSkipBoundaryWalls() => this.skipBoundaryWalls;
    public Vector2Int GetRoomCenterPos() => roomCenterPos;//pt citire
    public string GetRoomName() => roomName;
    public RoomType GetRoomType() => roomType;

    public List<string> GetNeighbors(string direction)
    {
        if (neighbors.ContainsKey(direction))
        {
            return neighbors[direction];
        }

        Debug.LogError($"Invalid direction: {direction}");
        return null;
    }

    //Set methods, Add, Remove:
    public void SetSkipBoundaryWalls(bool skipBoundaryWalls = false)
    {
        this.skipBoundaryWalls = skipBoundaryWalls;
    }
    public void SetRoomCenter(Vector2Int roomCenterPos)
    {
        this.roomCenterPos = roomCenterPos;
    }
    public void SetRoomName(string roomName)
    {
        this.roomName = roomName;
    }
    public void SetRoomType(RoomType roomType)
    {
        this.roomType = roomType;
    }

    public void SetNeighbor(string direction, string neighborName)
    {
        if (!neighbors.ContainsKey(direction))
        {
            Debug.LogError($"Invalid direction: {direction}");
            return;
        }
        neighbors[direction].Add(neighborName);

    }

    public void AddNeighbor(string direction, string neighborName)
    {
        if (!neighbors.ContainsKey(direction))
        {
            Debug.LogError($"Invalid direction: {direction}");
            return;
        }
        if (!neighbors[direction].Contains(neighborName))
        {
            neighbors[direction].Add(neighborName);
        }

    }

    //Room constructor:
    public Room(Vector2Int roomCenterPos = default, RoomType roomType = RoomType.Bucatarie, Vector2Int dimensions = default) : base(dimensions)//Call the base class constructor
    {
        this.roomCenterPos = roomCenterPos;
        this.roomType = roomType;

        this.RoomId = nextId++;
        //Debug.Log($"Room creat cu ID: {this.RoomId}");

    }

    public static void ResetRoomIds()//Metoda statica
    {
        nextId = 0;
    }

    public Vector2Int AddToCurrentPosition(int direction = -1)//o folosim la functia PlaceRoomsProcedurally()
    {
        Vector2Int half = this.GetDimensions() / 2;
        bool isOddX = half.x % 2 != 0;//x impar
        bool isOddY = half.y % 2 != 0;//y impar

        if (isOddX) half.x = (this.GetDimensions().x - 1) / 2;
        if (isOddY) half.y = (this.GetDimensions().y - 1) / 2;

        switch (direction)
        {
            case 0: // sus
                return new Vector2Int(0, half.y);
            case 1: // stanga
                return new Vector2Int(-half.x, 0);
            case 2: // jos
                return new Vector2Int(0, -half.y);
            case 3: // dreapta
                return new Vector2Int(half.x, 0);
            default:
                Debug.Log("S-a apelat pentru prima camera, fara directie");
                return new Vector2Int(1, 1);
        }
    }

    public Vector2Int StartPosition(Room room)
    {
        int number = UnityEngine.Random.Range(1, 4);//Alegem random un perete al camerei room
        if (number == 1)
        {
            Vector2Int firstPosition = room.GetUpTiles()[UnityEngine.Random.Range(0, room.GetUpTiles().Count)];
            return firstPosition;
        }
        if (number == 2)
        {
            Vector2Int firstPosition = room.GetDownTiles()[UnityEngine.Random.Range(0, room.GetDownTiles().Count)];
            return firstPosition;
        }
        if (number == 3)
        {
            Vector2Int firstPosition = room.GetLeftTiles()[UnityEngine.Random.Range(0, room.GetLeftTiles().Count)];
            return firstPosition;
        }
        if (number == 4)
        {
            Vector2Int firstPosition = room.GetRightTiles()[UnityEngine.Random.Range(0, room.GetRightTiles().Count)];
            return firstPosition;
        }

        return new Vector2Int(0, 0);
    }

    public HashSet<Vector2Int> FloorTilesAndSpaceAround(Vector2Int roomCenter, bool neighbor)
    {
        Vector2Int half = this.GetDimensions() / 2;
        bool isOddX = this.GetDimensions().x % 2 != 0;
        bool isOddY = this.GetDimensions().y % 2 != 0;

        if (isOddX) half.x = (this.GetDimensions().x - 1) / 2;
        if (isOddY) half.y = (this.GetDimensions().y - 1) / 2;

        HashSet<Vector2Int> positions = new HashSet<Vector2Int>();

        for (var x = -half.x - (neighbor ? 0 : 1); x < half.x + (isOddX ? 1 : 0) + (neighbor ? 0 : 1); x++)
        {
            for (var y = -half.y - (isOddY ? 1 : 0) - (neighbor ? 0 : 1); y < half.y + (neighbor ? 0 : 1); y++)
            {
                Vector2Int position = roomCenter + new Vector2Int(x, y);
                positions.Add(position);

            }
        }
        return positions;
    }

    //Clear function:
    public override void ClearAll()
    {
        base.ClearAll(); //Call the base class clear method
        foreach (var direction in neighbors.Keys)
        {
            neighbors[direction].Clear();
        }
    }

    public override bool Equals(object obj)//metoda suprascrisa pt a compara daca 2 obiecte Room sunt egale
    {
        if (obj is Room otherRoom) //Verificam dacă obj este de tip Room.
        {
            return this.roomCenterPos == otherRoom.roomCenterPos &&
                   this.dimensions == otherRoom.dimensions &&
                   this.roomName == otherRoom.roomName &&
                   this.roomType == otherRoom.roomType &&
                this.floorTiles.SetEquals(otherRoom.floorTiles);
            // AreNeighborsEqual(otherRoom.neighbors);
        }
        return false;
    }

    public bool AreNeighborsEqual(Dictionary<string, List<string>> otherNeighbors)
    {
        foreach (var direction in neighbors.Keys)
        {
            if (!otherNeighbors.TryGetValue(direction, out var otherNeighborList))
                return false;

            var thisNeighborList = neighbors[direction];

            //Ensure both lists are treated as unordered sets
            if (!new HashSet<string>(thisNeighborList).SetEquals(new HashSet<string>(otherNeighborList)))
                return false;
        }
        return true;

    }

    public override int GetHashCode()//2 HashSet-uri care au acc cod hash (calculat cu metoda asta) sunt egale.
    {
        int hash = HashCode.Combine(roomCenterPos);

        //Hash floor tiles
        foreach (var tile in floorTiles)
        {
            hash = HashCode.Combine(hash, tile);
        }

        //Aggregate all unique neighbors into a single set
        var uniqueNeighbors = new HashSet<string>();

        foreach (var direction in neighbors.Keys)
        {
            uniqueNeighbors.UnionWith(neighbors[direction]);
        }

        //Hash the unique neighbors
        foreach (var neighbor in uniqueNeighbors)
        {
            hash = HashCode.Combine(hash, neighbor);
        }

        return hash;
    }

}
