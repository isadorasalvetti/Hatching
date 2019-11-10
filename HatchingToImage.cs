using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HatchingToImage : MonoBehaviour {
    void CreatePlane() {
        Vector3[] vertices = new Vector3[4]{
            new Vector3(0, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0)
            };
        int[] triangles = new int[6] {
            1, 2, 3,
            2, 4, 3
        };
        Mesh plane = new Mesh();
        plane.vertices = vertices;
        plane.triangles = triangles;
    }
}
