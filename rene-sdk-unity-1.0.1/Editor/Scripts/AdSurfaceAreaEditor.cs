using ReneVerse;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AdSurfaceBase), true), CanEditMultipleObjects]
public class AdSurfaceAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        // For non-play mode, show fields for a single object
        // and a message for multiple objects
        if (targets.Length == 1)
        {
            AdSurfaceBase adSurfaceArea = (AdSurfaceBase)target;
            if (adSurfaceArea._adSurface != null)
            {
                MakeAdFieldsReadOnly(adSurfaceArea);
            }
        }
        else
        {
            EditorGUILayout.HelpBox
            ($"Multi-object editing of {nameof(AdSurfaceBase)} properties is not supported. However you can Play/Stop Ad at runtime",
                MessageType.Info);
        }
        
        // Always show the Play/Stop button in play mode
        if (Application.isPlaying)
        {
            SwitchAdPlayStateAtRuntime();
        }


    }


    private void SwitchAdPlayStateAtRuntime()
    {
        // Determine if any of the selected AdSurfaceAreas is serving an ad
        bool anyAdServing = false;
        foreach (Object obj in targets)
        {
            AdSurfaceBase adSurfaceArea = obj as AdSurfaceBase;
            if (adSurfaceArea != null && adSurfaceArea.IsAdServing)
            {
                anyAdServing = true;
                break;
            }
        }

        string buttonLabel = anyAdServing ? "Disable Ad" : "Serve Ad";
        if (GUILayout.Button(buttonLabel))
        {
            foreach (Object obj in targets)
            {
                AdSurfaceBase adSurfaceArea = obj as AdSurfaceBase;
                if (adSurfaceArea == null) continue;

                if (anyAdServing)
                {
                    adSurfaceArea.DisableAd();
                }
                else
                {
                    adSurfaceArea.ServeAd();
                }
            }
        }
    }


    private static void MakeAdFieldsReadOnly(AdSurfaceBase adSurfaceArea)
    {
        // Make the following UI elements read-only
        GUI.enabled = false;

        // Display each field of _adSurfaceWrapper
        EditorGUILayout.TextField("Ad Surface Id", adSurfaceArea._adSurface.AdSurfaceId);
        EditorGUILayout.EnumPopup("Ad Type", adSurfaceArea._adSurface.AdType);
        EditorGUILayout.EnumPopup("Interactivity", adSurfaceArea._adSurface.Interactivity);
        EditorGUILayout.TextField("Resolution", adSurfaceArea._adSurface.ResolutionIab);
        // Re-enable editing for other fields
        GUI.enabled = true;
    }
}