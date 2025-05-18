using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using UnityEditor;
using UnityEngine;


//În orice CustomEditor, Unity îţi dă automat o variabilă numită target care:
//-este instanţa componentului (scriptului) pe care tocmai o editezi în Inspector
//-are tipul generic UnityEngine.Object
[CustomEditor(typeof(Test))]//pentru orice GameObject care are atașat scriptul Test
public class WFCInspector : Editor//în locul inspectorului default vom folosi clasa WFCInspector
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();//păstrează toate câmpurile vizibile în Inspector așa cum ar fi fără custom editor

        Test myScript = (Test)target;

        //Desenează un buton în Inspector cu eticheta “Create tilemap”:
        //Când este apăsat, invocă două metode din scriptul Test: -CreateWFC() –generează matricea de indexi de patterns
        //                                                        -CreateTilemap() –folosește rezultatul pentru a popula un Tilemap în scenă
        if (GUILayout.Button("Create tilemap"))
        {
            myScript.CreateWFC();
            myScript.CreateTilemap();
        }

        //Desenează un buton în Inspector cu eticheta “Save tilemap”:
        //Cand este apasat, invoca o metoda din scriptul Test: -SaveTilemap()
        if (GUILayout.Button("Save tilemap"))
        {
            myScript.SaveTilemap();
        }
    }
}