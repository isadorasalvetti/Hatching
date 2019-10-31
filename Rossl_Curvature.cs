using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra.Single;
using Complex = System.Numerics.Complex;
using Vector = MathNet.Numerics.LinearAlgebra.Double.Vector;

namespace Rossl{
    public class RosslCurvature {
        private int[] _cornerTable;
        private Mesh _mesh;

        static int Next(int corner) {
            return 3 * (corner / 3) + (corner + 1) % 3;
        }

        static int Previous(int corner) {
            return 3 * (corner / 3) + (corner + 2) % 3;
        }

        public Tuple<Vector3, float>[] ComputeCurvature(Mesh mesh){
            _mesh = mesh;
            Tuple<Vector3, float>[] curvatures = new Tuple<Vector3, float>[_mesh.vertices.Length];
            BuildCornerTable();
            
            for (int i = 0; i < _mesh.vertices.Length; i++){
                List<int> neighbors = GetOrderedNeighboors(i);
                float[] r, phi;
                Matrix<float> F;
                MakeExponentialMap(i, neighbors, out r, out phi);
                GetUVF(r, phi, neighbors.ToArray(), out F);
                curvatures[i] = GetMajorCurvature(F, i);
                Debug.Log(i.ToString() + ": " + curvatures[i]);
            }
            return curvatures;
        }

        void BuildCornerTable(){
            List<int[]> cornerEdges = new List<int[]>(new int[_mesh.triangles.Length][]);
            _cornerTable = new int[_mesh.triangles.Length];

            //Debug.Log("Number of vertices = " + _mesh.vertices.Length.ToString() + ", Number of faces = " + _mesh.triangles.Length.ToString());

            for (int i = 0; i < _mesh.triangles.Length/3; i++){
                int[] vector = new int[3] {_mesh.triangles[i*3+0], _mesh.triangles[i*3+1], _mesh.triangles[i*3+2]};
                int[] edge0 = new int[2] {Mathf.Min(vector[1], vector[2]), Mathf.Max(vector[1], vector[2])}; // 0
                int[] edge1 = new int[2] {Mathf.Min(vector[0], vector[2]), Mathf.Max(vector[0], vector[2])}; // 1
                int[] edge2 = new int[2] {Mathf.Min(vector[0], vector[1]), Mathf.Max(vector[0], vector[1])}; // 2

                cornerEdges[i*3+0] = new int[2] {edge0[0], edge0[1]};
                cornerEdges[i*3+1] = new int[2] {edge1[0], edge1[1]};
                cornerEdges[i*3+2] = new int[2] {edge2[0], edge2[1]};
            }
            
            for (int i = 0; i < cornerEdges.Count; i++){
                int[] e1 = cornerEdges[i];
                _cornerTable[i] = -1;
                for (int j = 0; j < cornerEdges.Count; j++){
                    int[] e2 = cornerEdges[j];
                    if (i!=j && e1[0] == e2[0] && e1[1] == e2[1]){
                        _cornerTable[i] = j;
                        _cornerTable[j] = i;
                        break;
                    }
                }
            }
            //Debug.Log("Corner Table = " + string.Join(", ", new List<int>(_cornerTable).ConvertAll(j => j.ToString()).ToArray()));
        }

        List<int> GetOrderedNeighboors(int vert){
            List<int> vertexNeighboors = new List<int>();
            List<int> temp = new List<int>();

            int firstCorner = Next(_mesh.triangles.ToList().IndexOf(vert));
            vertexNeighboors.Add(firstCorner);
            int nextCorner = firstCorner;
            for (int i = 0; i < _mesh.triangles.Length/3; i++)
            {
                nextCorner = _cornerTable[Next(nextCorner)];
                if (nextCorner == firstCorner) break;
                if (nextCorner > -1) vertexNeighboors.Add(nextCorner);
                else{ //needs fix, remove vertex
                    break;
                }
            }
            return vertexNeighboors;
        }

