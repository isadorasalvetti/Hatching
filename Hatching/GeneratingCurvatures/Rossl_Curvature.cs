using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.LinearAlgebra.Single;
using UnityEngine;
using Complex = System.Numerics.Complex;

namespace Hatching.GeneratingCurvatures{
    public class RosslCurvature {

        public RosslCurvature(Mesh mesh){
            _mesh = mesh;
            _vertexNeighboors = new List<List<int>>();
        }

        private List<List<int>> _vertexNeighboors;
        private int[] _cornerTable;
        private Mesh _mesh;

        float [] k1; //minor
        float [] k2; //major
        Vector<float> [] d1;
        Vector<float> [] d2;
        Vector3 [] principalDirections;

        static int Next(int corner) {
            return 3 * (corner / 3) + (corner + 1) % 3;
        }

        static int Previous(int corner) {
            return 3 * (corner / 3) + (corner + 2) % 3;
        }

        public List<List<int>> GetVertexNeighboors() {
            if(_vertexNeighboors != null) return _vertexNeighboors;
            Debug.Log("Curvature information has not been computed"); return new List<List<int>>();
        }

        public Vector3[] GetPrincipalDirections() {
            if(principalDirections != null) return principalDirections;
            Debug.Log("Curvature information has not been computed"); return new Vector3[0];
        }

        public float[] GetCurvatureRatio() {
            if(k1 != null){
                float[] ratios = new float [_mesh.vertexCount];
                for(int i = 0; i < ratios.Length; i++ ) ratios[i] = k2[i] / k1[i];
                return ratios;
            };
            Debug.Log("Curvature information has not been computed"); return new float[0];
        }

        public void ComputeCurvature(){
            int n = _mesh.vertexCount;
            k1 = new float[n];
            k2 = new float[n];
            principalDirections = new Vector3[n];
            d1 = new Vector<float>[n];
            d2 = new Vector<float>[n];

            BuildCornerTable();
            
            for (int i = 0; i < _mesh.vertices.Length; i++){
                List<int> neighboors = GetOrderedNeighboors(i);
                _vertexNeighboors.Add(neighboors);
                float[] r, phi;
                Matrix<float> F;
                MakeExponentialMap(i, neighboors, out r, out phi);
                GetUVF(r, phi, neighboors.ToArray(), i, out F);
                GetCurvatures(F, out k1[i], out k2[i], out d1[i], out d2[i]);
                principalDirections[i] = ParametricTo3D(vectorToUnity(F.Row(0)), vectorToUnity(F.Row(1)), d1[i][0], d1[i][1]);
                //Debug.Log(i.ToString() + ": " + curvatures[i]);
            }
        }

        Vector3 vectorToUnity(Vector<float> v){
            return new Vector3(v[0], v[1], v[2]);
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

        void GetParametricSpaceBasis(int vert, int ng0, out Vector3 e1, out Vector3 e2, out Vector3 e3){
            e1 = _mesh.vertices[_mesh.triangles[ng0]] - _mesh.vertices[vert];
            e3 = _mesh.normals[vert];
            e2 = Vector3.Cross(e3, e1);
            }

        void GetUVF(float[] r, float[] phi, int[] neighbors, int v, out Matrix<float> F){
            float[,] V = new float[r.Length, 5];
            float[,] Q = new float[r.Length, 3];

            for (int i = 0; i < r.Length; i++){
                Vector3 vert = _mesh.vertices[_mesh.triangles[neighbors[i]]] - _mesh.vertices[v];
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

        void GetCurvatures(Matrix<float> F, out float k1, out float k2, out Vector<float> d1, out Vector<float> d2) { // F: Fu, Fv, Fuu, Fuv, Fvv
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
            
            d1 = eigenVectors.Row(0);
            d2 = eigenVectors.Row(1);
            k1 = (float)eigenValues.At(0).Real;
            k2 = (float)eigenValues.At(1).Real;
            
            if (Mathf.Abs(k1) < Mathf.Abs(k2)) return;

            swap(ref k1, ref k2);
            swap(ref d1, ref d2);

        }

        void swap<T>(ref T a, ref T b){
            T temp = a;
            a = b;
            b = temp;
        }

        Vector3 ChangeBasis(Vector3 e1, Vector3 e2, Vector3 e3, Vector<float> v){
            Matrix<float> mat = DenseMatrix.OfArray(new float[3, 3] {{e1.x, e2.x, e3.x}, 
                                                                     {e1.y, e2.y, e3.y},
                                                                     {e1.y, e2.y, e3.y}});            
            v = mat*DenseVector.OfEnumerable(v.Concat(new float[1]{0}));
            return new Vector3(v.At(0), v.At(1), v.At(2)).normalized;
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

        Vector3 ParametricTo3D(Vector3 Fu, Vector3 Fv, float u, float v) {
            return (v * Fu.normalized + u * Fv.normalized).normalized;
        }

        void ScalePhi(ref float[] phi, float maxAngle){
            // Scales phi such that it sums to
            // Debug.Log(maxAngle);
            float correctionRatio = 2*Mathf.PI/maxAngle;
            for (int i=0; i<phi.Length; i++) phi[i] = phi[i]*correctionRatio;
        }
    }
}
