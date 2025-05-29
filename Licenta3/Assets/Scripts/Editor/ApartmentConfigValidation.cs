using UnityEditor;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public static class ApartmentConfigValidation
{
    static ApartmentConfigValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Verifică toate ApartmentConfig din proiect
                var allConfigs = Resources.FindObjectsOfTypeAll<ApartmentConfig>();
                foreach (var config in allConfigs)
                {
                    var rooms = config.GetRooms();
                    if (rooms.Count < 3 ||
                        !rooms.Any(r => r.GetRoomType() == RoomType.Baie) ||
                        !rooms.Any(r => r.GetRoomType() == RoomType.Bucatarie) ||
                        !rooms.Any(r => r.GetRoomType() == RoomType.Dormitor))
                    {
                        EditorUtility.DisplayDialog("Eroare ApartmentConfig",
                            "Config-ul nu respectă cerințele! (minim 3 camere și trebuie Baie, Bucătărie, Dormitor)",
                            "OK");
                        EditorApplication.isPlaying = false;
                        break;
                    }
                }
            }
        };
    }
}
