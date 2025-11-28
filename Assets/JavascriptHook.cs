using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
}
