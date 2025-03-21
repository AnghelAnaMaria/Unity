using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hall : Space
{
    private Dictionary<string, List<Room>> hallNeighbors = new Dictionary<string, List<Room>>()
    {
    { "Up", new List<Room>() },
    { "Down", new List<Room>() },
    { "Left", new List<Room>() },
    { "Right", new List<Room>() }
    };

    //Get methods:
    public IReadOnlyList<Room> HallNeighbors(string direction)
    {
        if (!hallNeighbors.ContainsKey(direction))
            throw new ArgumentException($"Invalid direction: {direction}");

        return hallNeighbors[direction];
    }

    //Set methods, Add, Remove:
    public void AddNeighbor(string direction, Room neighbor)
    {
        if (!hallNeighbors.ContainsKey(direction))
            throw new ArgumentException($"Invalid direction: {direction}");

        if (neighbor == null)
            throw new ArgumentNullException(nameof(neighbor));

        if (!hallNeighbors[direction].Contains(neighbor))
        {
            hallNeighbors[direction].Add(neighbor);
        }

    }
    //Hall Constructor:
    public Hall(Vector2Int dimensions) : base(dimensions) { }

    //Clear:
    public override void ClearAll()
    {
        foreach (var direction in hallNeighbors.Keys)
        {
            hallNeighbors[direction].Clear();
        }
    }

    //Override Equals method
    public override bool Equals(object obj)
    {
        if (obj is Hall otherHall)//daca obj e de tip Hall object
        {
            return AreHallNeighborsEqual(otherHall.hallNeighbors);
        }
        return false;
    }

    private bool AreHallNeighborsEqual(Dictionary<string, List<Room>> otherHallNeighbors)
    {
        foreach (var direction in hallNeighbors.Keys)
        {
            if (!otherHallNeighbors.TryGetValue(direction, out var otherNeighborList))
                return false;

            var thisNeighborList = hallNeighbors[direction];

            //Check if both lists have the same count
            if (thisNeighborList.Count != otherNeighborList.Count)
                return false;

            //Ensure both lists are treated as unordered sets
            if (!new HashSet<Room>(thisNeighborList).SetEquals(new HashSet<Room>(otherNeighborList)))
                return false;
        }
        return true;
    }


    public override int GetHashCode()
    {
        int hash = 17; //Start with a non-zero value.

        //Aggregate all unique neighbors into a single set
        var uniqueNeighbors = new HashSet<Room>();

        foreach (var direction in hallNeighbors.Keys)
        {
            uniqueNeighbors.UnionWith(hallNeighbors[direction]);

        }

        foreach (var neighbor in uniqueNeighbors)
        {
            hash = HashCode.Combine(hash, neighbor);
        }

        return hash;
    }

}
