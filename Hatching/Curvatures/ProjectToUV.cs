using System;
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
                Vector3 pointPCurvature = verts[j] + pd*0.2f;
                Vector3 projectedPointP = ProjectPointPlane(pointPCurvature, trianglePlane);

                float[] triangleAreas = GetTriangleAreas(verts[0], verts[1], verts[2], projectedPointP);
                Vector2 pointPuv = BaricentricInterpolation(triangleAreas, uvs);
                Vector2 uvPD = uvs[j] - pointPuv;
                
                uvPDs[vertIndices[j]] = uvPD.normalized;
                Debug.Log(String.Format("Direction: {0}, Areas: {1}, {2}, {3}, Point: {4}, Direction: {5}, UV: {6}, pUV: {7}", 
                    uvPD, triangleAreas[0], triangleAreas[1], triangleAreas[2], verts[j], pd, uvs[j], pointPuv));
            }
        }
        Debug.Log("Projected curvatures to uv");
        return uvPDs;
    }

    private static Vector2 BaricentricInterpolation(float[] areas, Vector2[] pointValues) {
        Vector2 outInterpolated = Vector2.zero; 
        float totalArea = 0; foreach (float area in areas) totalArea += area;
        for (int i = 0; i < pointValues.Length; i++) {
            Vector2 areaContribution = pointValues[i] * areas[i] / totalArea;
            outInterpolated += areaContribution;
        }
        return outInterpolated;
    }


    private static float[] GetTriangleAreas(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 center) {
        float[] weights = new float[3];
       
        Vector3 v0 = p2 - p1, v1= p3 - p1, v2 = center - p1;
        float d00 = Vector3.Dot(v0, v0);
        float d01 = Vector3.Dot(v0, v1);
        float d11 = Vector3.Dot(v1, v1);
        float d20 = Vector3.Dot(v2, v0);
        float d21 = Vector3.Dot(v2, v1);
        float invDenom = 1.0f / (d00 * d11 - d01 * d01);
        weights[0] = (d11 * d20 - d01 * d21) * invDenom;
        weights[1] = (d00 * d21 - d01 * d20) * invDenom;
        weights[2] = 1.0f - weights[0] - weights[1];

        return weights;
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
