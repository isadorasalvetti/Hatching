using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CurvatureFilter
{
    private String showArray(double[] arr) {
        return string.Join(", ", new List<double>(arr).ConvertAll(j => j.ToString()));
    }
    public CurvatureFilter(Mesh mesh, List<List<int>> neighboors, Vector3[] curvatures, float[] ratios){
        _mesh = mesh;
        _neighboors = neighboors;
        _curvatures = curvatures;
        _ratios = ratios;

        _theta = new double[mesh.vertexCount];

        ComputePhiTheta();
        double[] test_theta = new double[_mesh.vertexCount];
        for(int i = 0; i < _mesh.vertexCount; i++) test_theta[i] = 0;
        Debug.Log("Initial Energy: " + EnergyFunction(test_theta).ToString());
        double[] gradient = EnergyGradient(_theta);
        Debug.Log(showArray(gradient));

        /*
        double EPS = 1e-5;
        double[] numericalGradient = new double[_theta.Length];
        for (int i = 0; i < _theta.Length; ++i) {
            _theta[i] += EPS;
            double energyForward = EnergyFunction(_theta);
            _theta[i] -= 2*EPS;
            double energyBackward = EnergyFunction(_theta);
            _theta[i] += EPS;
            numericalGradient[i] = (energyForward - energyBackward)/(2*EPS);
            if (Math.Abs(numericalGradient[i] - gradient[i]) > 1e-2) {
                Debug.Log("numerical " + numericalGradient[i].ToString());
                Debug.Log("analytic " + gradient[i].ToString());
                throw new Exception("GRADIENT has problem");
            }
        }
        Debug.Log("NUMERICAL GRADIENT " + showArray(numericalGradient));
        */
    }

    private static Mesh _mesh;
    private static List<List<int>> _neighboors;
    private Vector3[] _curvatures;
    private float[] _ratios;

    private double[] _theta;
    private static Dictionary<(int x, int y), double> _phi = new Dictionary<(int x, int y), double>();

    private Vector3 ti = new Vector3(1, 0, 0);

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

    public double[] MinimizeEnergy(){
        var lbfgs = new BroydenFletcherGoldfarbShanno(numberOfVariables: _phi.Keys.Count,
                                                      function: EnergyFunction, gradient: EnergyGradient);
        double[] solution = lbfgs.Solution;
        Debug.Log("Thetas: " + string.Join(", ", new List<double>(solution).ConvertAll(j => j.ToString())));
        return solution;
    }

    Vector3[] getNewVectors(){
        Vector3[] filteredCurvatures = new Vector3[_mesh.vertexCount];
        for(int i=0; i< _mesh.vertexCount; i++){
           filteredCurvatures[i] = _curvatures[i];
        }
        return filteredCurvatures;
    }

    void ComputePhiTheta(){
        for(int i=0; i< _mesh.vertexCount; i++){
            Vector3 vertexCurvature = _curvatures[i];
            Vector3 vertexNormal = _mesh.normals[i];
            Vector3 vi =_mesh.vertices[i];

            _theta[i] = Mathf.Acos(Vector3.Dot(_curvatures[i], ti));

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