using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;
using Complex = System.Numerics.Complex;

public static class RosslCurvature {
    static int Next(int corner) {
        return 3 * (corner / 3) + (corner + 1) % 3;
    }

    static int Previous(int corner) {
        return 3 * (corner / 3) + (corner + 2) % 3;
    }

    public static float[] ComputeCurvatureRatio(float[] k1, float[] k2) {
        if(k1 != null){
            float[] ratios = new float [k1.Length];
            for (int i = 0; i < ratios.Length; i++) {
                ratios[i] = Mathf.Abs(Mathf.Abs(k2[i]) - Mathf.Abs(k1[i])) / (Mathf.Abs(k2[i]) + Mathf.Epsilon); //(Major - Minor) / Major
            }
            return ratios;
        }
        Debug.Log("Curvature information has not been computed"); return new float[0];
    }

    private static String showArray<T>(T[] arr) {
        return string.Join(", ", new List<T>(arr).ConvertAll(j => j.ToString()));
    }
    
    public static void ComputeCurvature(ref MeshInfo meshInfo, out CurvatureData outData){
        int n = meshInfo.vertexCount;
        outData = new CurvatureData(n);

        int[] cornerTable = BuildCornerTable(meshInfo.mesh);
        
        for (int i = 0; i < n; i++){
            List<int> neighboors = GetOrderedNeighboors(meshInfo.mesh, i, cornerTable);
            meshInfo.neighboohood.Add(neighboors);
            float[] r, phi;
            Matrix<float> F;
            MakeExponentialMap(meshInfo.mesh, i, neighboors, out r, out phi);
            GetUVF(meshInfo.mesh, r, phi, neighboors.ToArray(), i, out F);
            GetCurvatures(F, i, ref outData);
            meshInfo.principalDirections[i] = ParametricTo3D(vectorToUnity(F.Row(0)), vectorToUnity(F.Row(1)), outData.d1[i][0], outData.d1[i][1]);
        }

        meshInfo.curvatureRatios = ComputeCurvatureRatio(outData.k1, outData.k2);
    }

    static Vector3 vectorToUnity(Vector<float> v){
        return new Vector3(v[0], v[1], v[2]);
    }

    static int[] BuildCornerTable(Mesh mesh){
        List<int[]> cornerEdges = new List<int[]>(new int[mesh.triangles.Length][]);
        int[] cornerTable = new int[mesh.triangles.Length];

        //Debug.Log("Number of vertices = " + _mesh.vertices.Length.ToString() + ", Number of faces = " + _mesh.triangles.Length.ToString());

        for (int i = 0; i < mesh.triangles.Length/3; i++){
            int[] vector = new int[3] {mesh.triangles[i*3+0], mesh.triangles[i*3+1], mesh.triangles[i*3+2]};
            int[] edge0 = new int[2] {Mathf.Min(vector[1], vector[2]), Mathf.Max(vector[1], vector[2])}; // 0
            int[] edge1 = new int[2] {Mathf.Min(vector[0], vector[2]), Mathf.Max(vector[0], vector[2])}; // 1
            int[] edge2 = new int[2] {Mathf.Min(vector[0], vector[1]), Mathf.Max(vector[0], vector[1])}; // 2

            cornerEdges[i*3+0] = new int[2] {edge0[0], edge0[1]};
            cornerEdges[i*3+1] = new int[2] {edge1[0], edge1[1]};
            cornerEdges[i*3+2] = new int[2] {edge2[0], edge2[1]};
        }
        
        for (int i = 0; i < cornerEdges.Count; i++){
            int[] e1 = cornerEdges[i];
            cornerTable[i] = -1;
            for (int j = 0; j < cornerEdges.Count; j++){
                int[] e2 = cornerEdges[j];
                if (i!=j && e1[0] == e2[0] && e1[1] == e2[1]){
                    cornerTable[i] = j;
                    cornerTable[j] = i;
                    break;
                }
            }
        }

        return cornerTable;
        //Debug.Log("Corner Table = " + string.Join(", ", new List<int>(_cornerTable).ConvertAll(j => j.ToString()).ToArray()));
    }

    static List<int> GetOrderedNeighboors(Mesh mesh, int vert, int[] cornerTable){
        List<int> vertexNeighboors = new List<int>();
        List<int> temp = new List<int>();

        int firstCorner = Next(mesh.triangles.ToList().IndexOf(vert));
        vertexNeighboors.Add(firstCorner);
        int nextCorner = firstCorner;
        for (int i = 0; i < mesh.triangles.Length/3; i++)
        {
            nextCorner = cornerTable[Next(nextCorner)];
            if (nextCorner == firstCorner) break;
            if (nextCorner > -1) vertexNeighboors.Add(nextCorner);
            else{ //needs fix, remove vertex
                break;
            }
        }
        return vertexNeighboors;
    }

