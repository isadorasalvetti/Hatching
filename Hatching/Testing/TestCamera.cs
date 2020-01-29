using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestCamera : MonoBehaviour
{
    public Material blitzMaterial;
    
    [ExecuteInEditMode]
    private void OnRenderImage(RenderTexture src, RenderTexture dest){
        Graphics.Blit(src, dest, blitzMaterial);
    }
}
