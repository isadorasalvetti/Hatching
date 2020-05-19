using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class GetCurvatures : MonoBehaviour {
    public string storedName;
    public string loadingName;
    
    private MeshFilter[] _mesheFilters;

    //Smooth Mesh
    private List<List<int>>[] _mapFromNew;

    //Principal Directions
    private CurvatureData[] _curvatureDatas;
    private MeshInfo[] _meshInfosDuplicated;
    private MeshInfo[] _meshInfosMerged;
    private int rotationCount = 0;

    private bool Initialize(){
        bool returnFalse = false;
        _mesheFilters = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in _mesheFilters)
            CurvatureFilter.DuplicateMeshVertices(meshFilter.sharedMesh);
        if (_meshInfosMerged == null) {
            Mesh[] smoothMesh;
            GetAllSmoothMeshes(out smoothMesh, out _mapFromNew);
            _meshInfosMerged = new MeshInfo[smoothMesh.Length];
            _meshInfosDuplicated = new MeshInfo[smoothMesh.Length];
            _curvatureDatas = new CurvatureData[smoothMesh.Length];
            for (int i = 0; i < _mesheFilters.Length; i++) {
                _meshInfosMerged[i] = new MeshInfo(smoothMesh[i]);
                _curvatureDatas[i] = new CurvatureData(smoothMesh[i].vertexCount);
            }
            returnFalse = true;
        }

        if(returnFalse) return false;
        return true;
    }

    private void GetAllSmoothMeshes(out Mesh[] allSmoothMeshes, out List<List<int>>[] allMapsFromNew){
        //Makes a copy of the mesh with all normals smoothed. 
        allSmoothMeshes = new Mesh[_mesheFilters.Length];
        allMapsFromNew = new List<List<int>>[_mesheFilters.Length];
        for (int m = 0; m < _mesheFilters.Length; m++){
            Mesh mesh = _mesheFilters[m].sharedMesh;
            allSmoothMeshes[m]= GetSmoothMesh(mesh, out allMapsFromNew[m]);
        }
    }

    public void ComputeCurvatureRossl(){
        Debug.Log("--- Computing curvatures");
        Initialize();
        for (int m = 0; m < _mesheFilters.Length; m++){
            RosslCurvature.ComputeCurvature(ref _meshInfosMerged[m], out _curvatureDatas[m]);
        }
        ApplyPrincipalDirectios();
    }

    public void OptimizePrincipalDirections(float reliabilityRatio)
    {
        if (!Initialize() || _meshInfosMerged[0].principalDirections == null) {
            Debug.Log("Curvatures not computed");
            return;
        }
        
        for (int m=0; m < _meshInfosMerged.Length; m++) {
            MeshInfo meshInfo = _meshInfosMerged[m];
            if (meshInfo.principalDirections.Length < 1) {
                Debug.Log("Principal directions not computed");
                return;
            }
            
            bool[] curvatureReliability = CurvatureFilter.GetReliability(meshInfo.curvatureRatios, reliabilityRatio);
            //Debug.Log(CurvatureFilter.showArray(meshInfo.principalDirections));
            meshInfo.principalDirections = CurvatureFilter.MinimizeEnergy(meshInfo, curvatureReliability);
            //Debug.Log(CurvatureFilter.showArray(meshInfo.principalDirections));
        }
        ApplyPrincipalDirectios();
    }
    

    public void TestCurvatureOptimization(float reliabilityRatio){
        if (!Initialize() || _meshInfosMerged[0].principalDirections == null) {
            Debug.Log("Curvatures not computed");
            return;
        }
        
        for (int m=0; m < _meshInfosMerged.Length; m++) {
            MeshInfo meshInfo = _meshInfosMerged[m];
            if (meshInfo.principalDirections.Length < 1) {
                Debug.Log("Principal directions not computed");
                return;
            }
            bool[] curvatureRatio = CurvatureFilter.GetReliability(meshInfo.curvatureRatios, reliabilityRatio);
            CurvatureFilter.TestEnergyResults(meshInfo, curvatureRatio);
        }
    }

    public void ApplyPrincipalDirectios(bool align=true){
        for (int m = 0; m < _mesheFilters.Length; m++){
            Mesh mesh = _mesheFilters[m].sharedMesh;
            Vector3[] newVectors = new Vector3[mesh.vertices.Length];
            Vector3[,] allNewVectors = new Vector3[mesh.vertices.Length, 4];
            Vector3[] smoothedNormals = new Vector3[mesh.normals.Length];

            int displacement = 0;
            for (int i = 0; i < _meshInfosMerged[m].principalDirections.Length; i++){
                for (int j = 0; j < _mapFromNew[m][i].Count; j++) {
                    displacement += j;
                    newVectors[_mapFromNew[m][i][j]] = _meshInfosMerged[m].principalDirections[i];
                    for(int k=0; k < 4; k++) allNewVectors[_mapFromNew[m][i][j], k] = _meshInfosMerged[m].AllPrincipalDirections[i, k];
                    smoothedNormals[_mapFromNew[m][i][j]] = _meshInfosMerged[m].approximatedNormals[i];
                }
            }
            
            mesh.normals = smoothedNormals;
            MeshInfo newMeshInfo = new MeshInfo(mesh);
            newMeshInfo.principalDirections = newVectors;
            newMeshInfo.AllPrincipalDirections = allNewVectors;
            newMeshInfo.approximatedNormals = smoothedNormals;
            
            if (align) CurvatureFilter.AlignDirections(newMeshInfo, false);
            StoreMeshInfoInFile(newMeshInfo, storedName);
            _meshInfosDuplicated[m] = newMeshInfo;
            newVectors = newMeshInfo.GetaDirection(0);
            mesh.colors = Array.ConvertAll(newVectors, j => new Color(j.x, j.y, j.z, 1));
        }
        Debug.Log("Applied principal directions as colors");
    }
    
    private Mesh GetSmoothMesh(Mesh mesh, out List<List<int>> mapToOld) {
        Mesh smoothMesh = new Mesh();
        int[] vertexTransformation = new int[mesh.vertices.Length];
        List<Vector3> uniqueVertexPositions = new List<Vector3>();
        mapToOld = new List<List<int>>();
    
        int removedVertives = 0;
        for (int i = 0; i < mesh.vertices.Length; i++){
            int firstIndex = uniqueVertexPositions.IndexOf(mesh.vertices[i]);
            if (firstIndex == -1){
                uniqueVertexPositions.Add(mesh.vertices[i]);
                vertexTransformation[i] = i - removedVertives;
                mapToOld.Add(new List<int>());
                mapToOld[i - removedVertives].Add(i);
            }
            else{
                vertexTransformation[i] = firstIndex;
                mapToOld[firstIndex].Add(i);
                removedVertives += 1;
            }
        }
        smoothMesh.vertices = uniqueVertexPositions.ToArray();
        int[] triangles = new int[mesh.triangles.Length];
        for (int i = 0; i < mesh.triangles.Length; i++){
            triangles[i] = vertexTransformation[mesh.triangles[i]];
        }

        smoothMesh.triangles = triangles;
        smoothMesh.RecalculateNormals(180);
        return smoothMesh;
    }

    public void ShowNormals(){
        foreach(MeshFilter meshFilter in _mesheFilters){
             Color[] colors = new Color[meshFilter.sharedMesh.colors.Length];
             for(int i = 0; i < meshFilter.sharedMesh.colors.Length; i++){
                 Vector3 n = meshFilter.sharedMesh.normals[i];
                 colors[i] = new Color(n[0], n[1], n[2], 1);
             }
             meshFilter.sharedMesh.colors = colors;
        }
    }

    public void ShowRatios() {
        if (!Initialize()) {
            Debug.Log("Curvatures not computed");
            return;
        }

        foreach (MeshInfo meshInfo in _meshInfosMerged) {
            Color[] colors = new Color[meshInfo.mesh.colors.Length];
            for (int i = 0; i < meshInfo.principalDirections.Length; i++) {
                meshInfo.principalDirections[i] = new Vector3(1-meshInfo.curvatureRatios[i], 0, 0);
            }
            ApplyPrincipalDirectios(align:false);
        }
    }

    public void RotatePrincipalDirections(float angle) {
        // Applies to global mesh and affects all game objects
        if (!Initialize()) {Debug.Log("Curvatures not computed"); return;}
            for (int m = 0; m < _meshInfosMerged.Length; m++) {
                CurvatureFilter.RotateAllDirections(ref _meshInfosMerged[m].principalDirections, _meshInfosMerged[m].approximatedNormals, angle);
                Debug.Log(CurvatureFilter.showArray(_meshInfosMerged[m].principalDirections));
            }
            ApplyPrincipalDirectios();
    }

    public void RotateVertexColors() {
        //Applies only to mesh in current game object
        var meshes = GetComponentsInChildren<MeshFilter>();
        for(int i=0; i < meshes.Length; i++) {
            var meshFilter = meshes[i];
            rotationCount += 1;
            //Debug.Log(i);
            //Vector3[] rotatedDirections = _meshInfosDuplicated[i].GetaDirection(rotationCount % 4);
            List<Color> colors = new List<Color>(meshFilter.mesh.colors);
            Vector3[] colorsAsVectors = colors.ConvertAll(j => new Vector3(j.r, j.g, j.b)).ToArray();
            CurvatureFilter.RotateAllDirections(ref colorsAsVectors, meshFilter.mesh.normals, 90);
            meshFilter.mesh.SetColors(new List<Vector3>(colorsAsVectors).ConvertAll(j => new Color(j.x, j.y, j.z)));
        }
    }

    public void ApplyUVDirections() {
        for (int i = 0; i < _meshInfosDuplicated.Length; i++) {
            _meshInfosDuplicated[i].uvCurvatures = ProjectToUV.GetUVCurvatures(_meshInfosDuplicated[i]);
            _meshInfosDuplicated[i].uvsProjected = true;
        }
    }

    public void StoreMeshInfoInFile(MeshInfo meshInfo, string meshName) {
        StoredCurvature curvatures = new StoredCurvature(meshInfo);
        XmlSerializer xs = new XmlSerializer(typeof(StoredCurvature));
        string path = Application.dataPath + "/Hatching/CurvatureData/" + meshName + ".xml";
        TextWriter tw = new StreamWriter(path);
        xs.Serialize(tw, curvatures);
        
        Debug.Log(String.Format("Saved curvatures to {0}", path));
    }

    public void ReadCurvatureFromXML() {
        Debug.Log("Loading name = " + loadingName);
        XmlSerializer serializer = new XmlSerializer(typeof(StoredCurvature));
        string path = Application.dataPath + "/Hatching/CurvatureData/" + loadingName + ".xml";
        StoredCurvature curvature;
        using (Stream reader = new FileStream(path, FileMode.Open)){
            curvature = (StoredCurvature)serializer.Deserialize(reader);          
        }
        // TODO: make it work with multi-mesh objects;
        Vector3 sampleOldVector = _meshInfosDuplicated[0].AllPrincipalDirections[0, 2]; //Debug
        _meshInfosDuplicated[0] = rebuildMeshInfo(_meshInfosDuplicated[0], curvature);
        Vector3 [] newVectors = _meshInfosDuplicated[0].GetaDirection(0);
        _mesheFilters[0].sharedMesh.colors = Array.ConvertAll(newVectors, j => new Color(j.x, j.y, j.z, 1));
        Debug.Log("Changed colors");
    }

    private MeshInfo rebuildMeshInfo(MeshInfo baseInfo, StoredCurvature newCurvatureInfo) {
        Debug.Log(CurvatureFilter.showArray(baseInfo.GetaDirection(0)));
        Debug.Log(CurvatureFilter.showArray(newCurvatureInfo.principalDirections0));
        if (baseInfo.vertexCount == newCurvatureInfo.vertexPositions.Length) {
            Debug.Log(baseInfo.vertexCount.ToString() + ", " +
                      newCurvatureInfo.principalDirections0.Length.ToString());
            for (int i = 0; i < baseInfo.vertexCount; i++) {
                baseInfo.AllPrincipalDirections[i,0] = newCurvatureInfo.principalDirections0[i];
                baseInfo.AllPrincipalDirections[i,1] = newCurvatureInfo.principalDirections1[i];
                baseInfo.AllPrincipalDirections[i,2] = newCurvatureInfo.principalDirections2[i];
                baseInfo.AllPrincipalDirections[i,3] = newCurvatureInfo.principalDirections3[i];
            }
        }
        else {
            Debug.Log("Vertex count did not match, could not load data. " + 
                      baseInfo.vertexCount.ToString() + " x " + newCurvatureInfo.vertexPositions.Length.ToString());
        }

        return baseInfo;
    }
}
