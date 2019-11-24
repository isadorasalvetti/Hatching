using System;
using System.Collections;
using System.Collections.Generic;
using Hatching.HatchingShader.GenerateInImageSpace;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class HatchingCamera : MonoBehaviour
{
    public bool getAnImage = false;
    public float dSeparation = 0.001f;
    
    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (getAnImage)
        {
            getAnImage = false;
            Texture2D texture = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            RenderTexture.active = src;
            texture.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            texture.Apply();
            ProcessHatching hatching = new ProcessHatching(texture, dSeparation: dSeparation);
        }
        Graphics.Blit(src, dst);
    }
}
