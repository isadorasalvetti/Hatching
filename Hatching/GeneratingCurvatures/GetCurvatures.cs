using System;
using System.Collections.Generic;
using Hatching.GeneratingCurvatures;
using UnityEngine;

public class GetCurvatures : MonoBehaviour
{
    private Color[][] _lastColors = new Color[0][];

    public void GetCurvatureRossl(){
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
        _lastColors = new Color[meshes.Length][];
        for (int m = 0; m < meshes.Length; m++){
            Mesh mesh = meshes[m].sharedMesh;
            List<List<int>> mapFromNew;

            Mesh smoothMesh = GetSmoothMesh(mesh, out mapFromNew);
            
            RosslCurvature cvr = new RosslCurvature(smoothMesh);
            cvr.ComputeCurvature();
            
            Color[] newColors = new Color[mesh.vertices.Length];
            Color[] curvatureColors = Array.ConvertAll(cvr.GetPrincipalDirections(), j => new Color(j.x, j.y, j.z, 1));
            
            int displacement = 0;
            for (int i = 0; i < curvatureColors.Length; i++){
                for (int j = 0; j < mapFromNew[i].Count; j++) {
                    displacement += j;
                    newColors[mapFromNew[i][j]] = curvatureColors[i];
                }
            }
            mesh.colors = newColors;
            _lastColors[m] = newColors;
        }
    }

    private Mesh GetSmoothMesh(Mesh mesh, out List<List<int>> mapToOld){
        Mesh smoothMesh = new Mesh();
        int[] vertexTransformation = new int[mesh.vertices.Length];
        List<Vector3> uniqueVertexPositions = new List<Vector3>();
        mapToOld = new List<List<int>>();
    
        int removedVertives = 0;
        for (int i = 0; i < mesh.vertices.Length; i++){
            int firstIndex = uniqueVertexPositions.IndexOf(mesh.vertices[i]);
            if (firstIndex == -1){
                uniqueVertexPositions.Add(mesh.vertices[i]);
                vertexTransformation[i] = i - removedVertives;
                mapToOld.Add(new List<int>());
                mapToOld[i - removedVertives].Add(i);
            }
            else{
                vertexTransformation[i] = firstIndex;
                mapToOld[firstIndex].Add(i);
                removedVertives += 1;
            }
        }
    
        smoothMesh.vertices = uniqueVertexPositions.ToArray();
        int[] triangles = new int[mesh.triangles.Length];

        for (int i = 0; i < mesh.triangles.Length; i++){
            triangles[i] = vertexTransformation[mesh.triangles[i]];
        }

        smoothMesh.triangles = triangles;
        smoothMesh.RecalculateNormals();
        //Debug.Log("New vertices: " + smoothMesh.vertices.Length.ToString() + ", Old vertices: " + mesh.vertices.Length.ToString());
        //Debug.Log("Faces: " + string.Join(", ", new List<int>(smoothMesh.triangles).ConvertAll(j => j.ToString())));
        //Debug.Log("Normal (sample): " + smoothMesh.normals[2].ToString() + " vs: " + mesh.normals[2].ToString());
        return smoothMesh;
    }

    public void GetCurvatureRusinkiewicz(){
        Vector3[] pdir1, pdir2;
        float[] curv1, curv2, ratio;
    
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
        _lastColors = new Color[meshes.Length][];
        for (int m = 0; m < meshes.Length; m++){
            List<List<int>> mapFromNew;
            Mesh mesh = GetSmoothMesh(meshes[m].sharedMesh, out mapFromNew);
            int[] faces = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector3[] cornerAreas;
            float[] pointAreas;
        
            RusCurvature.ComputePointAndCornerAreas(vertices, faces, out pointAreas, out cornerAreas);
            RusCurvature.ComputeCurvature(vertices, normals, faces, pointAreas, cornerAreas, out pdir1, out pdir2, out curv1, out curv2);
            Color[] colors = new Color[pdir1.Length];
            ratio = new float[pdir1.Length];
            for (int i = 0; i < colors.Length; i++) ratio[i] = curv1[i] - curv2[i];
            for (int i=0; i < pdir1.Length; i++){
                pdir1[i] = Vector3.Normalize(pdir1[i]);
                colors[i] = new Color(Mathf.Abs(pdir1[i][0]), Mathf.Abs(pdir1[i][1]), Mathf.Abs(pdir1[i][2]), 0)*ratio[i];
            }
        
            Color[] newColors = new Color[meshes[m].sharedMesh.vertices.Length];
            int displacement = 0;
            for (int i = 0; i < colors.Length; i++){
                for (int j = 0; j < mapFromNew[i].Count; j++) {
                    displacement += j;
                    newColors[mapFromNew[i][j]] = colors[i];
                }
            }
            meshes[m].sharedMesh.colors = newColors;
            _lastColors[m] = newColors;
        }
    }

    public void ChangeColorScale(int scale) {
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>();
        for (int m = 0; m < _lastColors.Length; m++) {
            Mesh mesh = meshes[m].sharedMesh;
            mesh.colors = Array.ConvertAll(_lastColors[m], j => j * scale);
        }
    }

    public void ShowNormals(){
        foreach(MeshFilter meshFilter in GetComponentsInChildren<MeshFilter>()){
            Color[] colors = new Color[meshFilter.sharedMesh.colors.Length];
            for(int i = 0; i < meshFilter.sharedMesh.colors.Length; i++){
                Vector3 n = meshFilter.sharedMesh.normals[i];
                colors[i] = new Color(n[0], n[1], n[2], 1);
            }
            meshFilter.sharedMesh.colors = colors;
        }
    }

    float MaxCurvature(float[] curv){
        float max = 0;
        for (int i=0; i<curv.Length; i++){
            if (curv[i]>max) max = curv[i];
        }
        return max;
    }
}
