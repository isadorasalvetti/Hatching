using System.Collections;
using System.Collections.Generic;
using Custom.Singleton;
using UnityEditor;
using UnityEngine;

public class VertexPainter
{
    // Use to change vertex colors for visualizations
    public static void UpdateColor(Mesh mesh, int vertex, Vector3 newColor)
    {
        Color colNewColor = new Color(newColor.x, newColor.y, newColor.z, 1);
        UpdateColor(mesh, vertex, colNewColor);
    }
    
    public static void UpdateColor(Mesh mesh, int vertex, Color newColor)
    {
        mesh.colors[vertex] = newColor;
    }

    public static void ResetColors(Mesh mesh)
    {
        mesh.colors = new Color[mesh.vertices.Length];
    }

}
