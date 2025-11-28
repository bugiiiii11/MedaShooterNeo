using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.Detectors;
using System;
using Cryptomeda.Minigames.BackendComs;

public class CheatDetector : Singleton<CheatDetector>
{
    public GameObject OnCheatedScreen;

    private void Start()
    {
        ObscuredCheatingDetector.StartDetection(OnMemoryCheatDetected);
        SpeedHackDetector.StartDetection();
    }

    public void OnMemoryCheatDetected()
    {
        if(PlayerProfileInfo.instance)
            RestfulManager.Post(RestfulEndpoint.Cheating, JsonBuilder.BuildCheating(PlayerProfileInfo.instance.WalletAddress), OnCheaterReported);

        UINumbersHandler.instance.FullScore = 0;
        UINumbersHandler.instance.realScoreValue = 50;
        GameManager.instance.PauseGame(true, true);
        OnCheatedScreen.SetActive(true);
    }

    private void OnCheaterReported(Response obj)
    {
    }
}
