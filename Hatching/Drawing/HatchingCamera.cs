using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public Material normalsNDepth;
    public Material imageSpaceOutline;
    public Material principalDirections;

    private int diff = 0;
    
    private void Start()
    {
        myCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        if (getAnImage) {
            diff = 0;
            getAnImage = false;
            for (int i = 0; i < 1; i++) {
                MakeHatching();
                //foreach (var mesh in objectsVisible) {
                    //mesh.transform.Rotate(Vector3.left, 10);
                //}
                diff++;
            }
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

    private void changeMaterials(Material newMaterial){
        foreach (var visibleObject in objectsVisible) {
            var meshRenderer = visibleObject.GetComponent<MeshRenderer>();
            var currentMaterials = meshRenderer.materials;
            Material[] newMaterials = Enumerable.Repeat(newMaterial, currentMaterials.Length).ToArray();
            meshRenderer.materials = newMaterials;
        }
    }

    public void MakeHatching()
    {
        RenderTexture.active = myCamera.targetTexture;

        // Outline
        changeMaterials(normalsNDepth);
        var cameraTexture = myCamera.targetTexture;
        Texture2D texture = RenderCamera();
        Graphics.Blit(texture, cameraTexture, imageSpaceOutline);
        cameraTexture.ResolveAntiAliasedSurface();
        texture.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        texture.Apply();
        Image bitmap = Image.Load<Rgba32>(texture.EncodeToPNG());
        
        // Hatching
        changeMaterials(principalDirections);
        texture = RenderCamera();
        //Image bitmap = Image.Load<Rgba32>(texture.EncodeToPNG());
        //Image bitmap = new Image<Rgba32>(texture.width, texture.height);
        
        ProcessHatching hatching = new ProcessHatching(texture, dSeparation: dSeparation, dTest: dTest, level: 0.8f);
                
        Debug.Log(string.Format("Started drawing lines. dSeparation: {0}, dTest: {1}%", dSeparation, dTest));
        hatching.StartRandomSeed();
        hatching.DrawHatchings(bitmap);

        foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors
        
        texture = RenderCamera();
        hatching = new ProcessHatching(texture, dSeparation: dSeparation, dTest: dTest, level: 0.3f);
        Debug.Log("Drawing parallel lines.");
        hatching.StartRandomSeed();
        hatching.DrawHatchings(bitmap);
        
        bitmap.Save("C:\\Users\\isadora.albrecht\\Documents\\Downloads\\test" + diff.ToString() + ".png", new PngEncoder());
        //bitmap.Save("C:\\Users\\Isadora\\Documents\\_MyWork\\Papers\\Thesis\\test.png", new PngEncoder());
        
        foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors
    }
}
