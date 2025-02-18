using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RoomConfig
{
    [SerializeField] private RoomType roomType;
    [SerializeField] private Vector2Int roomDimensions;

    public RoomType GetRoomType() => roomType;
    public Vector2Int GetRoomDimensions() => roomDimensions;

}