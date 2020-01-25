using System;
using System.Collections;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static ProcessHatching;

public class HatchingCamera : MonoBehaviour
{
    public bool getAnImage = false;
    public float dSeparation = 0.01f;
    public float dTest = 0.5f;

    public GameObject[] objectsVisible; //TODO: make this automatic and check if they are visible

    private Camera myCamera;
    

    private void Start()
    {
        myCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (getAnImage)
        {
            getAnImage = false;
            MakeHatching();
        }
    }

    public Texture2D RenderCamera()
    {
        myCamera.Render();
        var cameraTexture = myCamera.targetTexture;
        RenderTexture.active = cameraTexture;
        Texture2D texture = new Texture2D(cameraTexture.width, cameraTexture.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, cameraTexture.width, cameraTexture.height), 0, 0);
        texture.Apply();
        
        return texture;
    }

    public void MakeHatching()
    {
        RenderTexture.active = myCamera.targetTexture;

        Texture2D texture = RenderCamera();
        Image bitmap = Image.Load<Rgba32>(texture.EncodeToPNG());
        
        ProcessHatching hatching = new ProcessHatching(texture, dSeparation: dSeparation, dTest: dTest);
                
        Debug.Log(string.Format("Started drawing lines. dSeparation: {0}, dTest: {1}%", dSeparation, dTest));
        hatching.StartRandomSeed();
        hatching.DrawHatchings(bitmap);

        foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors
        
        texture = RenderCamera();
        hatching = new ProcessHatching(texture, dSeparation: dSeparation, dTest: dTest);
        Debug.Log("Drawing parallel lines.");
        hatching.StartRandomSeed();
        hatching.DrawHatchings(bitmap);
        
        bitmap.Save("C:\\Users\\isadora.albrecht\\Documents\\Downloads\\test.png", new PngEncoder());
        //bitmap.Save("C:\\Users\\Isadora\\Documents\\_MyWork\\Papers\\Thesis\\test.png", new PngEncoder());
    }
}
