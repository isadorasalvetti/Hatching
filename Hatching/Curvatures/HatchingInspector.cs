using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(GetCurvatures))]
public class ObjectBuilderEditor : Editor
{
    public float reliabilityRatio = 0.85f;
    
    void GUILine( int i_height = 1 )
   {
       Rect rect = EditorGUILayout.GetControlRect(false, i_height );
       rect.height = i_height;
       EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
   }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GetCurvatures myScript = (GetCurvatures)target;
        GUILayout.Label("Compute");
        GUILine();
        if(GUILayout.Button("Compute Curvatures (Rossl)")) myScript.ComputeCurvatureRossl();
        
        GUILayout.Label("Change");
        GUILine();
        if (GUILayout.Button("Optimize Current Directions")) myScript.OptimizePrincipalDirections(reliabilityRatio);
        else if(GUILayout.Button("Rotate all directions 90 degres")) myScript.RotatePrincipalDirections(90);
        
        GUILayout.Label("View");
        GUILine();
        if(GUILayout.Button("Get optimization test values")) myScript.TestCurvatureOptimization(reliabilityRatio);
        else if(GUILayout.Button("Re-apply Directions")) myScript.ApplyPrincipalDirectios();
        else if(GUILayout.Button("Show Normals")) myScript.ShowNormals();
        else if(GUILayout.Button("View Ratios")) myScript.ShowRatios();

    }
}
