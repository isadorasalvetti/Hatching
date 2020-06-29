using System;
using System.Collections;
using System.Collections.Generic;
using Custom.Singleton;
using Hatching;
using UnityEngine;

public class CurvatureFilter
{
    public static String showArray<T>(T[] arr) {
        return string.Join(", ", new List<T>(arr).ConvertAll(j => j.ToString()));
    }

    private float[] _ratios;
    private float minRatio;

    private static Mesh _mesh;
    private static List<List<int>> _neighboors;
    private static double[] _theta;
    private static Vector3[] _ti;
    private static bool[] _directionIsReliable;
    private static Dictionary<(int x, int y), double> _phi = new Dictionary<(int x, int y), double>();

    private static void SetUpMinimizationData(MeshInfo info, bool[] directionIsRealiable) {
        _mesh = info.mesh;
        _neighboors = info.neighboohood;
        _theta = new double[info.vertexCount];
        _ti = new Vector3[info.vertexCount];
        _directionIsReliable = directionIsRealiable;

        int vertices = 0;
        foreach(bool reliability in _directionIsReliable)
            if (!reliability)
                vertices += 1;
        Debug.Log("Found " + vertices.ToString() + " unreliable vertices to optimize, out of " + _directionIsReliable.Length.ToString() + ".");
    }

    public static bool[] GetReliability(float[] ratios, float minRatio) {
        bool[] directionIsReliable = new bool[ratios.Length];
        for (int i = 0; i < ratios.Length; i++) directionIsReliable[i] = Mathf.Abs(ratios[i]) > minRatio;
        return directionIsReliable;
    }

    static Func<double[], double> EnergyFunction = delegate(double[] theta){
        double result = 0;
        for(int i=0; i< _mesh.vertexCount; i++){
            if (_directionIsReliable[i]) continue;
            List<int> neighboors = _neighboors[i];
            for(int j=0; j<neighboors.Count; j++){
                int _j = _mesh.triangles[neighboors[j]];
                result -= Math.Cos(4 * ((theta[i]-_phi[(i, _j)]) - (theta[_j]-_phi[(_j, i)])));
            }
        }
        return result;
    };

    public static void EnergyGradient(double[] theta, ref double func, double[] grad, object obj){
        func = 0;
        for(int i=0; i< _mesh.vertexCount; i++){
            if (_directionIsReliable[i]) continue;
            List<int> neighboors = _neighboors[i];
            for(int j=0; j<neighboors.Count; j++){
                int _j = _mesh.triangles[neighboors[j]];
                func -= Math.Cos(4 * ((theta[i]-_phi[(i, _j)]) - (theta[_j]-_phi[(_j, i)])));
            }
        }
        
        for(int i=0; i< _mesh.vertexCount; i++){
            if (_directionIsReliable[i]) continue;
            List<int> neighboors = _neighboors[i];
                for(int j=0; j<neighboors.Count; j++){
                    int _j = _mesh.triangles[neighboors[j]];
                    grad[i] -= -4 * Math.Sin(4*(theta[i] - theta[_j] - _phi[(i, _j)] + _phi[(_j, i)]));
                    grad[_j] -= 4 * Math.Sin(4*(theta[i] - theta[_j] - _phi[(i, _j)] + _phi[(_j, i)]));
                }
        }
    }

    public static void TestEnergyResults(MeshInfo meshInfo, bool[] directionIsReliable, int results=30)
    {
        SetUpMinimizationData(meshInfo, directionIsReliable);
        ComputePhiTheta(meshInfo);
        
        Debug.Log("DATA:");
        Debug.Log("Reliability: " + showArray(_directionIsReliable));
        Debug.Log("Thetas: " + showArray(_theta));
        Debug.Log("Phis: " + string.Join(", ", new List<double>(_phi.Values).ConvertAll(j => j.ToString())));  
        
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
        Debug.Log("ANALITIC GRADIENT " + showArray(numericalGradient));
        Debug.Log("NUMERICAL GRADIENT " + showArray(numericalGradient));
        
        Debug.Log("-------");

        double[] Energies = new double[results];
        float increment = Mathf.PI / 30;

        double[] theta = (double[]) _theta.Clone();
        for (int i = 0; i < results; i++)
        {
            theta[0] += increment;
            theta[1] -= increment;
            Energies[i] = EnergyFunction(theta);
        }
        
        Debug.Log("Energies: " + showArray(Energies));
        Debug.Log("Increment per step: " + increment.ToString());
        */
    }

