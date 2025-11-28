using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ElRaccoone.Tweens;
using TMPro;
using System;
using System.Runtime.InteropServices;
using Cryptomeda.Minigames.BackendComs;

public class UIGameOverScreen : MonoBehaviour
{
    public GameObject EscMenu, CollectedPerks;
    public UITextVisualInput PointsValueText, TokensValueText;
    public RealtimeDurationChecker timeManager;
    private bool isGameOver = false;
    [DllImport("__Internal")]
    private static extern void SendGameOver();

    private void Start() 
    {
        GameManager.instance.EventManager.AddListener<PlayerDiedEvent>(OnPlayedDied);
    }

    private void OnPlayedDied(PlayerDiedEvent ev)
    {
        var canvGroup = GetComponent<CanvasGroup>();
        canvGroup.TweenCanvasGroupAlpha(1, 2).SetFrom(0).SetOnComplete(
            () =>
            {
                // fill in values
                PointsValueText.StartTypeWriter(UINumbersHandler.instance.FullScore);
                //TokensValueText.StartTypeWriter(UINumbersHandler.instance.FullCoins);
            });

        canvGroup.blocksRaycasts = true;
        GameManager.instance.PauseGame(true, false);

        // switch music
        DoubleAudioSource.instance.CrossFade(DoubleAudioSource.instance.Clips[2], 0.45f, 3f);

        EscMenu.SetActive(false);
        CollectedPerks.SetActive(false);

        GameManager.instance.EnemySpawner.KillAllEnemies();

        // send data to backend but wait for ending duration packet
        timeManager.OnGameOverTimestampReceived += OnTimestampReceived;

        isGameOver = true;
    }

    private void OnTimestampReceived(float unityTimestamp, float serverTimestamp, long code)
    {
        // send data to backend
        if (PlayerProfileInfo.instance)
        {
            int unityDuration;
            int serverDuration;
            if(code == 200)
            {
                unityDuration = Mathf.RoundToInt(unityTimestamp);
                serverDuration = Mathf.RoundToInt(serverTimestamp);
            }
            else
            {
                unityDuration = Mathf.RoundToInt(unityTimestamp);
                serverDuration = 0;
            }

            int scoreValue = UINumbersHandler.instance.realScoreValue - 50;
            var enc = DataEncryption.EncryptScore((uint)scoreValue);

            var walletAddress = PlayerProfileInfo.instance.WalletAddress;
            if(PlayerProfileInfo.instance.EquippedHero != null)
            {
                walletAddress = PlayerProfileInfo.instance.EquippedHero.OwnerWallet;
                Debug.Log($"Address of player ({PlayerProfileInfo.instance.WalletAddress}) was changed to owner of nft: {walletAddress}");
            }

            if (string.IsNullOrEmpty(walletAddress))
                walletAddress = PlayerProfileInfo.instance.WalletAddress;

            var json = JsonBuilder.BuildScore(enc, GameManager.instance.GameStats, PlayerProfileInfo.instance.WalletAddress, unityDuration, serverDuration);

            RestfulManager.Post(RestfulEndpoint.ScoreAndAddress, json, OnPostScoreResponse);
        }

#if UNITY_EDITOR
        else
        {
            int scoreValue = UINumbersHandler.instance.realScoreValue - 50;
            var enc = DataEncryption.EncryptScore((uint)scoreValue);
            int unityDuration;
            int serverDuration;
            if (code == 200)
            {
                unityDuration = Mathf.RoundToInt(unityTimestamp);
                serverDuration = Mathf.RoundToInt(serverTimestamp);
            }
            else
            {
                unityDuration = Mathf.RoundToInt(unityTimestamp);
                serverDuration = 0;
            }
            Debug.Log($"Game with duration {unityDuration} (realtime {serverDuration}) has ended.");
        }
#endif
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.T))
    //    {
    //        int scoreValue = UINumbersHandler.instance.realScoreValue - 50;
    //        var enc = DataEncryption.EncryptScore((uint)scoreValue);
    //        // var request = JsonBuilder.BuildScore(enc, GameManager.instance.GameStats, "0x2222222222222222222222222222222222222222");
    //        // RestfulManager.Post(RestfulEndpoint.ScoreAndAddress, request, OnPostScoreResponse);

    //        //Debug.Log(MetricComputer.BuildScore(enc, GameManager.instance.GameStats));
    //        Debug.Log(JsonBuilder.BuildScore(enc, GameManager.instance.GameStats, "0x0000000000000000000000000000000000000000"));
    //    }
    //}

    private void OnPostScoreResponse(Response obj)
    {
        if(obj.Code == 200)
        {
            Debug.Log("Score was updated successfully. Calling GameOver():\n\n" + obj.Text);
            SendGameOver();
        }
        else
        {
            Debug.Log(obj.Text);
            // try again?
        }
    }

    public void OnClickRetryButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickExitButton()
    {
        SceneManager.LoadScene(1);
    }
    
    /*void Update()
    {
        if (!isGameOver)
            return;
        // for key controls
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Joystick1Button1) || Input.GetKeyDown(KeyCode.Joystick1Button17))
            OnClickRetryButton();
    }*/
}
