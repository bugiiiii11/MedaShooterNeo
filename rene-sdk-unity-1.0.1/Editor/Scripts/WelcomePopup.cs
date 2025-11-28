using ReneVerse;
using UnityEditor;
using UnityEngine;

public class WelcomePopup : AssetPostprocessor
{
    private const string Welcomepopupshown = "WelcomePopupShown";

    private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        ShowWelcomePopup();
    }


    private static void ShowWelcomePopup()
    {
        // Check if the pop-up has been shown before
        if (EditorPrefs.GetBool(Welcomepopupshown)) return;

        // Show the pop-up with two buttons
        bool option = EditorUtility.DisplayDialog(Constants.WelcomePopUpTitle, Constants.WelcomePopUpMessage, 
            Constants.Ok, Constants.CancelWelcomePopUp );

        if (!option) // If "Learn More" is clicked
        {
            Application.OpenURL($"{Constants.DocsURL}");
        }

        // Mark the pop-up as shown so it doesn't appear again
        EditorPrefs.SetBool(Welcomepopupshown, true);
    }
}