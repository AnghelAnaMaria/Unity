using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class HoverManager : MonoBehaviour
{
    public Camera mainCamera;
    public Tilemap roomTilemap;
    public TooltipManager tooltipManager;

    private Dictionary<Vector2Int, Room> tileToRoom;//Dicţionar pentru acces O(1)

    void Start()
    {
        BuildTileLookup();
    }

    void BuildTileLookup()
    {
        tileToRoom = new Dictionary<Vector2Int, Room>();
        foreach (var room in ApartmentData.Instance.GetRooms())
        {
            foreach (var tile in room.GetFloorTiles())
            {
                // Dacă două camere ar share-ui acelaşi tile, îl va înlocui pe ultimul
                tileToRoom[tile] = room;
            }
        }
    }

    void Update()
    {
        // 1) Obţine poziţia în lume a mouse-ului
        Vector3 screenPos = Input.mousePosition;
        // dacă ai cameră perspective, setează aici z = distanţa până la planul tilemap
        screenPos.z = Mathf.Abs(mainCamera.transform.position.z);
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(screenPos);

        // 2) Converteşte în coordonate de celulă
        Vector3Int cell = roomTilemap.WorldToCell(mouseWorld);
        var tilePos = new Vector2Int(cell.x, cell.y);

        // 3) Caută camera în dicţionar
        if (tileToRoom != null && tileToRoom.TryGetValue(tilePos, out Room hoveredRoom))
        {
            string name = hoveredRoom.GetRoomType().ToString();
            Vector2Int dim = hoveredRoom.GetDimensions();

            // Dacă vrei m² reale, înmulţeşte dim.x/ dim.y cu mărimea celulei:
            float cellSize = roomTilemap.cellSize.x; // presupunem pătrat
            float area = dim.x * dim.y * cellSize * cellSize;

            tooltipManager.ShowTooltip($"{name}\n{area:0.##} m²");
        }
        else
        {
            tooltipManager.HideTooltip();
        }
    }
}
