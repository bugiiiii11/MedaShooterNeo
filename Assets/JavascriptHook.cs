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
                // The old DAILY button's exact action -- but NOT this frame.
                // A human pressing DAILY had been sitting here long enough for
                // the NFT fetch to land; this walk arrives the instant the
                // scene loads, before InventoryBackend.Start has even run, so
                // hopping straight to gameplay ran the daily on base stats
                // with no hero, no weapon and no land shield (S343). Wait for
                // the inventory instead.
                WaitForInventoryThenLaunch();
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

    /// <summary>How long the launch will wait for the inventory fetches before
    /// going anyway. Generous: the four calls land in well under a second on a
    /// warm backend, and the shield's own delayed checks already span 5 s.</summary>
    private const float InventoryWaitTimeoutSeconds = 10f;

    private static LaunchWaitRunner launchWaitRunner;

    /// <summary>Holds the launch in the inventory scene until the NFT fetches
    /// have landed, then makes the hop. Fail-open by design: a dead backend
    /// costs the player a weaker daily, never a run they cannot start.</summary>
    private static void WaitForInventoryThenLaunch()
    {
        if (launchWaitRunner != null)
            return; // already waiting -- a second sceneLoaded must not restack it

        launchWaitRunner = new GameObject("ms_daily_launch_waiter").AddComponent<LaunchWaitRunner>();
        // Survives the gameplay load so the coroutine can finish its last line;
        // it destroys itself immediately after.
        Object.DontDestroyOnLoad(launchWaitRunner.gameObject);
        launchWaitRunner.StartCoroutine(WaitForInventory());
    }

    private static IEnumerator WaitForInventory()
    {
        var waited = 0f;
        while (waited < InventoryWaitTimeoutSeconds && !InventoryBackend.InventorySettled)
        {
            // The player can still leave -- back to the menu, or a reload. The
            // walk is not over, so leave pendingDailyLaunch armed and let
            // sceneLoaded pick it up wherever they land.
            if (SceneManager.GetActiveScene().buildIndex != InventorySceneIndex)
            {
                Debug.Log("[JavascriptHook] daily launch: left the inventory scene while waiting, launch stays pending");
                EndInventoryWait();
                yield break;
            }

            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (InventoryBackend.InventorySettled)
            Debug.Log($"[JavascriptHook] daily launch: inventory ready after {waited:0.00}s -> gameplay");
        else
            Debug.LogWarning($"[JavascriptHook] daily launch: inventory still not ready after {InventoryWaitTimeoutSeconds:0}s -> launching anyway");

        pendingDailyLaunch = false;
        SceneManager.LoadScene(MsModeSelectBootstrap.GameplaySceneIndex);
        EndInventoryWait();
    }

    private static void EndInventoryWait()
    {
        if (launchWaitRunner == null)
            return;

        Object.Destroy(launchWaitRunner.gameObject);
        launchWaitRunner = null;
    }

    /// <summary>Empty MonoBehaviour that hosts the launch wait. It outlives the
    /// inventory scene on purpose -- a runner that died with the scene would
    /// strand the player on the inventory screen with the daily still armed,
    /// which is the exact S335 failure this walk exists to prevent.</summary>
    private class LaunchWaitRunner : MonoBehaviour
    {
    }
}
