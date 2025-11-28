using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerProfileInfo))]
public class WalletClosedAccessValidator : MonoBehaviour
{
    public GameObject NotValidCanvasPrefab;
    public GameObject BlockingOverlay;

    public void Validate(string walletAddress)
    {
        var isAllowed = WalletCsvParser.instance.IsAllowed(walletAddress);

        if(!isAllowed)
        {
            GetComponent<PlayerProfileInfo>().IsUserValid = false;

            if(!BlockingOverlay)
                BlockingOverlay = Instantiate(NotValidCanvasPrefab);

            if(GameManager.instance)
                GameManager.instance.PauseGame(true, true);
        }
        else
        {
            GetComponent<PlayerProfileInfo>().IsUserValid = true;
            if(BlockingOverlay)
            {
                Destroy(BlockingOverlay);
                if(GameManager.instance)
                    GameManager.instance.PauseGame(false, true);
            }
        }
    }
}
