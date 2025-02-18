using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ApartmentConfig", menuName = "ApartmentConfig")]
public class ApartmentConfig : ScriptableObject
{
    [SerializeField] private List<RoomConfig> rooms = new List<RoomConfig>();

    public List<RoomConfig> GetAll()
    {
        return new List<RoomConfig>(rooms);
    }

    public List<RoomConfig> GetRooms()
    {
        return rooms.FindAll(room => !IsHall(room));
    }

    public List<RoomConfig> GetHalls()
    {
        return rooms.FindAll(room => IsHall(room));
    }

    private bool IsHall(RoomConfig roomConfig)
    {
        return roomConfig.GetRoomType() == RoomType.Hol;
    }
}