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
        bool[] frozen_triangles = new bool[_mesh.vertices.Length];
        Vector3[] normals = _mesh.normals;
        
        // Pool of triangles to evaluate next
        List<int> triangle_pool = new List<int>();
        triangle_pool.Add(0);

        for(int count=0; count < 100000; count++)
        {
            //Stop if there are no availabl triangles
            if (triangle_pool.Count < 1) break;

            //Get next triangle to evaluate and its vertices
            int current_triangle = triangle_pool[0];
            triangle_pool.RemoveAt(0);

            int tria = triangles[current_triangle*3+0];
            int trib = triangles[current_triangle*3+1];
            int tric = triangles[current_triangle*3+2];

            // Skip if all triangle vertices are frozen
            if (frozen_triangles[tria] && frozen_triangles[trib] && frozen_triangles[tric]) continue;
            
            float maxConsistency = -3;
            var maxConsistencyCoef = (0, 0, 0);
            
            //Find maximun consisntency
            for (int k =-1; k < 2; k+=2)
                for (int l =-1; l < 2; l+=2)
                    for (int m = -1; m < 2; m+=2)
                    {
                        // Do not flip principal directions for frozen vertices
                        if(k < 0 && frozen_triangles[tria]) continue;
                        if(l < 0 && frozen_triangles[trib]) continue;
                        if(m < 0 && frozen_triangles[tric]) continue;

                        Vector3 Di = k*projectVector(_principalDirections[tria], normals[tria]);
                        Vector3 Dj = l*projectVector(_principalDirections[trib], normals[tria]);
                        Vector3 Dk = m*projectVector(_principalDirections[tric], normals[tria]);
                        float my_consistency = Vector3.Dot(Di, Dj) + Vector3.Dot(Dj, Dk) + Vector3.Dot(Dk, Di);
                        // Debug.Log("Index:" + k.ToString() + l.ToString() + m.ToString() + " Consitency:" + my_consistency);
                        // Debug.Log("Vertices:" + Di.ToString() + Dj.ToString() + Dk.ToString());
                        
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
            //Debug.Log("indices:" + inda.ToString() + indb.ToString() + indc.ToString());
            //Debug.Log("max consistency:" + maxConsistency);
            _principalDirections[tria] = inda*_principalDirections[tria];
            _principalDirections[trib] = indb*_principalDirections[trib];
            _principalDirections[tric] = indc*_principalDirections[tric];

            frozen_triangles[tria] = true;
            frozen_triangles[trib] = true;
            frozen_triangles[tric] = true;

            //Add new triangles
            for (int i = 0; i<3; i++){
                foreach (int id in _neighboors[triangles[current_triangle*3+i]]){
                    int tri_id = id/3;
                    if(!triangle_pool.Contains(tri_id)) triangle_pool.Add(tri_id);
                }
            }
        }
        return _principalDirections;
    }

    private Vector3 CtoV(Color c){
        return new Vector3(c.r, c.g, c.b);
    }

    private Vector3 projectVector(Vector3 vec, Vector3 normal){
        return (vec - (Vector3.Dot(vec, normal)) * normal).normalized;           
    }

}