using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static ProcessHatching;

public class HatchingCamera : MonoBehaviour
{
    public bool getAnImage = false;
    public float dSeparation = 0.01f;
    public float dTest = 0.5f;
    
    private void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (getAnImage)
        {
            getAnImage = false;
            Texture2D texture = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            RenderTexture.active = src;
            texture.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            texture.Apply();
            ProcessHatching hatching = new ProcessHatching(texture, dSeparation: dSeparation, dTest: dTest);
        }
        Graphics.Blit(src, dst);
    }
}
