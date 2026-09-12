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

        // C2: the result screen used to show a bare number. Tell the player
        // WHICH run just ended -- and, after a daily, that Retry is not another
        // daily attempt. Wrapped because a summary line must never be what
        // stops a game-over screen from appearing.
        try
        {
            BuildRunSummary();
            RelabelRetryAfterDaily();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UIGameOverScreen] run summary skipped: {e.Message}");
        }

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

    /// <summary>
    /// "L2 COLD FRONT - WAVE 17 - DAILY" under the score. Built by cloning the
    /// score label at runtime, the same zero-scene-YAML pattern
    /// MsModeSelectBootstrap uses: nothing new is serialized into the scene, and
    /// the clone dies with it.
    /// </summary>
    private void BuildRunSummary()
    {
        if (PointsValueText == null || PointsValueText.textMesh == null)
            return;

        var source = PointsValueText.textMesh.gameObject;
        var clone = Instantiate(source, source.transform.parent, false);
        clone.name = "ms_run_summary";

        // The clone inherits the typewriter component, which would overwrite
        // the text with an animated number.
        foreach (var typer in clone.GetComponentsInChildren<UITextVisualInput>(true))
            typer.enabled = false;

        var sourceRect = source.GetComponent<RectTransform>();
        var rect = clone.GetComponent<RectTransform>();
        if (sourceRect != null && rect != null)
        {
            rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -70f);
            rect.sizeDelta = new Vector2(Mathf.Max(sourceRect.sizeDelta.x, 600f), 48f);
        }

        var text = clone.GetComponent<TextMeshProUGUI>();
        if (text == null)
            return;

        text.enableAutoSizing = true;
        text.fontSizeMax = 36f;
        text.fontSizeMin = 14f;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = RunSummaryLine();
    }

    private static string RunSummaryLine()
    {
        var level = Determinism.MsLevelSelect.EffectiveLevel;
        var name = Determinism.MsLevelSelect.LevelName(level);
        var waves = GameManager.instance != null && GameManager.instance.GameStats != null
            ? GameManager.instance.GameStats.WavesCount
            : 0;

        var line = string.IsNullOrEmpty(name)
            ? $"L{level} - WAVE {waves}"
            : $"L{level} {name} - WAVE {waves}";

        return Determinism.MsLevelSelect.IsDaily ? line + " - DAILY" : line;
    }

    /// <summary>
    /// After a daily run, Retry silently plays a NORMAL run of the sticky
    /// level -- the daily attempt was burned at /run/start, so replaying it as
    /// a daily would only mint a 409 (MsLevelSelect pref contract). That is
    /// correct behaviour and was invisible; say it on the button.
    ///
    /// The button is found by its serialized call to OnClickRetryButton rather
    /// than by name, so a renamed GameObject cannot silently break this.
    /// </summary>
    private void RelabelRetryAfterDaily()
    {
        if (!Determinism.MsLevelSelect.IsDaily)
            return;

        foreach (var button in GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            for (var i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) != nameof(OnClickRetryButton))
                    continue;

                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = "PLAY AGAIN (NORMAL RUN)";

                return;
            }
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
