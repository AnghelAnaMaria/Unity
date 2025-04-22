using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ApartmentConfig", menuName = "ApartmentConfig")]
public class ApartmentConfig : ScriptableObject
{
    [SerializeField] private List<RoomConfig> rooms = new List<RoomConfig>();

    [Header("Opțiuni Layout Apartament")]
    [Tooltip("Bifează dacă vrei ca bucătăria să fie open space (activ numai dacă există cel puțin o cameră de tip bucătărie).")]
    [SerializeField] private bool includeOpenSpaceKitchen = false;

    [Tooltip("Bifează dacă vrei ca sufrageria să fie open space (activ numai dacă există cel puțin o cameră de tip sufragerie).")]
    [SerializeField] private bool includeOpenSpaceLivingRoom = false;

    public List<RoomConfig> GetAll()
    {
        return new List<RoomConfig>(rooms);
    }

    public List<RoomConfig> GetRooms()
    {
        return rooms;
    }

    public bool IncludeOpenSpaceKitchen => includeOpenSpaceKitchen;
    public bool IncludeOpenSpaceLivingRoom => includeOpenSpaceLivingRoom;

    /*public List<RoomConfig> GetHalls()
    {
        return rooms.FindAll(room => IsHall(room));
    }

    private bool IsHall(RoomConfig roomConfig)
    {
        return roomConfig.GetRoomType() == RoomType.Hol;
    }*/
}