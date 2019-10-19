using UnityEngine;
using UnityEditor;

public class CreateTextureArray : ScriptableWizard
{
    public Texture2D [] textures;

    [MenuItem("Assets/Create/Texture Array")]
    static void CreateWizard () {
		ScriptableWizard.DisplayWizard<CreateTextureArray>(
			"Create Texture Array", "Create"
		);
	}

    public void OnWizardCreate()
    {
        if (textures.Length == 0) {
			return;
		}
        string path = EditorUtility.SaveFilePanelInProject(
			"Save Texture Array", "TextureArray", "asset", "Save Texture Array"
		);
		if (path.Length == 0) {
			return;
		}
        Texture2DArray textureArray = new Texture2DArray(textures[0].width, textures[0].height, textures.Length,
        TextureFormat.R16, false, false);

        textureArray.filterMode = FilterMode.Bilinear;
        textureArray.wrapMode = TextureWrapMode.Repeat;

        for (int i=0; i<textures.Length; i++){
            textureArray.SetPixels(textures[i].GetPixels(0), i, 0);
        }

        textureArray.Apply();

        AssetDatabase.CreateAsset(textureArray, path);

        // Print the path of the created asset
        Debug.Log(AssetDatabase.GetAssetPath(textureArray));
    }
}