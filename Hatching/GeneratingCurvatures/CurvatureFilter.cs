using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurvatureFilter
{
    CurvatureFilter(Mesh mesh, List<List<int>> neighboors, Vector3[] curvatures, int[] ratios){
        _mesh = mesh;
        _neighboors = neighboors;
        _curvatures = curvatures;
        _ratios = ratios;
    }

    private Mesh _mesh;
    private List<List<int>> _neighboors;
    private Vector3[] _curvatures;
    private int[] _ratios;

    void AlighCurvatures(out Vector3[] result){
        result = new Vector3[_mesh.vertexCount];
        for(int i=0; i< _mesh.vertexCount; i++){
            Vector3 vertexCurvature = _curvatures[i];
        }
    }

}