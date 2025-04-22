using UnityEditor;
using UnityEngine;

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
        foreach (RoomConfig room in config.GetRooms())
        {
            if (room.GetRoomType() == RoomType.Bucatarie)
                countBucatarie++;
            else if (room.GetRoomType() == RoomType.Sufragerie)
                countSufragerie++;
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

        serializedObject.ApplyModifiedProperties();
    }
}
