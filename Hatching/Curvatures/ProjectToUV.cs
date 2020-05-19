using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectToUV
{
    // Start is called before the first frame update
    public static Vector2[] GetUVCurvatures(MeshInfo meshInfo) {
        Vector2[] uvPDs = new Vector2[meshInfo.vertexCount];
        
        for (int i = 0; i < meshInfo.mesh.triangles.Length / 3; i++) {
            Vector4 trianglePlane = GetTrianglePlane(i, meshInfo.mesh.triangles, meshInfo.mesh.vertices);
            int[] tris = meshInfo.mesh.triangles;
            
            int[] vertIndices = new int[] { tris[i*3], tris[i*3+1], tris[i*3+2]};
            
            Vector3[] verts = new Vector3[] {
                meshInfo.mesh.vertices[vertIndices[0]],
                meshInfo.mesh.vertices[vertIndices[1]],
                meshInfo.mesh.vertices[vertIndices[2]]
            };
            
            Vector2[] uvs =  new Vector2[] {
                meshInfo.mesh.uv[vertIndices[0]],
                meshInfo.mesh.uv[vertIndices[1]],
                meshInfo.mesh.uv[vertIndices[2]]
            };
            
            for (int j = 0; j < 3; j++) {
                Vector3 pd = meshInfo.principalDirections[i*3 + j];
                Vector3 pointPCurvature = verts[j] + pd;
                Vector3 projectedPointP = ProjectPointPlane(pointPCurvature, trianglePlane);

                float[] triangleAreas = GetTriangleAreas(verts[0], verts[1], verts[2], projectedPointP);
                Vector2 pointPuv = BaricentricInterpolation(triangleAreas, uvs);
                Vector2 uvPD = uvs[j] - pointPuv;
                uvPDs[vertIndices[j]] = uvPD;
            }
        }
        Debug.Log("Projected curvatures to uv");
        return uvPDs;
    }

    private static Vector2 BaricentricInterpolation(float[] areas, Vector2[] pointValues) {
        Vector2 outInterpolated = Vector2.zero; 
        float totalArea = 0; foreach (float area in areas) totalArea += area;
        for (int i = 0; i < pointValues.Length; i++) {
            outInterpolated += pointValues[i] * areas[i] / totalArea;
        }
        return outInterpolated;
    }


    private static float[] GetTriangleAreas(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 center) {
        float[] areas = new float[3];
        
        areas[0] = Vector3.Cross(p2 - p1, center - p1).magnitude / 2;
        areas[1] = Vector3.Cross(p3 - p2, center - p2).magnitude / 2;
        areas[2] = Vector3.Cross(p2 - p3, center - p3).magnitude / 2;

        return areas;
    }

    int GetPointTriangleIndex(int vertex, int[] triangles){
        for(int i = 0; i < triangles.Length; i++) {
            if (triangles[i] == vertex)
                return i;
        }
        return -1;
    }

    private static Vector4 GetTrianglePlane(int triangle, int[] triangles, Vector3[] vertices) {
        Vector3 p1 = vertices[triangles[triangle * 3]];
        Vector3 p2 = vertices[triangles[triangle * 3 + 1]];
        Vector3 p3 = vertices[triangles[triangle * 3 + 2]];
        
        Vector3 v1 = p1 - p2; Vector3 v2 = p2 - p3;
        
        Vector3 planeNormal = Vector3.Cross(v1, v2).normalized;
        float d = planeNormal.x * p1.x + planeNormal.y * p1.y + planeNormal.z * p1.z;

        return new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, d);
    }

    private static Vector3 ProjectPointPlane(Vector3 point, Vector4 plane) {
        float d = plane.w;
        Vector3 normal = new Vector3(point.x, point.y, point.z);
        Vector3 projPoint = point - (d / Vector3.Dot(point, normal))*normal;
        return projPoint;
    }
    
}
