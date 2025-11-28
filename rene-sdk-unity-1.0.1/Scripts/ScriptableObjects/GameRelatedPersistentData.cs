using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rene.Sdk.Api.Game.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class GameRelatedPersistentData : ScriptableObject
{
    private GameResponse.GameData _gameData;
    public GameResponse.GameData GetGameData => _gameData;
    public void CleanGameData() => _gameData = null;

    public bool IsGameDataAvailable => _gameData?.GameId != null;


    public List<AdSurfacesResponse.AdSurfacesData.AdSurface> GetStandAloneAdSurfaces => GetGameData.AdSurfaces.Items;

#if UNITY_EDITOR
    public async Task SaveGameRelatedData(GameResponse.GameData gameData)
    {
        //For cleaning the previous ones
        _gameData = gameData;
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

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

#endif


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