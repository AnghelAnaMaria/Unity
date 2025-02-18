using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Space//I use inheritance. I'll inherit from this class:
{
    protected Vector2 dimensions;
    protected HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> nearWallTilesUp = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> nearWallTilesDown = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> nearWallTilesRight = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> nearWallTilesLeft = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> innerTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> cornerTiles = new HashSet<Vector2Int>();
    protected HashSet<Vector2Int> propPositions = new HashSet<Vector2Int>();
    protected List<GameObject> propObjectReferences = new List<GameObject>();
    protected List<Vector2Int> accessibleDoorTiles = new List<Vector2Int>();

    //Get methods:
    public Vector2 Dimensions() => dimensions;
    public IReadOnlyCollection<Vector2Int> FloorTiles() => floorTiles;
    public HashSet<Vector2Int> GetFloorTiles()
    {
        return new HashSet<Vector2Int>(floorTiles);
    }
    public IReadOnlyCollection<Vector2Int> NearWallTilesUp => nearWallTilesUp;
    public IReadOnlyCollection<Vector2Int> NearWallTilesDown => nearWallTilesDown;
    public IReadOnlyCollection<Vector2Int> NearWallTilesRight => nearWallTilesRight;
    public IReadOnlyCollection<Vector2Int> NearWallTilesLeft => nearWallTilesLeft;
    public IReadOnlyCollection<Vector2Int> InnerTiles => innerTiles;
    public IReadOnlyCollection<Vector2Int> CornerTiles => cornerTiles;
    public IReadOnlyCollection<Vector2Int> PropPositions => propPositions;
    public IReadOnlyCollection<GameObject> PropObjectReferences => propObjectReferences;
    public IReadOnlyCollection<Vector2Int> AccessibleDoorTiles => accessibleDoorTiles;

    //Set methods:
    public void SetRoomDimensions(Vector2 dimensions)
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
        //if (collection.Contains(item)) throw new InvalidOperationException("Item already exists.");
        return collection.Add(item);//return false if item already in collection
    }
    private void RemoveFromCollection<T>(HashSet<T> collection, T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (!collection.Contains(item)) throw new InvalidOperationException("Item not found.");
        collection.Remove(item);
    }

    public void AddFloorTiles(Vector2Int pos) => AddToCollection(floorTiles, pos);
    public void AddNearWallTilesDown(Vector2Int pos) => AddToCollection(nearWallTilesDown, pos);
    public void AddNearWallTilesUp(Vector2Int pos) => AddToCollection(nearWallTilesUp, pos);
    public void AddNearWallTilesLeft(Vector2Int pos) => AddToCollection(nearWallTilesLeft, pos);
    public void AddNearWallTilesRight(Vector2Int pos) => AddToCollection(nearWallTilesRight, pos);
    public void AddInnerTiles(Vector2Int pos) => AddToCollection(innerTiles, pos);
    public void AddCornerTiles(Vector2Int pos) => AddToCollection(cornerTiles, pos);
    public void AddPropPositions(Vector2Int pos) => AddToCollection(propPositions, pos);

    public void RemoveFloorTiles(Vector2Int pos) => RemoveFromCollection(floorTiles, pos);
    public void RemoveNearWallTilesDown(Vector2Int pos) => RemoveFromCollection(nearWallTilesDown, pos);
    public void RemoveNearWallTilesUp(Vector2Int pos) => RemoveFromCollection(nearWallTilesUp, pos);
    public void RemoveNearWallTilesLeft(Vector2Int pos) => RemoveFromCollection(nearWallTilesLeft, pos);
    public void RemoveNearWallTilesRight(Vector2Int pos) => RemoveFromCollection(nearWallTilesRight, pos);
    public void RemoveInnerTiles(Vector2Int pos) => RemoveFromCollection(innerTiles, pos);
    public void RemoveColliderTiles(Vector2Int pos) => RemoveFromCollection(cornerTiles, pos);
    public void RemovePropPositions(Vector2Int pos) => RemoveFromCollection(propPositions, pos);


    //Constructor:
    public Space(Vector2 dimensions)
    {
        this.dimensions = dimensions;

        floorTiles = new HashSet<Vector2Int>();
        nearWallTilesUp = new HashSet<Vector2Int>();
        nearWallTilesDown = new HashSet<Vector2Int>();
        nearWallTilesRight = new HashSet<Vector2Int>();
        nearWallTilesLeft = new HashSet<Vector2Int>();
        innerTiles = new HashSet<Vector2Int>();
        cornerTiles = new HashSet<Vector2Int>();
        propPositions = new HashSet<Vector2Int>();
        propObjectReferences = new List<GameObject>();
        accessibleDoorTiles = new List<Vector2Int>();
    }

    //Clear function:
    public virtual void ClearAll()
    {
        floorTiles.Clear();
        nearWallTilesUp.Clear();
        nearWallTilesDown.Clear();
        nearWallTilesRight.Clear();
        nearWallTilesLeft.Clear();
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
