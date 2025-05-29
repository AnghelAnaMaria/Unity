using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(ApartmentConfig))]
public class ApartmentConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        ApartmentConfig config = (ApartmentConfig)target;

        // Afișează lista de camere (rooms) în Inspector
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rooms"), true);

        // Calculăm numărul de camere de tip Bucătărie și Sufragerie
        int countBucatarie = 0;
        int countSufragerie = 0;
        int countBaie = 0;
        int countDormitor = 0;

        foreach (RoomConfig room in config.GetRooms())
        {
            switch (room.GetRoomType())
            {
                case RoomType.Bucatarie:
                    countBucatarie++;
                    break;
                case RoomType.Sufragerie:
                    countSufragerie++;
                    break;
                case RoomType.Baie:
                    countBaie++;
                    break;
                case RoomType.Dormitor:
                    countDormitor++;
                    break;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Opțiuni Layout", EditorStyles.boldLabel);

        // Afișăm checkbox-ul pentru Bucătărie Open Space, activ doar dacă există cel puțin o cameră de tip bucătărie
        EditorGUI.BeginDisabledGroup(countBucatarie == 0);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("includeOpenSpaceKitchen"),
            new GUIContent("Bucătărie Open Space"));
        EditorGUI.EndDisabledGroup();

        // Afișăm checkbox-ul pentru Sufragerie Open Space, activ doar dacă există cel puțin o cameră de tip sufragerie
        EditorGUI.BeginDisabledGroup(countSufragerie == 0);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("includeOpenSpaceLivingRoom"),
            new GUIContent("Sufragerie Open Space"));
        EditorGUI.EndDisabledGroup();

        // *** VALIDARE INPUT ***
        EditorGUILayout.Space();
        if (config.GetRooms().Count < 3)
            EditorGUILayout.HelpBox("Apartamentul trebuie să conțină cel puțin 3 camere!", MessageType.Error);
        if (countBaie == 0)
            EditorGUILayout.HelpBox("Trebuie să existe cel puțin o baie!", MessageType.Error);
        if (countBucatarie == 0)
            EditorGUILayout.HelpBox("Trebuie să existe cel puțin o bucătărie!", MessageType.Error);
        if (countDormitor == 0)
            EditorGUILayout.HelpBox("Trebuie să existe cel puțin un dormitor!", MessageType.Error);

        serializedObject.ApplyModifiedProperties();
    }
}
