using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Test))]
public class WFCInspector : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Get a reference to the target script
        Test myScript = (Test)target;

        // Button to run WFC and create the tilemap
        if (GUILayout.Button("Create tilemap"))
        {
            myScript.CreateWFC();
            myScript.CreateTilemap();
        }

        // Button to save the generated tilemap
        if (GUILayout.Button("Save tilemap"))
        {
            myScript.SaveTilemap();
        }
    }
}
