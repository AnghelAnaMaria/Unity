using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomConfig
{
    [SerializeField] private RoomType roomType;

    [SerializeField, Tooltip("Dimensions must be at least 2x2.")] private Vector2Int roomDimensions = new Vector2Int(2, 2);


    public RoomType GetRoomType() => roomType;
    public void SetRoomType(RoomType type) => roomType = type;

    public Vector2Int GetRoomDimensions() => roomDimensions;
    public void SetRoomDimensions(Vector2Int dim) => roomDimensions = dim;

}