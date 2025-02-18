using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room : Space
{
    private Vector2 roomCenterPos;
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
    public Vector2 RoomCenterPos() => roomCenterPos;//pt citire
    public string RoomName() => roomName;
    public RoomType RoomType() => roomType;

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
    public void SetRoomCenter(Vector2 roomCenterPos)
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
    public Room(Vector2 roomCenterPos, RoomType roomType, Vector2 dimensions) : base(dimensions)//Call the base class constructor
    {
        this.roomCenterPos = roomCenterPos;
        this.roomType = roomType;
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
