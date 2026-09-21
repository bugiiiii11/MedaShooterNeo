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
    /// LAUNCH used to fire only from the inventory scene (S310) -- anywhere
    /// else it was silently dropped while the daily flag stayed ARMED, so a
    /// player who pressed LAUNCH DAILY on the "Tap on screen to continue"
    /// splash saw nothing happen, and their NEXT campaign START would have
    /// been their daily attempt (S335, founder report). Since S335 a launch is
    /// PENDING until the gameplay scene loads: menu -> inventory -> gameplay,
    /// each hop driven by sceneLoaded, so LAUNCH works from wherever the build
    /// is sitting. The loading scene (index 0) is the one place it waits --
    /// the wallet handshake moves it to the menu on its own.
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

        if (launch && daily)
            RequestDailyLaunch();
    }

    // Build indices per ProjectSettings/EditorBuildSettings.asset:
    // 0 loading, 1 menu, 2 inventory, 3 develop_overhaul (gameplay).
    private const int LoadingSceneIndex = 0;
    private const int MenuSceneIndex = 1;
    private const int InventorySceneIndex = 2;

    private static bool pendingDailyLaunch;
    private static bool sceneHookInstalled;

    /// <summary>Starts (or resumes) the walk to the gameplay scene. A second
    /// request while one is pending is a no-op; a request during gameplay is
    /// ignored -- the run in progress is already the attempt.</summary>
    private static void RequestDailyLaunch()
    {
        var index = SceneManager.GetActiveScene().buildIndex;
        if (index == MsModeSelectBootstrap.GameplaySceneIndex)
        {
            Debug.Log("[JavascriptHook] daily launch ignored: gameplay scene already active");
            return;
        }

        if (!sceneHookInstalled)
        {
            sceneHookInstalled = true;
            SceneManager.sceneLoaded += OnSceneLoadedForPendingLaunch;
        }

        pendingDailyLaunch = true;
        AdvancePendingLaunch(index);
    }

    private static void OnSceneLoadedForPendingLaunch(Scene scene, LoadSceneMode mode)
    {
        if (pendingDailyLaunch)
            AdvancePendingLaunch(scene.buildIndex);
    }

    private static void AdvancePendingLaunch(int index)
    {
        switch (index)
        {
            case MenuSceneIndex:
                // Same hop the splash tap makes (SceneLoader.LoadLevel(2)).
                Debug.Log("[JavascriptHook] daily launch pending: menu -> inventory");
                SceneManager.LoadScene(InventorySceneIndex);
                break;
            case InventorySceneIndex:
                // The old DAILY button's exact action; the flag was armed in SetRunMode.
                pendingDailyLaunch = false;
                Debug.Log("[JavascriptHook] daily launch: inventory -> gameplay");
                SceneManager.LoadScene(MsModeSelectBootstrap.GameplaySceneIndex);
                break;
            case LoadingSceneIndex:
                Debug.Log("[JavascriptHook] daily launch pending: waiting for the wallet handshake to leave the loading scene");
                break;
            default:
                // Gameplay (or anything unknown): the walk is over.
                pendingDailyLaunch = false;
                break;
        }
    }
}
