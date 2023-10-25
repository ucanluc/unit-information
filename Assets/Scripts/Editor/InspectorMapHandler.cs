using MapMono;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(MapHandler))]
public class MapHandlersEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MapHandler mapHandler = (MapHandler)target;
        if (GUILayout.Button("Generate Map"))
        {
            mapHandler.GenerateMap();
        }

        if (GUILayout.Button("Clear Map"))
        {
            mapHandler.ClearMap();
        }

        if (GUILayout.Button("Force Show Map"))
        {
            mapHandler.ForceShowMap();
        }

        if (GUILayout.Button("Force Hide Map"))
        {
            mapHandler.ForceHideMap();
        }
    }
}