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

    public CurvatureData(int nVerts)
    {
        k1 = new float[nVerts];
        k2 = new float[nVerts];
        
        d1 = new Vector<float>[nVerts];
        d2 = new Vector<float>[nVerts];
    }
}

public struct MeshInfo
{
    public Mesh mesh;
    public int vertexCount;
    public List<List<int>> neighboohood;
    public float[] curvatureRatios;
    public Vector3[] principalDirections;

    public MeshInfo(Mesh myMesh)
    {
        mesh = myMesh;
        vertexCount = myMesh.vertexCount;
        principalDirections = new Vector3[myMesh.vertices.Length];
        curvatureRatios = new float[myMesh.vertices.Length];
        neighboohood = new List<List<int>>();
    }

}