    /*
    public static Vector3[] MinimizeEnergy(MeshInfo meshInfo, bool[] directionIsReliable){
        Debug.Log("Start Minimization.");
        SetUpMinimizationData(meshInfo, directionIsReliable);
        ComputePhiTheta(meshInfo);
        var lbfgs = new BroydenFletcherGoldfarbShanno(numberOfVariables: _theta.Length,
            function: EnergyFunction, gradient: EnergyGradient);
        double success = lbfgs.Minimize(_theta);
        double[] solutionTheta = lbfgs.Solution;
        Debug.Log("Initial Energy: " + EnergyFunction(_theta).ToString());
        Debug.Log("Optimal Energy: " + EnergyFunction(solutionTheta).ToString());
        return getNewVectors(solutionTheta);
    }
    */
    
    public static Vector3[] MinimizeEnergy(MeshInfo meshInfo, bool[] directionIsReliable){
        Debug.Log("----- Start Minimization.");
        SetUpMinimizationData(meshInfo, directionIsReliable);
        ComputePhiTheta(meshInfo);
        alglib.minlbfgsstate lbfgs;
        alglib.minlbfgscreate(1, _theta, out lbfgs);
        
        // Run
        double[] solutionTheta;
        alglib.minlbfgsreport report;
        alglib.minlbfgsoptimize(lbfgs, EnergyGradient, null, null);
        alglib.minlbfgsresults(lbfgs, out solutionTheta, out report);
        alglib.minlbfgssetcond(lbfgs, 0, 0, 0, 0);
        
        Debug.Log("Initial Energy: " + EnergyFunction(_theta).ToString());
        Debug.Log("Optimal Energy: " + EnergyFunction(solutionTheta).ToString());
        Debug.Log("Initial Theta: " + showArray(_theta));
        Debug.Log("Optimal Theta: " + showArray(solutionTheta).ToString());
        Debug.Log("Report: " + report.terminationtype.ToString() + ", " + report.iterationscount.ToString());
        
        return getNewVectors(solutionTheta);
    }

    public static Vector3[] getNewVectors(double[] solutionTheta){
        Vector3[] filteredCurvatures = new Vector3[_mesh.vertexCount];
        for(int i=0; i< _mesh.vertexCount; i++){
            float degrees = Math2.radToDeg((float) 1.5);
            filteredCurvatures[i] = Quaternion.AngleAxis(degrees, _mesh.normals[i])*_ti[i];
        }
        return filteredCurvatures;
    }

    public static void ComputePhiTheta(MeshInfo meshInfo){
        for(int i=0; i< meshInfo.vertexCount; i++){
            Vector3 vertexCurvature = meshInfo.principalDirections[i].normalized;
            Vector3 vertexNormal = meshInfo.mesh.normals[i].normalized;
            Vector3 vi = meshInfo.mesh.vertices[i];
            Vector3 ti = vertexCurvature;
            
            _theta[i] = 0;
            _ti[i] = ti;

            List<int> neighboors = _neighboors[i];
            for(int j=0; j<neighboors.Count; j++){
                int _j = meshInfo.mesh.triangles[neighboors[j]];
                Vector3 vj = meshInfo.mesh.vertices[_j];
                Vector3 vivj = vj - vi;
                Vector3 vivjDir = (vivj - (Vector3.Dot(vivj, vertexNormal)) * vertexNormal).normalized;
                float dot = Vector3.Dot(vivjDir, ti);
                Vector3 cross = Math2.Cross(ref vivjDir, ref ti);
                if (Vector3.Dot(vertexNormal, cross) < 0) _phi[(i, _j)] = -Mathf.Acos(dot);
                else _phi[(i, _j)] = Mathf.Acos(dot);
            }
        }
    }