    static void MakeExponentialMap(Mesh mesh, int vert, List<int> neighboors, out float[] r, out float[] phi){
        // Move vertices such that v = (0, 0, 0)
        Vector3 v0 = mesh.vertices[vert];
        Vector3[] vn = new Vector3[neighboors.Count];
        r = new float[neighboors.Count];
        phi = new float[neighboors.Count];

        // First neighboor - begining of the polar coordinates
        vn[0] = mesh.vertices[mesh.triangles[neighboors[0]]] - v0;
        r[0] = vn[0].magnitude;
        phi[0] = 0;

        float accumulatedAngle = 0;
        for (int i = 1; i < neighboors.Count; i++){
            vn[i] = mesh.vertices[mesh.triangles[neighboors[i]]] - v0;
            r[i] = vn[i].magnitude;
            accumulatedAngle += Mathf.Acos(Mathf.Clamp(Vector3.Dot(vn[i].normalized, vn[i-1].normalized), -1, 1));
            phi[i] = accumulatedAngle;
        }
        float maxAngle = accumulatedAngle + Mathf.Acos(Vector3.Dot(vn[neighboors.Count-1].normalized, vn[0].normalized));
        ScalePhi(ref phi, maxAngle);

        //Debug.Log("Neighbor Amount: " + neighboors.Count.ToString());
        //Debug.Log("Phi: " + string.Join(", ", new List<float>(phi).ConvertAll(j => j.ToString()).ToArray()));
        //Debug.Log("R: " + string.Join(", ", new List<float>(r).ConvertAll(j => j.ToString()).ToArray()));
    }

    static void GetUVF(Mesh mesh, float[] r, float[] phi, int[] neighbors, int v, out Matrix<float> F){
        float[,] V = new float[r.Length, 5];
        float[,] Q = new float[r.Length, 3];

        for (int i = 0; i < r.Length; i++){
            Vector3 vert = mesh.vertices[mesh.triangles[neighbors[i]]] - mesh.vertices[v];
            Q[i, 0] = vert.x;
            Q[i, 1] = vert.y;
            Q[i, 2] = vert.z;
        }
        
        for (int i = 0; i < r.Length; i++){
            float ui = r[i] * Mathf.Cos(phi[i]);
            float vi = r[i] * Mathf.Sin(phi[i]);
            V[i, 0] = ui;
            V[i, 1] = vi;
            V[i, 2] = ui*ui/2;
            V[i, 3] = ui*vi;
            V[i, 4] = vi*vi/2; 
        }
        Matrix<float> Vm = DenseMatrix.OfArray(V);
        Matrix<float> Qm = DenseMatrix.OfArray(Q);

        F = GetF(Vm, Qm, r.Length);
    }

    static Matrix<float> GetF(Matrix<float> VM, Matrix<float> QM, int n){
        Matrix<float> F = DenseMatrix.OfArray(new float[1,n]);
        if(n==5) F = VM.Inverse()*QM;
        if(n<5) F = VM.Transpose()*(VM*VM.Transpose()).Inverse()*QM;
        if(n>5) F = (VM.Transpose()*VM).Inverse()*VM.Transpose()*QM;
        return F;
    }

    static void GetCurvatures(Matrix<float> F, int v, ref CurvatureData outData) { // F: Fu, Fv, Fuu, Fuv, Fvv
        //float lambda1, lambda2, k1, k2;

        Vector3 Fu = new Vector3(F[0, 0], F[0, 1], F[0, 2]);
        Vector3 Fv = new Vector3(F[1, 0], F[1, 1], F[1, 2]);
        Vector3 Nu = Vector3.Cross(Fu, Fv).normalized;
        Vector<float> N = DenseVector.OfArray(new float[]{Nu.x, Nu.y, Nu.z});
        
        float e = F.Row(0).DotProduct(F.Row(0));
        float f = F.Row(1).DotProduct(F.Row(0));
        float g = F.Row(1).DotProduct(F.Row(1));
        
        float l = F.Row(2).DotProduct(N);
        float m = F.Row(3).DotProduct(N);
        float n = F.Row(4).DotProduct(N);

        Matrix<float> mat = DenseMatrix.OfArray(new float[2,2]{{e, f}, {f, g}}) * DenseMatrix.OfArray(new float[2,2]{{l, m}, {m, n}});
        Evd<float> eigen = mat.Evd();
        Matrix<float> eigenVectors = eigen.EigenVectors;
        Vector<Complex> eigenValues = eigen.EigenValues;
        
        outData.d1[v] = eigenVectors.Row(0);
        outData.d2[v] = eigenVectors.Row(1);
        outData.k1[v] = (float)eigenValues.At(0).Real;
        outData.k2[v] = (float)eigenValues.At(1).Real;
        
        if (outData.k1[v] < outData.k2[v]) return;

        swap(ref outData.k1[v], ref outData.k2[v]);
        swap(ref outData.d1[v], ref outData.d2[v]);
    }

    static void swap<T>(ref T a, ref T b){
        T temp = a;
        a = b;
        b = temp;
    }

    static Vector3 ParametricTo3D(Vector3 Fu, Vector3 Fv, float u, float v) {
        return (v * Fu.normalized + u * Fv.normalized).normalized;
    }

    static void ScalePhi(ref float[] phi, float maxAngle){
        // Scales phi such that it sums to
        // Debug.Log(maxAngle);
        float correctionRatio = 2*Mathf.PI/maxAngle;
        for (int i=0; i<phi.Length; i++) phi[i] = phi[i]*correctionRatio;
    }
}

