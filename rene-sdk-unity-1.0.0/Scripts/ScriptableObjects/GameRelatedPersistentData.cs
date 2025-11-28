using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rene.Sdk.Api.Game.Data;
using UnityEngine;
using UnityEngine.Networking;

public class GameRelatedPersistentData : ScriptableObject
{
    public GameResponse.GameData GameData;


    public List<AdSurfacesResponse.AdSurfacesData.AdSurface> GetStandAloneAdSurfaces => GameData.AdSurfaces.Items;


    public async Task SaveGameRelatedData(GameResponse.GameData gameData)
    {
        //For cleaning the previous ones
        GameData = gameData;
        foreach (var collectionData in gameData.Collections.Items)
        {
            foreach (var ownableAsset in collectionData.OwnableAssets.Items)
            {
                ownableAsset.MainImageTextureString = await DownloadImageAsBase64(ownableAsset.Image.Url);
            }

            foreach (var brandedAsset in collectionData.BrandedAssets.Items)
            {
                brandedAsset.MainImageTextureString = await DownloadImageAsBase64(brandedAsset.Image.Url);
            }
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    private async Task<string> DownloadImageAsBase64(string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            await AwaitUnityWebRequest(uwr);
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading image: " + uwr.error);
                return null;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
            byte[] imageBytes = texture.EncodeToPNG(); // or EncodeToJPG based on your requirement
            return Convert.ToBase64String(imageBytes);
        }
    }

    private async Task<Texture2D> DownloadImage(string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            await AwaitUnityWebRequest(uwr);

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error loading image: " + uwr.error);
                return null;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
            return texture;
        }
    }

    private async Task AwaitUnityWebRequest(UnityWebRequest uwr)
    {
        var operation = uwr.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();
    }
}