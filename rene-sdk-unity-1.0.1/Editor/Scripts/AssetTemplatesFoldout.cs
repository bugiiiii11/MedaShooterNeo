using System.Collections.Generic;
using System.Linq;
using Rene.Sdk.Api.Game.Data;
using ReneVerse;
using UnityEditor;
using UnityEngine;

public class AssetTemplatesFoldout
{
    private static AssetTemplatesFoldout _instance;
    private Dictionary<string, bool> ownableAssetsFoldoutStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> brandedAssetsFoldoutStates = new Dictionary<string, bool>();
    private Dictionary<string, bool> collectionFoldoutStates = new Dictionary<string, bool>();
    private string _adSurfacesFoldoutHeader;
    private bool adsurfacesFoldout;
    private bool assetsFoldout;
    private bool ownableAssetsFoldout;
    private bool brandedAssetsFoldout;


    private AssetTemplatesFoldout()
    {
        // Initialize the foldout states in the constructor
        var _currentGameRelatedPersistentTemplates = Helper.LoadAndGetGameRelatedPersistentData;
        if (_currentGameRelatedPersistentTemplates.GetGameData.Collections != null &&
            _currentGameRelatedPersistentTemplates.GetGameData.Collections.Items.Count > 0)
        {
            foreach (var collectionData in _currentGameRelatedPersistentTemplates.GetGameData.Collections.Items)
            {
                foreach (var asset in collectionData.OwnableAssets.Items)
                {
                    ownableAssetsFoldoutStates[asset.OwnableAssetId] = false; // Default state is closed
                }

                foreach (var asset in collectionData.BrandedAssets.Items)
                {
                    brandedAssetsFoldoutStates[asset.BrandedAssetId] = false; // Default state is closed
                }
            }
        }
    }

