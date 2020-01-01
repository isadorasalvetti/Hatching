﻿using System;
using System.Collections.Generic;
using Hatching.GeneratingCurvatures;
using UnityEngine;

public class GetCurvatures : MonoBehaviour
{
    private MeshFilter[] _meshes;

    //Smooth Mesh
    private bool _initialized;
    private List<List<int>>[] _mapFromNew;
    private Mesh[] _smoothMesh;
    private List<Vector3[]> _principalDirections;

    //Principal Directions
    private RosslCurvature[] _RosslCrv;
    private void Initialize(){
        _meshes = GetComponentsInChildren<MeshFilter>();
        GetAllSmoothMeshes(out _smoothMesh, out _mapFromNew);
        _initialized = true;
    }

    private void GetAllSmoothMeshes(out Mesh[] allSmoothMeshes, out List<List<int>>[] allMapsFromNew){
        allSmoothMeshes = new Mesh[_meshes.Length];
        allMapsFromNew = new List<List<int>>[_meshes.Length];
        for (int m = 0; m < _meshes.Length; m++){
            Mesh mesh = _meshes[m].sharedMesh;
            allSmoothMeshes[m]= GetSmoothMesh(mesh, out allMapsFromNew[m]);
            }
    }

    public void ComputeCurvatureRossl(){
        if(!_initialized) Initialize();
        for (int m = 0; m < _meshes.Length; m++){
            Mesh mesh = _meshes[m].sharedMesh;

            List<List<int>> mapFromNew = _mapFromNew[m];
            Mesh smoothMesh = _smoothMesh[m];
            
            _RosslCrv[m] = new RosslCurvature(smoothMesh);
            _RosslCrv[m].ComputeCurvature();

            var filter = new CurvatureFilter(smoothMesh, _RosslCrv[m].GetVertexNeighboors(),
                                            _RosslCrv[m].GetPrincipalDirections(), _RosslCrv[m].GetCurvatureRatio());
            _principalDirections[m] = filter.AlignDirections();
        }
        ApplyPrincipalDirectios(_principalDirections);
    }

    public void ApplyPrincipalDirectios(List<Vector3[]> principalDirections){
        for (int m = 0; m < _meshes.Length; m++){
            Mesh mesh = _meshes[m].sharedMesh;
            Color[] newColors = new Color[mesh.vertices.Length];
            Color[] curvatureColors = Array.ConvertAll(principalDirections[m], j => new Color(j.x, j.y, j.z, 1));

            int displacement = 0;
            for (int i = 0; i < curvatureColors.Length; i++){
                for (int j = 0; j < _mapFromNew[m][i].Count; j++) {
                    displacement += j;
                    newColors[_mapFromNew[m][i][j]] = curvatureColors[i];
                }
            mesh.colors = newColors;
            }
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
