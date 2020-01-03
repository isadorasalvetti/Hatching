﻿using System;
using System.Collections.Generic;
using UnityEngine;

public class GetCurvatures : MonoBehaviour
{
    private MeshFilter[] _meshes;

    //Smooth Mesh
    private List<List<int>>[] _mapFromNew;

    //Principal Directions
    private CurvatureData[] _curvatureDatas;
    private MeshInfo[] _meshInfos;


    private bool Initialize(){
        bool returnFalse = false;
        if (_meshes == null) {
            _meshes = GetComponentsInChildren<MeshFilter>();
            returnFalse = true;
        }
        if (_meshInfos == null) {
            Mesh[] smoothMesh;
            GetAllSmoothMeshes(out smoothMesh, out _mapFromNew);
            _meshInfos = new MeshInfo[smoothMesh.Length]; 
            _curvatureDatas = new CurvatureData[smoothMesh.Length];
            for (int i = 0; i < _meshes.Length; i++) {
                _meshInfos[i] = new MeshInfo(smoothMesh[i]);
                _curvatureDatas[i] = new CurvatureData(smoothMesh[i].vertexCount);
            }
            returnFalse = true;
        }

        if(returnFalse) return false;
        return true;
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
        Initialize();
        for (int m = 0; m < _meshes.Length; m++){
            RosslCurvature.ComputeCurvature(ref _meshInfos[m], out _curvatureDatas[m]);
        }
        ApplyPrincipalDirectios();
    }

    public void AlignCurvatures(){
        if (!Initialize() || _meshInfos[0].principalDirections == null) {
            Debug.Log("Curvatures not computed");
            return;
        }

        for (int m = 0; m < _meshInfos.Length; m++){
            MeshInfo meshInfo = _meshInfos[m];
            if (meshInfo.principalDirections.Length < 1){
                Debug.Log("Principal directions not computed");
                return;
            }
            _meshInfos[m].principalDirections = CurvatureFilter.AlignDirections(_meshInfos[m]);
        }
        ApplyPrincipalDirectios();
    }
    
    public void OptimizePrincipalDirections(float reliabilityRatio)
    {
        if (!Initialize() || _meshInfos[0].principalDirections == null) {
            Debug.Log("Curvatures not computed");
            return;
        }
        
        for (int m=0; m < _meshInfos.Length; m++) {
            MeshInfo meshInfo = _meshInfos[m];
            if (meshInfo.principalDirections.Length < 1) {
                Debug.Log("Principal directions not computed");
                return;
            }
            bool[] curvatureReliability = CurvatureFilter.GetReliability(meshInfo.curvatureRatios, reliabilityRatio);
            meshInfo.principalDirections = CurvatureFilter.MinimizeEnergy(meshInfo, curvatureReliability);
        }
        ApplyPrincipalDirectios();
    }
    

    public void TestCurvatureOptimization(float reliabilityRatio){
        if (!Initialize() || _meshInfos[0].principalDirections == null) {
            Debug.Log("Curvatures not computed");
            return;
        }
        
        for (int m=0; m < _meshInfos.Length; m++) {
            MeshInfo meshInfo = _meshInfos[m];
            if (meshInfo.principalDirections.Length < 1) {
                Debug.Log("Principal directions not computed");
                return;
            }
            bool[] curvatureRatio = CurvatureFilter.GetReliability(meshInfo.curvatureRatios, reliabilityRatio);
            CurvatureFilter.TestEnergyResults(meshInfo, curvatureRatio);
        }
    }

    public void ApplyPrincipalDirectios(){
        Debug.Log("Applied principal directions as colors");
        for (int m = 0; m < _meshes.Length; m++){
            Mesh mesh = _meshes[m].sharedMesh;
            Color[] newColors = new Color[mesh.vertices.Length];
            Color[] curvatureColors = Array.ConvertAll(_meshInfos[m].principalDirections, j => new Color(j.x, j.y, j.z, 1));

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
        foreach (var t in curv){
            if (t>max) max = t;
        }
        return max;
    }
}
