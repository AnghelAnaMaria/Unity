using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Room : Space
{
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
    public Vector2Int RoomCenterPos() => roomCenterPos;//pt citire
    public string RoomName() => roomName;
    public RoomType GetRoomType() => roomType;

    public List<string> Neighbors(string direction)
    {
        if (neighbors.ContainsKey(direction))
        {
            return neighbors[direction];
        }

        Debug.LogError($"Invalid direction: {direction}");
        return null;
    }

    //Set methods, Add, Remove:
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
    public Room(Vector2Int roomCenterPos, RoomType roomType, Vector2Int dimensions) : base(dimensions)//Call the base class constructor
    {
        this.roomCenterPos = roomCenterPos;
        this.roomType = roomType;
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
                Debug.Log("Ceva nu a mers bine; nu s-a dat o directie");
                return new Vector2Int(1, 1);
        }
    }

    public Vector2Int StartPosition(Room room)
    {
        int number = UnityEngine.Random.Range(1, 4);//Alegem random un perete al camerei room
        if (number == 1)
        {
            Vector2Int firstPosition = room.GetNearWallTilesUp()[UnityEngine.Random.Range(0, room.GetNearWallTilesUp().Count)];
            return firstPosition;
        }
        if (number == 2)
        {
            Vector2Int firstPosition = room.GetNearWallTilesDown()[UnityEngine.Random.Range(0, room.GetNearWallTilesDown().Count)];
            return firstPosition;
        }
        if (number == 3)
        {
            Vector2Int firstPosition = room.GetNearWallTilesLeft()[UnityEngine.Random.Range(0, room.GetNearWallTilesLeft().Count)];
            return firstPosition;
        }
        if (number == 4)
        {
            Vector2Int firstPosition = room.GetNearWallTilesRight()[UnityEngine.Random.Range(0, room.GetNearWallTilesRight().Count)];
            return firstPosition;
        }

        return new Vector2Int(0, 0);
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
                this.floorTiles.SetEquals(otherRoom.floorTiles) &&
                AreNeighborsEqual(otherRoom.neighbors);
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
