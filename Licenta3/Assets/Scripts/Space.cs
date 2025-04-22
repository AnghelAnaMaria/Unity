using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class Space//I use inheritance. I'll inherit from this class:
{
    protected Vector2Int dimensions;
    protected HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> upTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> downTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> rightTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> leftTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> innerTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> cornerTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2> propPositions = new HashSet<Vector2>();
    protected List<GameObject> propObjectReferences = new List<GameObject>();
    protected List<Vector2Int> accessibleDoorTiles = new List<Vector2Int>();

    //Get methods:
    public Vector2Int GetDimensions() => dimensions;
    //public IReadOnlyCollection<Vector2Int> FloorTiles() => floorTiles;
    public List<Vector2Int> GetFloorTiles()
    {
        return new List<Vector2Int>(floorTiles);
    }
    public List<Vector2Int> GetUpTiles() => new List<Vector2Int>(upTiles);
    public List<Vector2Int> GetDownTiles() => new List<Vector2Int>(downTiles);
    public List<Vector2Int> GetRightTiles() => new List<Vector2Int>(rightTiles);
    public List<Vector2Int> GetLeftTiles() => new List<Vector2Int>(leftTiles);
    public List<Vector2Int> GetInnerTiles() => new List<Vector2Int>(innerTiles);
    public List<Vector2Int> GetCornerTiles() => new List<Vector2Int>(cornerTiles);
    public List<Vector2> GetPropPositions() => new List<Vector2>(propPositions);
    public List<GameObject> GetPropObjectReferences() => new List<GameObject>(propObjectReferences);
    public List<Vector2Int> GetAccesibleDoorTiles() => new List<Vector2Int>(accessibleDoorTiles);

    //Set methods:
    public void SetRoomDimensions(Vector2Int dimensions)
    {
        if (dimensions.x <= 0 || dimensions.y <= 0)
            throw new ArgumentException("Dimensions must be positive values.");
        this.dimensions = dimensions;
    }

    public void AddPropObjectReferences(GameObject obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));//we can have duplicates
        this.propObjectReferences.Add(obj);
    }
    public void RemovePropObjectReferences(GameObject obj)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (!propObjectReferences.Contains(obj)) throw new InvalidOperationException("Item not found.");
        this.propObjectReferences.Remove(obj);
    }

    public void AddAccessibleDoorTiles(Vector2Int pos)
    {
        if (pos == null) throw new ArgumentNullException(nameof(pos));
        if (accessibleDoorTiles.Contains(pos)) throw new InvalidOperationException("Tile already exists.");
        this.accessibleDoorTiles.Add(pos);
    }
    public void RemoveAccessibleDoorTiles(Vector2Int pos)
    {
        if (pos == null) throw new ArgumentNullException(nameof(pos));
        if (!accessibleDoorTiles.Contains(pos)) throw new InvalidOperationException("Item not found.");
        this.accessibleDoorTiles.Remove(pos);
    }

    //Alternative Set methods (cu metode generice tip sablon):
    private bool AddToCollection<T>(HashSet<T> collection, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (collection.Contains(item))
        {
            return false;
        }
        collection.Add(item);
        return true;
    }
    private bool RemoveFromCollection<T>(HashSet<T> collection, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (!collection.Contains(item))
        {
            return false;
        }
        collection.Remove(item);
        return true;
    }

    public void AddFloorTiles(Vector2Int pos) => AddToCollection(floorTiles, pos);
    public void AddDownTiles(Vector2Int pos) => AddToCollection(downTiles, pos);
    public void AddUpTiles(Vector2Int pos) => AddToCollection(upTiles, pos);
    public void AddLeftTiles(Vector2Int pos) => AddToCollection(leftTiles, pos);
    public void AddRightTiles(Vector2Int pos) => AddToCollection(rightTiles, pos);
    public void AddInnerTiles(Vector2Int pos) => AddToCollection(innerTiles, pos);
    public void AddCornerTiles(Vector2Int pos) => AddToCollection(cornerTiles, pos);
    public void AddPropPositions(Vector2Int pos) => AddToCollection(propPositions, pos);

    public void RemoveFloorTiles(Vector2Int pos) => RemoveFromCollection(floorTiles, pos);
    public void RemoveDownTiles(Vector2Int pos) => RemoveFromCollection(downTiles, pos);
    public void RemoveUpTiles(Vector2Int pos) => RemoveFromCollection(upTiles, pos);
    public void RemoveLeftTiles(Vector2Int pos) => RemoveFromCollection(leftTiles, pos);
    public void RemoveRightTiles(Vector2Int pos) => RemoveFromCollection(rightTiles, pos);
    public void RemoveInnerTiles(Vector2Int pos) => RemoveFromCollection(innerTiles, pos);
    public void RemoveColliderTiles(Vector2Int pos) => RemoveFromCollection(cornerTiles, pos);
    public void RemovePropPositions(Vector2Int pos) => RemoveFromCollection(propPositions, pos);


    //Constructor:
    public Space(Vector2Int dimensions)
    {
        this.dimensions = dimensions;

        floorTiles = new HashSet<Vector2Int>();
        upTiles = new HashSet<Vector2Int>();
        downTiles = new HashSet<Vector2Int>();
        rightTiles = new HashSet<Vector2Int>();
        leftTiles = new HashSet<Vector2Int>();
        innerTiles = new HashSet<Vector2Int>();
        cornerTiles = new HashSet<Vector2Int>();
        propPositions = new HashSet<Vector2>();
        propObjectReferences = new List<GameObject>();
        accessibleDoorTiles = new List<Vector2Int>();
    }

    //Clear function:
    public virtual void ClearAll()
    {
        floorTiles.Clear();
        upTiles.Clear();
        downTiles.Clear();
        rightTiles.Clear();
        leftTiles.Clear();
        innerTiles.Clear();
        cornerTiles.Clear();
        propPositions.Clear();
        foreach (var item in propObjectReferences)
        {
            GameObject.Destroy(item);
        }
        propObjectReferences.Clear();
        accessibleDoorTiles.Clear();

    }
}
