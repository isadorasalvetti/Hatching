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

    public CurvatureFilter(Mesh mesh, List<List<int>> neighboors, Vector3[] principalDirections, float[] ratios)
    {
        _mesh = mesh;
        _neighboors = neighboors;
        _principalDirections = principalDirections;
        _ratios = ratios;

        _theta = new double[mesh.vertexCount];
        _ti = new Vector3[mesh.vertexCount];

        ComputePhiTheta();
        //Debug.Log("Initial Energy: " + EnergyFunction(test_theta).ToString());
        //Debug.Log("Derivatives: " + showArray(EnergyGradient(test_theta)));
        //Debug.Log("Current Curvatures: " + showArray(_principalDirections));
        
        /*
        double EPS = 1e-5;	
        double[] gradient = EnergyGradient(_theta);
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
    private Vector3[] _principalDirections;
    private float[] _ratios;

    private double[] _theta;
    private double[] _solutionTheta;
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
                result -= Math.Cos(4 * ((theta[i]-_phi[(i, _j)]) - (theta[_j]-_phi[(_j, i)])));
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
                    result[i] -= -4 * Math.Sin(4*(theta[i] - theta[_j] - _phi[(i, _j)] + _phi[(_j, i)]));
                    result[_j] -= 4 * Math.Sin(4*(theta[i] - theta[_j] - _phi[(i, _j)] + _phi[(_j, i)]));
                }
        }
        return result;
    };

    public void MinimizeEnergy(){
        var lbfgs = new BroydenFletcherGoldfarbShanno(numberOfVariables: _theta.Length,
                                                      function: EnergyFunction, gradient: EnergyGradient);
        double success = lbfgs.Minimize(_theta);
        _solutionTheta = lbfgs.Solution;
        Debug.Log("Initial Energy: " + EnergyFunction(_theta).ToString());
        Debug.Log("Optimal Energy: " + EnergyFunction(_solutionTheta).ToString());
        Debug.Log("Thetas: " + string.Join(", ", new List<double>(_solutionTheta).ConvertAll(j => j.ToString())));
    }

    public Vector3[] getNewVectors(){
        Vector3[] filteredCurvatures = new Vector3[_mesh.vertexCount];
        for(int i=0; i< _mesh.vertexCount; i++){
            float degrees = Math2.radToDegree((float) _solutionTheta[i]);
            filteredCurvatures[i] = Quaternion.AngleAxis(degrees, _mesh.normals[i])*_ti[i];
        }
        return filteredCurvatures;
    }

    void ComputePhiTheta(){
        for(int i=0; i< _mesh.vertexCount; i++){
            Vector3 vertexCurvature = _principalDirections[i].normalized;
            Vector3 vertexNormal = _mesh.normals[i].normalized;
            Vector3 vi =_mesh.vertices[i];
            Vector3 ti = vertexCurvature;
            
            _theta[i] = 0;
            _ti[i] = ti;

            List<int> neighboors = _neighboors[i];
            for(int j=0; j<neighboors.Count; j++){
                int _j = _mesh.triangles[neighboors[j]];
                Vector3 vj = _mesh.vertices[_j];
                Vector3 vivj = vj - vi;
                Vector3 vivjDir = (vivj - (Vector3.Dot(vivj, vertexNormal)) * vertexNormal).normalized;
                float dot = Vector3.Dot(vivjDir, ti);
                Vector3 cross = Math2.Cross(ref vivjDir, ref ti);
                if (Vector3.Dot(vertexNormal, cross) < 0) _phi[(i, _j)] = -Mathf.Acos(dot);
                else _phi[(i, _j)] = Mathf.Acos(dot);
            }
        }
        Debug.Log("Thetas: " + string.Join(", ", new List<double>(_theta).ConvertAll(j => j.ToString())));
        Debug.Log("Phis: " + string.Join(", ", new List<double>(_phi.Values).ConvertAll(j => j.ToString())));  
    }

    public Vector3[] AlignDirections()
    {
        int[] triangles = _mesh.triangles;
        Vector3[] normals = _mesh.normals;
        
        for (int i = 0; i < triangles.Length/3; i++)
        {
            int tria = triangles[i*3+0];
            int trib = triangles[i*3+1];
            int tric = triangles[i*3+2];
            
            float maxConsistency = 0;
            var maxConsistencyCoef = (-1, -1, -1);
            
            //Find maximun consisntency
            for (int k =0; k < 2; k++)
                for (int l =0; l < 2; l++)
                    for (int m = 0; m < 2; m++)
                    {
                        Vector3 Di = (-2*k+1)*_principalDirections[tria];
                        Vector3 Dj = (-2*l+1)*_principalDirections[trib];
                        Vector3 Dk = (-2*l+m)*_principalDirections[tric];
                        //int consistency_index = k + l * 4 + m * 16;
                        float my_consistency = Math2.aTb(Di, Dj) + Math2.aTb(Dj, Dk) + Math2.aTb(Dk, Di);
                        if (my_consistency > maxConsistency)
                        {
                            maxConsistency = my_consistency;
                            maxConsistencyCoef = (k, l, m);
                        }
                    }
            
            //Assign the max consistent vectors per triangle
            int inda = maxConsistencyCoef.Item1;
            int indb = maxConsistencyCoef.Item2;
            int indc = maxConsistencyCoef.Item3;
            _principalDirections[tria] = (-2*inda+1)*_principalDirections[tria];
            _principalDirections[trib] = (-2*indb+1)*_principalDirections[trib];
            _principalDirections[tric] = (-2*indc+1)*_principalDirections[tric];
        }
        
        return _principalDirections;
    }

    private Vector3 CtoV(Color c){
        return new Vector3(c.r, c.g, c.b);
    }

}