    /// <summary>
    /// Singleton pattern
    /// </summary>
    public static AssetTemplatesFoldout Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AssetTemplatesFoldout();
            }

            return _instance;
        }
    }

    private void DrawAdSurfacesGUI(List<AdSurfacesResponse.AdSurfacesData.AdSurface> getStandAloneAdSurfaces)
    {
        foreach (var adSurface in getStandAloneAdSurfaces)
        {
            EditorGUILayout.BeginVertical("box"); // Start a new vertical group for each ad surface
            ShowAdSurfaceInfo(adSurface);
            if (GUILayout.Button(Constants.CreateAdSurface))
            {
                CreateAdSurfaceArea(adSurface);
            }

            EditorGUILayout.EndVertical(); // End the vertical group
        }
    }


    private void DrawAssets(GameRelatedPersistentData gameDataContainer)
    {
        EditorGUI.indentLevel++;
        DrawOwnableAssets(gameDataContainer.GetGameData);
        DrawBrandedAssets(gameDataContainer.GetGameData);
        EditorGUI.indentLevel--;
    }

    private static bool NoOwnableAssets(GameResponse.GameData ownableAssets) =>
        !ownableAssets.Collections.Items.Any(collection => collection.OwnableAssets?.Items.Count > 0);

    private static bool NoBrandedAssets(GameResponse.GameData ownableAssets) =>
        !ownableAssets.Collections.Items.Any(collection => collection.BrandedAssets?.Items.Count > 0);

    private void DrawOwnableAssets(GameResponse.GameData gameData)
    {
        
        ownableAssetsFoldout = EditorGUILayout.Foldout(ownableAssetsFoldout, Constants.OwnableCollections);
        if (!ownableAssetsFoldout) return;

        EditorGUI.indentLevel++;
        foreach (var collection in gameData.Collections.Items.Where(c => c.OwnableAssets?.Items.Count > 0))
        {
            DrawCollectionAssets(collection, collection.OwnableAssets.Items, Constants.OwnableCollections);
        }
        EditorGUI.indentLevel--;
    }

    private void DrawBrandedAssets(GameResponse.GameData gameData)
    {
        brandedAssetsFoldout = EditorGUILayout.Foldout(brandedAssetsFoldout, Constants.BrandedCollections);
        if (!brandedAssetsFoldout) return;

        EditorGUI.indentLevel++;
        foreach (var collection in gameData.Collections.Items.Where(c => c.BrandedAssets?.Items.Count > 0))
        {
            DrawCollectionAssets(collection, collection.BrandedAssets.Items, Constants.BrandedCollections);
        }
        EditorGUI.indentLevel--;
        
    }

    private void DrawCollectionAssets(GameResponse.GameData.CollectionsData.CollectionData collection, IEnumerable<IAsset> assets, string assetType)
    {
        if (!collectionFoldoutStates.ContainsKey(collection.CollectionId))
            collectionFoldoutStates[collection.CollectionId] = false;

        collectionFoldoutStates[collection.CollectionId] = EditorGUILayout.Foldout(collectionFoldoutStates[collection.CollectionId], collection.Name);
        if (!collectionFoldoutStates[collection.CollectionId]) return;

        EditorGUI.indentLevel++;
        foreach (var asset in assets)
        {
            AssetsDraw(asset, $"No ad surfaces available for this {assetType.ToLower()}.");
        }
        EditorGUI.indentLevel--;
    }

    private void AssetsDraw(IAsset ownableAsset, string absenseOfAdsurfaces, string assetPrefix = null)
    {
        EditorGUILayout.BeginVertical("box"); // Start a new vertical group


        // Asset Template Header with Texture
        GUILayout.BeginHorizontal();
        GUILayout.Label(ownableAsset.GetTexture(), GUILayout.Width(50), GUILayout.Height(50));

        GUILayout.BeginVertical(); // Begin vertical layout for text

        GUILayout.Label(assetPrefix + ownableAsset.GetName, EditorStyles.boldLabel);

        GUILayout.Label(ownableAsset.GetDescription, EditorStyles.wordWrappedLabel); // Asset template description
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        // Check if Ad Surfaces exist
        if (ownableAsset?.GetadSurfaces?.Items != null && ownableAsset?.GetadSurfaces?.Items.Count > 0)
        {
            if (!ownableAssetsFoldoutStates.ContainsKey(ownableAsset.GetOwnableAssetId))
            {
                ownableAssetsFoldoutStates.Add(ownableAsset.GetOwnableAssetId, false);
            }

            ownableAssetsFoldoutStates[ownableAsset.GetOwnableAssetId] = EditorGUILayout.Foldout(
                ownableAssetsFoldoutStates[ownableAsset.GetOwnableAssetId],
                ownableAsset.GetName + " Ad Surfaces");

            if (ownableAssetsFoldoutStates[ownableAsset.GetOwnableAssetId])
            {
                // Draw the ad surfaces if the foldout is open
                DrawAdSurfacesGUI(ownableAsset.GetadSurfaces.Items);
            }
        }
        else
        {
            // No Ad Surfaces Message
            GUILayout.Label(absenseOfAdsurfaces, EditorStyles.miniLabel);
        }


        EditorGUILayout.EndVertical(); // End the vertical group
    }

    public void AdSurfacesFoldout(string labelText, GameRelatedPersistentData loadAndGetGameRelatedPersistentData)
    {
        if (loadAndGetGameRelatedPersistentData.GetGameData.AdSurfaces != null &&
            loadAndGetGameRelatedPersistentData.GetGameData.AdSurfaces.Items.Count == 0)
        {
            GUILayout.Label(labelText, EditorStyles.label);
        }
        else
        {
            adsurfacesFoldout = EditorGUILayout.Foldout(adsurfacesFoldout, Constants.StandaloneGameAdSurfaces);
            if (adsurfacesFoldout)
            {
                DrawAdSurfacesGUI(loadAndGetGameRelatedPersistentData.GetStandAloneAdSurfaces);
            }
        }
    }

    public void AssetsFoldouts(string noAssetsText, GameRelatedPersistentData gameDataContainer)
    {
        assetsFoldout = EditorGUILayout.Foldout(assetsFoldout, Constants.AssetsFoldoutHeader);
        if (assetsFoldout)
        {
            DrawAssets(gameDataContainer);
        }
    }

    private static bool NoAseetsFound(GameRelatedPersistentData loadAndGetGameRelatedPersistentData)
    {
        return loadAndGetGameRelatedPersistentData.GetGameData.Collections == null ||
               loadAndGetGameRelatedPersistentData.GetGameData.Collections.Items.Count == 0;
    }

    private void CreateAdSurfaceArea(AdSurfacesResponse.AdSurfacesData.AdSurface adSurface)
    {
        Vector3 scale = adSurface.ResolutionIab.ParseResolutionToScale();
        string loadPathPrefix;
        loadPathPrefix = Helper.GetCurrentRendererPipeline;
        if (adSurface.AdType == AdType.VIDEO)
        {
            AdSurfaceCreation(adSurface, loadPathPrefix, scale, "Video Ad Surface");
        }
        else if (adSurface.AdType == AdType.BANNER)
        {
            AdSurfaceCreation(adSurface, loadPathPrefix, scale, "Banner Ad Surface");
        }
    }

    private static void AdSurfaceCreation(AdSurfacesResponse.AdSurfacesData.AdSurface adSurface, string loadPathPrefix,
        Vector3 scale, string surface)
    {
        Transform parentTransform = Selection.activeGameObject != null ? Selection.activeGameObject.transform : null;
        var videoAdSurface = GameObject.Instantiate
            (Resources.Load($"{loadPathPrefix}/{surface}"), parentTransform) as GameObject;
        var videoAdSurfaceComponent = videoAdSurface.GetComponent<AdSurfaceBase>();
        videoAdSurfaceComponent.transform.localScale = scale;
        videoAdSurfaceComponent._adSurface = adSurface;
        EditorGUIUtility.PingObject(videoAdSurface);
        EditorUtility.SetDirty(videoAdSurfaceComponent);
    }

    private static void ShowAdSurfaceInfo(AdSurfacesResponse.AdSurfacesData.AdSurface adSurface)
    {
        GUILayout.Label("AdSurfaceId: " + adSurface.AdSurfaceId);
        GUILayout.Label("AdType: " + adSurface.AdType);
        GUILayout.Label("Interactivity: " + adSurface.Interactivity);
        GUILayout.Label("Video Resolution: " + adSurface.ResolutionIab);
    }

    public void DrawCenteredMessage(string message)
    {
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.Label(message, EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
    }
}