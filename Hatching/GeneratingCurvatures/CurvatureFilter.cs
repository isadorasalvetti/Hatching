using System;
using System.Collections;
using System.Collections.Generic;
using Hatching;
using UnityEngine;

public class CurvatureFilter
{
    private String showArray<T>(T[] arr) {
        return string.Join(", ", new List<T>(arr).ConvertAll(j => j.ToString()));
    }

    public CurvatureFilter(Mesh mesh, List<List<int>> neighboors, Vector3[] curvatures, float[] ratios)
    {
        _mesh = mesh;
        _neighboors = neighboors;
        _curvatures = curvatures;
        _ratios = ratios;

        _theta = new double[mesh.vertexCount];
        _ti = new Vector3[mesh.vertexCount];

        ComputePhiTheta();
        double[] test_theta = new double[_mesh.vertexCount];
        for (int i = 0; i < _mesh.vertexCount; i++) test_theta[i] = 0;
        Debug.Log("Initial Energy: " + EnergyFunction(test_theta).ToString());
        Debug.Log("Derivatives: " + showArray(EnergyGradient(test_theta)));
        Debug.Log("Current Curvatures: " + showArray(_curvatures));
    }

    private static Mesh _mesh;
    private static List<List<int>> _neighboors;
    private Vector3[] _curvatures;
    private float[] _ratios;

    private double[] _theta;
    private Vector3[] _ti;
    private static Dictionary<(int x, int y), double> _phi = new Dictionary<(int x, int y), double>();

    Func<double[], double> EnergyFunction = delegate(double[] theta){
        double result = 0;
        for(int i=0; i< _mesh.vertexCount; i++){
            List<int> neighboors = _neighboors[i];
            for(int j=0; j<neighboors.Count; j++){
                int _j = _mesh.triangles[neighboors[j]];
                //if (!_phi.ContainsKey((i, _j))) Debug.Log("Phi does not contain " + (i, _j).ToString());
                //else if (!_phi.ContainsKey((_j, i))) Debug.Log("!Phi does not contain " + (_j, i).ToString());
                result += Math.Cos(4 * ((theta[i]-_phi[(i, _j)]) - (theta[_j]-_phi[(_j, i)])));
            }
        }
        return result;
    };

    Func<double[], double[]> EnergyGradient = delegate(double[] theta){
        double[] result = new double[theta.Length];        
        for(int i=0; i< _mesh.vertexCount; i++){
            List<int> neighboors = _neighboors[i];
                for(int j=0; j<neighboors.Count; j++){
                    int _j = _mesh.triangles[neighboors[j]];
                    result[i] += -4 * Math.Sin(4*(theta[i] - theta[_j] - _phi[(i, _j)] + _phi[(_j, i)]));
                    result[_j] += 4 * Math.Sin(4*(theta[i] - theta[_j] - _phi[(i, _j)] + _phi[(_j, i)]));
                }
        }
        return result;
    };

    public Vector3[] MinimizeEnergy(){
        var lbfgs = new BroydenFletcherGoldfarbShanno(numberOfVariables: _theta.Length,
                                                      function: EnergyFunction, gradient: EnergyGradient);
        double success = lbfgs.Minimize(_theta);
        double[] solution = lbfgs.Solution;
        Debug.Log("Thetas: " + string.Join(", ", new List<double>(solution).ConvertAll(j => j.ToString())));
        Vector3[] newVectors = getNewVectors(solution);
        Debug.Log("Optimized vectors: " + showArray(newVectors));
        return newVectors;
    }

    Vector3[] getNewVectors(double[] theta){
        Vector3[] filteredCurvatures = new Vector3[_mesh.vertexCount];
        for(int i=0; i< _mesh.vertexCount; i++){
            float degrees = Math2.radToDegree((float) theta[i]);
            filteredCurvatures[i] = _ti[i];
        }
        return filteredCurvatures;
    }

    void ComputePhiTheta(){
        for(int i=0; i< _mesh.vertexCount; i++){
            Vector3 vertexCurvature = _curvatures[i];
            Vector3 vertexNormal = _mesh.normals[i];
            Vector3 vi =_mesh.vertices[i];
            Vector3 ti = vertexCurvature;
            
            //float angle = Mathf.Acos(Vector3.Dot(vertexCurvature, ti));
            //Vector3 cross = Math2.Cross(ref _curvatures[i], ref ti);
            //if (Vector3.Dot(vertexNormal, cross) < 0) _theta[i] = -angle;
            //else _theta[i] = angle;
            _theta[i] = 0;
            _ti[i] = ti;

            List<int> neighboors = _neighboors[i];
            for(int j=0; j<neighboors.Count; j++){
                int _j = _mesh.triangles[neighboors[j]];
                Vector3 vj = _mesh.vertices[_j];
                Vector3 vivj = vj - vi;
                Vector3 vivjDir = vivj - (Vector3.Dot(vivj, vertexNormal)) * vertexNormal.normalized;
                _phi[(i, _j)] = Mathf.Acos(Vector3.Dot(vivjDir.normalized, ti));
            }
        }
        Debug.Log("Thetas: " + string.Join(", ", new List<double>(_theta).ConvertAll(j => j.ToString())));
        Debug.Log("Phis: " + string.Join(", ", new List<double>(_phi.Values).ConvertAll(j => j.ToString())));  
    }

    private Vector3 CtoV(Color c){
        return new Vector3(c.r, c.g, c.b);
    }

}