    public static void AlignDirections(MeshInfo meshInfo, bool connectivity)
    {
        Debug.Log("Aligning curvatures");
        Debug.Log(String.Format("Triangles: {0}, Vertices: {1}, Colors:{2}", 
            meshInfo.mesh.triangles.Length, meshInfo.mesh.vertices.Length, meshInfo.mesh.colors.Length));

        int[] triangles = meshInfo.mesh.triangles;
        bool[] frozenTriangles = new bool[meshInfo.mesh.vertices.Length];
        Vector3[] normals = meshInfo.approximatedNormals;
        int maxIterations;
        List<int> trianglePool = new List<int>();
        
        if (connectivity) {
            trianglePool.Add(0);
            maxIterations = 1000000000;
        }
        
        else maxIterations = triangles.Length/3;

        for(int count=0; count < maxIterations; count++)
        {
            int currentTriangle;
            if (connectivity) { // Not in use - currently using a mesh where vertices are not shared between triangles.
                if (trianglePool.Count < 1) {
                    Debug.Log(string.Format("Finished with {0} iteractions", count));
                    break;
                }
                currentTriangle = trianglePool[0];
                trianglePool.RemoveAt(0);
            }
            else currentTriangle = count;
            
            int tria = triangles[currentTriangle*3+0];
            int trib = triangles[currentTriangle*3+1];
            int tric = triangles[currentTriangle*3+2];

            // Skip if all triangle vertices are frozen
            if (frozenTriangles[tria] && frozenTriangles[trib] && frozenTriangles[tric]) continue;
            
            float maxConsistency = -3;
            var maxConsistencyCoef = (0, 0, 0);

            for (int k = 0; k < 4; k+=1)
                for (int l = 0; l < 4; l+=1)
                    for (int m = 0; m < 4; m+=1)
                    {
                        // Do not change principal directions for frozen vertices
                        if(k != 0 && frozenTriangles[tria]) continue;
                        if(l != 0 && frozenTriangles[trib]) continue;
                        if(m != 0 && frozenTriangles[tric]) continue;

                        Vector3 Di = meshInfo.AllPrincipalDirections[tria, k];
                        Vector3 Dj = meshInfo.AllPrincipalDirections[trib, l];
                        Vector3 Dk = meshInfo.AllPrincipalDirections[tric, m];

                        float myConsistency = (Vector3.Dot(Di, Dj))
                                               +(Vector3.Dot(Dj, Dk))
                                               +(Vector3.Dot(Dk, Di));

                        if (myConsistency > maxConsistency + 0.05f) {
                            maxConsistency = myConsistency;
                            maxConsistencyCoef = (k, l, m);
                        }
                    }
            
            //Assign the max consistent vectors per triangle
            int inda = maxConsistencyCoef.Item1;
            int indb = maxConsistencyCoef.Item2;
            int indc = maxConsistencyCoef.Item3;
            
            RotatePrincipalDirections(tria, inda, ref meshInfo.AllPrincipalDirections);
            RotatePrincipalDirections(trib, indb, ref meshInfo.AllPrincipalDirections);
            RotatePrincipalDirections(tric, indc, ref meshInfo.AllPrincipalDirections);

            frozenTriangles[tria] = true;
            frozenTriangles[trib] = true;
            frozenTriangles[tric] = true;

            //Add new triangles
            if (connectivity)
                for (int i = 0; i<3; i++){
                    foreach (int id in meshInfo.neighboohood[triangles[currentTriangle*3+i]]){
                        int tri_id = id/3;
                        if(!trianglePool.Contains(tri_id)) trianglePool.Add(tri_id);
                    }
                }
        }
        
        //Debug.Log("Finishing with" + showArray(meshInfo.principalDirections));
    }

    private static Vector3 CtoV(Color c){
        return new Vector3(c.r, c.g, c.b);
    }

    private static Vector3 projectVector(Vector3 vec, Vector3 normal){
        return (vec - (Vector3.Dot(vec, normal)) * normal).normalized;           
    }

    public static void RotateAllDirections(ref Vector3[] directions, Vector3[] normals, float angle) {
        for (int i = 0; i < directions.Length; i++) {
            directions[i] = Quaternion.AngleAxis(angle, normals[i]) * directions[i];
        }
    }
    
    static void swapVec3(ref Vector3 a, ref Vector3 b) {
        Vector3 temp = a;
        a = b;
        b = temp;
    }

    static void RotatePrincipalDirections(int index, int newFirst, ref Vector3[,] directions) {
        if (newFirst == 0) return;
        Vector3 temp = directions[index, 0];
        for (int i = 0; i < newFirst; i++)
            for (int j = 0; j < 4; j++) {
                Vector3 nextDirection;
                if (j >= 3) nextDirection = temp;
                else nextDirection = directions[index, j + 1];
                directions[index, j] = nextDirection;
            }
    }

    public static void DuplicateMeshVertices(Mesh mesh, bool colors = false) {
        Debug.Log("Duplicated mesh vertices");
        Vector3[] newVertices = new Vector3[mesh.triangles.Length];
        Vector3[] newNormals = new Vector3[mesh.triangles.Length];
        Vector2[] newUV = new Vector2[mesh.triangles.Length];
        Color[] newColors = new Color[mesh.triangles.Length];
        int[] newFaces = new int[mesh.triangles.Length];
        
        for (int i = 0; i < mesh.triangles.Length; i++) {
                newFaces[i] = i;
                newVertices[i] = mesh.vertices[mesh.triangles[i]];
                if (colors) newColors[i] = mesh.colors[mesh.triangles[i]];
                newNormals[i] = mesh.normals[mesh.triangles[i]];
                //newUV[i] = mesh.uv[mesh.triangles[i]];
        }

        mesh.vertices = newVertices;
        mesh.triangles = newFaces;
        mesh.normals = newNormals;
        //mesh.uv = newUV;
        if (colors) mesh.colors = newColors;
    }

}