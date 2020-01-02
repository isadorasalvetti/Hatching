using System;
using System.Collections.Generic;
using UnityEngine;

public class GetCurvatures : MonoBehaviour
{
    private MeshFilter[] _meshes;

    //Smooth Mesh
    private List<List<int>>[] _mapFromNew;
    private Mesh[] _smoothMesh;
    private List<Vector3[]> _principalDirections;
    private List<bool[]> _curvatureReliability; 

    //Principal Directions
    private RosslCurvature[] _rosslCrv;
    private CurvatureFilter[] _filter;


    private void Initialize(){
        _meshes = GetComponentsInChildren<MeshFilter>();
        GetAllSmoothMeshes(out _smoothMesh, out _mapFromNew);

        _rosslCrv =  new RosslCurvature[_meshes.Length];
        _principalDirections = new List<Vector3[]>();
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
            Mesh smoothMesh = _smoothMesh[m];
            
            _rosslCrv[m] = new RosslCurvature(smoothMesh);
            _rosslCrv[m].ComputeCurvature();

            _principalDirections.Add(_rosslCrv[m].GetPrincipalDirections());
        }
        ApplyPrincipalDirectios();
    }

    public void AlignCurvatures(){
        InitializeFilter();
        for (int m = 0; m < _meshes.Length; m++) _principalDirections[m] = _filter[m].AlignDirections();
        ApplyPrincipalDirectios();
    }
    
    public void OptimizePrincipalDirections(float reliabilityRatio)
    {
        InitializeFilter(reliabilityRatio);
        for (int m = 0; m < _meshes.Length; m++) _principalDirections[m] = _filter[m].MinimizeEnergy();
        ApplyPrincipalDirectios();
    }

    private void InitializeFilter(float reliabilityRatio=0.5f){
        _filter = new CurvatureFilter[_meshes.Length];
            for (int m = 0; m < _meshes.Length; m++){
                _filter[m] = new CurvatureFilter(_smoothMesh[m], _rosslCrv[m].GetVertexNeighboors(),
                                                _principalDirections[m], _rosslCrv[m].GetCurvatureRatio(), reliabilityRatio);
        }
    }

    public void TestCurvatureOptimization(){
        Debug.Log("Curvatures / Principal Directions must have been computed.");
        _filter[0].TestEnergyResults();
    }

    public void ApplyPrincipalDirectios(){
        Debug.Log("Applied principal directions as colors");
        for (int m = 0; m < _meshes.Length; m++){
            Mesh mesh = _meshes[m].sharedMesh;
            Color[] newColors = new Color[mesh.vertices.Length];
            Color[] curvatureColors = Array.ConvertAll(_principalDirections[m], j => new Color(j.x, j.y, j.z, 1));

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
