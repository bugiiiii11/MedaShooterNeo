using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitForWalletAddress : MonoBehaviour
{
    public GameObject LoadingObject, WaitingText, ManualAddressPanel;

    public bool IsWalletAddressSetup = false;

    private float startWaitTime = 0;

    public float MaxWaitTime = 15f;

    public Button CancelButton;

    private void Start()
    {
        startWaitTime = Time.time;
    }

    void Update()
    {
        if(!IsWalletAddressSetup && Time.time - startWaitTime > MaxWaitTime)
        {
            // wallet address not set up
            CancelButton.gameObject.SetActive(true);
            LoadingObject.SetActive(false);
        }
    }

    public void EnterAddressManually()
    {
        IsWalletAddressSetup = true;
        ManualAddressPanel.SetActive(true);
    }

    public void SetAddressManually(CryptoWalletValidator address)
    {
        IsWalletAddressSetup = true;
        print(PlayerProfileInfo.instance);
        PlayerProfileInfo.instance.WalletAddress = address.field.text;

        if(PlayerProfileInfo.instance.IsUserValid)
            GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("menu");
    }
}
