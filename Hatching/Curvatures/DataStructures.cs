using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;

public struct CurvatureData
{
    public float [] k1; //minor
    public float [] k2; //major
    public Vector<float> [] d1;
    public Vector<float> [] d2;
    public Vector3 normal;

    public CurvatureData(int nVerts)
    {
        k1 = new float[nVerts];
        k2 = new float[nVerts];
        
        d1 = new Vector<float>[nVerts];
        d2 = new Vector<float>[nVerts];
        
        normal = Vector3.zero;
    }
}

public struct MeshInfo
{
    public Mesh mesh;
    public int vertexCount;
    public List<List<int>> neighboohood;
    public float[] curvatureRatios;
    public Vector3[] principalDirections;
    public Vector3[,] AllPrincipalDirections;
    public Vector3[] approximatedNormals;
    public Vector2[] uvCurvatures;
    public bool uvsProjected;

    public MeshInfo(Mesh myMesh)
    {
        mesh = myMesh;
        vertexCount = myMesh.vertexCount;
        principalDirections = new Vector3[myMesh.vertices.Length];
        AllPrincipalDirections = new Vector3[myMesh.vertices.Length, 4];
        approximatedNormals = new Vector3[myMesh.vertices.Length];
        curvatureRatios = new float[myMesh.vertices.Length];
        uvCurvatures = new Vector2[myMesh.vertices.Length];
        neighboohood = new List<List<int>>();
        uvsProjected = false;
    }

    public Vector3[] GetaDirection(int index) {
        Vector3[] principalDirectionVector = new Vector3[mesh.vertices.Length];
        for (int i = 0; i < mesh.vertices.Length; i++) {
            principalDirectionVector[i] = AllPrincipalDirections[i, index];
        }

        return principalDirectionVector;
    }
}

public struct StoredCurvature {
    public Vector3[] vertexPositions;
    public Vector3[] normals;
    public Vector3[] principalDirections0;
    public Vector3[] principalDirections1;
    public Vector3[] principalDirections2;
    public Vector3[] principalDirections3;

    public StoredCurvature(MeshInfo meshInfo){
        principalDirections0 = new Vector3[meshInfo.vertexCount];
        principalDirections1 = new Vector3[meshInfo.vertexCount];
        principalDirections2 = new Vector3[meshInfo.vertexCount];
        principalDirections3 = new Vector3[meshInfo.vertexCount];
        vertexPositions = new Vector3[meshInfo.vertexCount];
        normals = new Vector3[meshInfo.vertexCount];
        for (int i = 0; i < meshInfo.vertexCount; i++) {
            principalDirections0[i] = meshInfo.AllPrincipalDirections[i, 0];
            principalDirections1[i] = meshInfo.AllPrincipalDirections[i, 1];
            principalDirections2[i] = meshInfo.AllPrincipalDirections[i, 2];
            principalDirections3[i] = meshInfo.AllPrincipalDirections[i, 3];
            vertexPositions[i] = meshInfo.mesh.vertices[i];
            normals[i] = meshInfo.approximatedNormals[i];
        }
    }
}
