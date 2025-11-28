using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ReneVerse
{
    public static class Helper
    {
        public static Material GetCorrespondentRenderPipelineMaterial()
        {
            return Resources.Load<Material>(GraphicsSettings.currentRenderPipeline == null
                ? "Materials/BuiltinMaterial"
                : "Materials/URPMaterial");
        }

        public static string GetCurrentRendererPipeline =>
            GraphicsSettings.currentRenderPipeline == null ? "BuiltIn" : "URP";
        public static GameRelatedPersistentData LoadAndGetGameRelatedPersistentData
        {
            get
            {
                GameRelatedPersistentData gameRelatedPersistentTemplates =
                    Resources.Load<GameRelatedPersistentData>("GameAssetTemplates");
                if (gameRelatedPersistentTemplates == null)
                {
                    // If not found, create a new instance
                    gameRelatedPersistentTemplates = ScriptableObject.CreateInstance<GameRelatedPersistentData>();

#if UNITY_EDITOR
                    // Ensure the Resources directory exists
                    string resourcesPath = "Assets/rene-sdk-unity/Plugins/ReneVerse/Resources";
                    if (!AssetDatabase.IsValidFolder(resourcesPath))
                    {
                        AssetDatabase.CreateFolder("Assets/rene-sdk-unity/Plugins/ReneVerse", "Resources");
                    }

                    // Save the new instance as an asset in the Unity project (only in the Unity Editor)
                    string assetPath = resourcesPath + "/GameAssetTemplates.asset";
                    AssetDatabase.CreateAsset(gameRelatedPersistentTemplates, assetPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
#endif
                }

                return gameRelatedPersistentTemplates;
            }
        }
        public static void ResetGameRelatedPersistentData()
        {
            // Load the existing GameRelatedPersistentData asset
            GameRelatedPersistentData gameRelatedPersistentData = Resources.Load<GameRelatedPersistentData>("GameAssetTemplates");
            if (gameRelatedPersistentData != null)
            {
                // Reset the fields of the scriptable object to their default values
                gameRelatedPersistentData.CleanGameData();

#if UNITY_EDITOR
                // Mark the object as dirty to ensure the changes are saved
                EditorUtility.SetDirty(gameRelatedPersistentData);
                AssetDatabase.SaveAssets();
#endif
            }
            else
            {
                // Optionally handle the case where the asset does not exist
                Debug.LogError("GameRelatedPersistentData not found. Make sure it exists and is located at 'Assets/rene-sdk-unity/Plugins/ReneVerse/Resources/GameAssetTemplates.asset'");
            }
        }

        public static IEnumerator LoadTextureFromURLAndSettingMeshRenderer(string url, MeshRenderer targetQuad)
        {
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                    Material mat = new Material(GetCorrespondentRenderPipelineMaterial())
                    {
                        mainTexture = texture
                    };
                    targetQuad.material = mat;
                }
                else
                {
                    Debug.LogError($"Failed to load texture from URL: {uwr.error}");
                }
            }
        }


        public static string InsertSpacesBeforeUppercase(this string text)
        {
            return Regex.Replace(text, "(\\B[A-Z])", " $1");
        }

        public static Texture2D GetTextureFromString(string MainImageTextureString)
        {
            if (string.IsNullOrEmpty(MainImageTextureString)) return null;

            byte[] imageBytes = Convert.FromBase64String(MainImageTextureString);
            Texture2D
                texture = new Texture2D(2, 2); // The size here is a placeholder, it will be replaced by LoadImage.
            texture.LoadImage(imageBytes);
            return texture;
        }

        public static Texture2D GetTextureFromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;

            byte[] imageBytes = Convert.FromBase64String(base64);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageBytes);
            return texture;
        }
    }
}