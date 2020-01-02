using System;
using System.Collections;
using System.Collections.Generic;
using Custom.Singleton;
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

    public Vector3[] MinimizeEnergy(){
        Debug.Log("Start Minimization.");
        ComputePhiTheta();
        var lbfgs = new BroydenFletcherGoldfarbShanno(numberOfVariables: _theta.Length,
                                                      function: EnergyFunction, gradient: EnergyGradient);
        double success = lbfgs.Minimize(_theta);
        _solutionTheta = lbfgs.Solution;
        Debug.Log("Initial Energy: " + EnergyFunction(_theta).ToString());
        Debug.Log("Optimal Energy: " + EnergyFunction(_solutionTheta).ToString());
        return getNewVectors();
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
        //Debug.Log("Thetas: " + string.Join(", ", new List<double>(_theta).ConvertAll(j => j.ToString())));
        //Debug.Log("Phis: " + string.Join(", ", new List<double>(_phi.Values).ConvertAll(j => j.ToString())));  
    }

    public Vector3[] AlignDirections()
    {
        Debug.Log("Aligned curvatures");
        Debug.Log("Starting with" + showArray(_principalDirections));
        
        int[] triangles = _mesh.triangles;
        bool[] frozenTriangles = new bool[_mesh.vertices.Length];
        Vector3[] normals = _mesh.normals;
        
        // Pool of triangles to evaluate next
        List<int> trianglePool = new List<int>();
        trianglePool.Add(0);

        for(int count=0; count < 100000; count++)
        {
            //Stop if there are no availabl triangles
            if (trianglePool.Count < 1) break;

            //Get next triangle to evaluate and its vertices
            int currentTriangle = trianglePool[0];
            trianglePool.RemoveAt(0);

            int tria = triangles[currentTriangle*3+0];
            int trib = triangles[currentTriangle*3+1];
            int tric = triangles[currentTriangle*3+2];

            // Skip if all triangle vertices are frozen
            if (frozenTriangles[tria] && frozenTriangles[trib] && frozenTriangles[tric]) continue;
            
            float maxConsistency = -3;
            var maxConsistencyCoef = (0, 0, 0);
            
            //Find maximun consisntency
            for (int k = -1; k < 2; k+=2)
                for (int l = -1; l < 2; l+=2)
                    for (int m = -1; m < 2; m+=2)
                    {
                        // Do not flip principal directions for frozen vertices
                        if(k == -1 && frozenTriangles[tria]) continue;
                        if(l == -1 && frozenTriangles[trib]) continue;
                        if(m == -1 && frozenTriangles[tric]) continue;

                        Vector3 Di = k*projectVector(_principalDirections[tria], normals[tria]);
                        Vector3 Dj = l*projectVector(_principalDirections[trib], normals[trib]);
                        Vector3 Dk = m*projectVector(_principalDirections[tric], normals[tric]);
                        
                        float my_consistency = Vector3.Dot(Di, Dj) + Vector3.Dot(Dj, Dk) + Vector3.Dot(Dk, Di);
                        if (my_consistency > maxConsistency) {
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

            frozenTriangles[tria] = true;
            frozenTriangles[trib] = true;
            frozenTriangles[tric] = true;

            //Add new triangles
            for (int i = 0; i<3; i++){
                foreach (int id in _neighboors[triangles[currentTriangle*3+i]]){
                    int tri_id = id/3;
                    if(!trianglePool.Contains(tri_id)) trianglePool.Add(tri_id);
                }
            }
        }
        Debug.Log("Finishing with" + showArray(_principalDirections));
        return _principalDirections;
    }

    private Vector3 CtoV(Color c){
        return new Vector3(c.r, c.g, c.b);
    }

    private Vector3 projectVector(Vector3 vec, Vector3 normal){
        return (vec - (Vector3.Dot(vec, normal)) * normal).normalized;           
    }

}