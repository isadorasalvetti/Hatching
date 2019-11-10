using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(GetCurvatures))]
public class ObjectBuilderEditor : Editor
{
    int brightness;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GetCurvatures myScript = (GetCurvatures)target;
        if(GUILayout.Button("Compute Curvatures (Rossl)")) myScript.GetCurvatureRossl();
        else if(GUILayout.Button("Compute Curvatures (Rusinkiewicz)")) myScript.GetCurvatureRusinkiewicz();
        else if(GUILayout.Button("Show Normals")) myScript.ShowNormals();
    }
}