using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using AssetMessageService;

public class TestPostprocessor : AssetPostprocessor
{
	//------------------------------------------------------
	// unity system function
	//------------------------------------------------------

	void OnPostprocessTexture(Texture2D texture)
	{
		EditorApplication.delayCall += () => 
		{
			var fixedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
			if (!fixedTexture.name.StartsWith("tex_"))
			{
				AssetMessenger.Set(fixedTexture, "名前は tex_ から始まる事", MessageType.Error, "MaterialImporter");
			}
			else
			{
				AssetMessenger.Clear(fixedTexture, "MaterialImporter");
			}
		};
	}
}
