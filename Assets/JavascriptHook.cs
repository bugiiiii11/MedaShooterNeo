using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Determinism;
using UnityEngine;
using UnityEngine.SceneManagement;

public class JavascriptHook : MonoBehaviour
{
    public WaitForWalletAddress waiting;

    [DllImport("__Internal")]
    private static extern void SendWalletAddressReady();

    private void Start()
    {
        if (waiting)
        {
#if !UNITY_EDITOR
            SendWalletAddressReady();
#endif
        }
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            SetWalletAddress("0xfa186a8c30d6da6ef7b79c9e756e043d10a4e607");
    }
#endif


    public void SetWalletAddress(string address)
    {
        Debug.Log("Received address from web: "+ address);

        var currentScene = SceneManager.GetActiveScene();

        var player = PlayerProfileInfo.instance;

        Debug.Log("Setting address to player profile info.");
        player.WalletAddress = address;

        Debug.Log("Saving address to cache");
        PlayerPrefs.SetString("cached_addr_data", address);

        if (currentScene.buildIndex == 0)
        {
            Debug.Log("Handling scene transition...");

            // handle case for loading scene
            if (waiting && player.IsUserValid)
            {
                // go to main menu
                waiting.IsWalletAddressSetup = true;
                waiting.GoToMainMenu();
                Debug.Log("Scene transition handled");
            }
        }
        /*else if (currentScene.buildIndex == 1 || currentScene.buildIndex == 2)
        {
            // handle case for main menu and level scenes
            var overlay = player.GetComponent<WalletClosedAccessValidator>().BlockingOverlay;
            if (player.IsUserValid && overlay)
            {
                Destroy(overlay);
                if(GameManager.instance)
                {
                    GameManager.instance.PauseGame(false, true);
                }
            }
        }*/
    }

    /// <summary>
    /// Polish C1(b), S310 -- the mission choice, pushed in from the WEB PAGE.
    ///
    /// Payload is "&lt;level&gt;|&lt;daily&gt;|&lt;launch&gt;", e.g. "2|0|0" or "3|1|1".
    /// One string because SendMessage takes exactly one argument, and three
    /// separate calls could interleave with a scene load and apply half a
    /// choice.
    ///
    /// The page is now the level selector (`MSMissionSelect.jsx`); the three
    /// cloned in-scene buttons are suppressed the moment this arrives, so the
    /// player never sees two selectors disagreeing. Nothing here is trusted for
    /// scoring: level and mode still ride the run row and the anchor exactly as
    /// before, and the server re-derives the daily level from the date.
    ///
    /// LAUNCH only fires from the inventory scene -- that is the one place the
    /// old DAILY button existed and the only scene from which a gameplay load
    /// is meaningful. Anywhere else the daily flag is simply ARMED, so the next
    /// run the player starts is their daily attempt.
    /// </summary>
    public void SetRunMode(string payload)
    {
        if (string.IsNullOrEmpty(payload))
            return;

        var parts = payload.Split('|');
        if (parts.Length < 1)
            return;

        int level;
        if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out level)
            && level >= MsLevelSelect.MinLevel && level <= MsLevelSelect.MaxLevel)
        {
            MsLevelSelect.SetSelectedLevel(level);
        }

        var daily = parts.Length > 1 && parts[1] == "1";
        var launch = parts.Length > 2 && parts[2] == "1";

        // Order matters: mark ownership BEFORE tearing the clones down, so a
        // scene loaded after this point never rebuilds them either.
        MsLevelSelect.MarkExternalSelection();
        MsModeSelectBootstrap.SuppressInSceneSelector();

        if (daily)
            MsLevelSelect.ArmDaily();

        Debug.Log($"[JavascriptHook] Run mode from page: level={level} daily={daily} launch={launch}");

        if (launch && daily && MsModeSelectBootstrap.IsInventoryScene)
            SceneManager.LoadScene(MsModeSelectBootstrap.GameplaySceneIndex);
    }
}
