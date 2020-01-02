using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(GetCurvatures))]
public class ObjectBuilderEditor : Editor
{
    public float reliabilityRatio = 0.5f;
    
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
        if(GUILayout.Button("Compute Curvatures (Rossl)")) myScript.ComputeCurvatureRossl();
        else if(GUILayout.Button("Optimize Current Directions")) myScript.OptimizePrincipalDirections(reliabilityRatio);
        if(GUILayout.Button("Align Current Directions")) myScript.AlignCurvatures();
        else if(GUILayout.Button("Show Normals")) myScript.ShowNormals();
        GUILine();
        if(GUILayout.Button("Get optimization test values")) myScript.TestCurvatureOptimization();
        
    }
}