        void MakeExponentialMap(int vert, List<int> neighboors, out float[] r, out float[] phi){
            // Move vertices such that v = (0, 0, 0)
            Vector3 v0 = _mesh.vertices[vert];
            Vector3[] vn = new Vector3[neighboors.Count];
            r = new float[neighboors.Count];
            phi = new float[neighboors.Count];

            // First neighboor - begining of the polar coordinates
            vn[0] = _mesh.vertices[_mesh.triangles[neighboors[0]]] - v0;
            r[0] = vn[0].magnitude;
            phi[0] = 0;

            float accumulatedAngle = 0;
            for (int i = 1; i < neighboors.Count; i++){
                vn[i] = _mesh.vertices[_mesh.triangles[neighboors[i]]] - v0;
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

        void GetUVF(float[] r, float[] phi, int[] neighbors, out Matrix<float> F){
            float[,] V = new float[r.Length, 5];
            float[,] Q = new float[r.Length, 3];

            for (int i = 0; i < r.Length; i++){
                Vector3 vert = _mesh.vertices[_mesh.triangles[neighbors[i]]];
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

        Matrix<float> GetF(Matrix<float> VM, Matrix<float> QM, int n){
            Matrix<float> F = DenseMatrix.OfArray(new float[1,n]);
            if(n==5) F = VM.Inverse()*QM;
            if(n<5) F = VM.Transpose()*(VM*VM.Transpose()).Inverse()*QM;
            if(n>5) F = (VM.Transpose()*VM).Inverse()*VM.Transpose()*QM;
            return F;
        }

        Tuple<Vector3, float> GetMajorCurvature(Matrix<float> F, int i) { // F: Fu, Fv, Fuu, Fuv, Fvv
            //float lambda1, lambda2, k1, k2;
            
            //Vector3 Fu = new Vector3(F[0, 0], F[0, 1], F[0, 2]);
            //Vector3 Fv = new Vector3(F[1, 0], F[1, 1], F[1, 2]);
            //Vector3 Nu = Vector3.Cross(Fu, Fv).normalized;
            Vector3 Nu = _mesh.normals[i];
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

            //SolveForLambda(n, m, l, out lambda1, out lambda2);
            //SolveForK(n, m, l, lambda1, out k1);
            //SolveForK(n, m, l, lambda2, out k2);

            Vector<float> lambda1 = eigenVectors.Row(0);
            Vector<float> lambda2 = eigenVectors.Row(1);
            float k1 = (float)eigenValues.At(0).Real;
            float k2 = (float)eigenValues.At(1).Real;
            
            if (k1 > k2) return new Tuple<Vector3, float> (new Vector3(lambda1.At(0), lambda1.At(1), 0), k2 / k1);
            return new Tuple<Vector3, float>(new Vector3(lambda2.At(0), lambda2.At(1), 0), k1 / k2);
        }

        void SolveForLambda(float n, float m, float l, out float lambda1, out float lambda2) {
            float root = Mathf.Sqrt(((n - l) * (n - l) + 4 * m));
            lambda1 = ((n - l) + root) / (2 * m);
            lambda2 = ((n - l) - root) / (2 * m);
        }

        void SolveForK(float n, float m, float l, float lambda, out float k) {
            float num = l + 2 * m * lambda + n * lambda * lambda;
            float div = 1 + lambda * lambda;
            k = Mathf.Abs(num / div);
            if (float.IsNaN(k)) k = 1;
        }

        Vector3 ParametricTo3D(Vector3 Fu, Vector3 Fv, float lambda) {
            return (Fu + lambda * Fv).normalized;
        }

        void ScalePhi(ref float[] phi, float maxAngle){
            // Scales phi such that it sums to
            // Debug.Log(maxAngle);
            float correctionRatio = 2*Mathf.PI/maxAngle;
            for (int i=0; i<phi.Length; i++) phi[i] = phi[i]*correctionRatio;
        }
    }
}
