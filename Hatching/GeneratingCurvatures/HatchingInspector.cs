using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(GetCurvatures))]
public class ObjectBuilderEditor : Editor
{
    int brightness;
    void GuiLine( int i_height = 1 )
   {
       Rect rect = EditorGUILayout.GetControlRect(false, i_height );
       rect.height = i_height;
       EditorGUI.DrawRect(rect, new Color ( 0.5f,0.5f,0.5f, 1 ) );
   }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GetCurvatures myScript = (GetCurvatures)target;
        if(GUILayout.Button("Compute Curvatures (Rossl)")) myScript.GetCurvatureRossl();
        if(GUILayout.Button("Filter current")) myScript.ShowNormals();
    }
}
