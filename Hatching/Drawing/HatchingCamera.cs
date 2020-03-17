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
        Image bitmap;

        // Outline
        changeMaterials(normalsNDepth);
        var cameraTexture = myCamera.targetTexture;
        Texture2D textureOutline = RenderCamera();

        bitmap = Image.Load<Rgba32>(textureOutline.EncodeToPNG());
        bitmap.Save(HatchingSettings.saveHatchingPath + "NormalsAndDepth" + diff.ToString() + ".png", new PngEncoder());
        
        Graphics.Blit(textureOutline, cameraTexture, imageSpaceOutline);
        
        textureOutline.ReadPixels(new Rect(0, 0, textureOutline.width, textureOutline.height), 0, 0);
        textureOutline.Apply();
        
        Image lineBitmap = Image.Load<Rgba32>(textureOutline.EncodeToPNG());
        //bitmap.Save(HatchingSettings.saveHatchingPath + "Outline" + diff.ToString() + ".png", new PngEncoder());
        
        
        // Hatching
        changeMaterials(principalDirections);
        Texture2D[] texture = new Texture2D[4];
            
        texture[0] = RenderCamera();
        foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors 90 degrees
        texture[1] = RenderCamera();
        foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors 90 degrees
        texture[2] = RenderCamera();
        foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors 90 degrees
        texture[3] = RenderCamera();

        bitmap = lineBitmap;
        
        ProcessHatching hatching = new ProcessHatching(texture, dSeparation: dSeparation, dTest: dTest, level:1.0f);
                
        Debug.Log(string.Format("Started drawing lines. dSeparation: {0}, dTest: {1}%", dSeparation, dTest));
        hatching.StartRandomSeed();
        hatching.DrawHatchings(bitmap);

        //foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors
        
        //texture = RenderCamera();
        //hatching = new ProcessHatching(texture, textureAlt, dSeparation: dSeparation, dTest: dTest, level: 0.3f);
        //Debug.Log("Drawing parallel lines.");
        //hatching.StartRandomSeed();
        //hatching.DrawHatchings(bitmap);
        
        Debug.Log(HatchingSettings.saveHatchingPath + "test" + diff.ToString() + ".png");
        bitmap.Save(HatchingSettings.saveHatchingPath + "test" + diff.ToString() + ".png", new PngEncoder());
        
        foreach (var obj in objectsVisible) obj.GetComponent<GetCurvatures>().RotateVertexColors(); //Rotate all principal directions/ colors
    }
}
