using Cryptomeda.Minigames.BackendComs;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


public class RealtimeDurationChecker : MonoBehaviour
{
    public float GameTimeDuration = 0;
    public float RealGameTimeDuration = 0;

    public Action<float, float, long> OnGameOverTimestampReceived;

    private float startTime, endTime, pausedTime, additiveDuration;
    private DateTime realStartTime, realEndTime;
    private bool wasPaused = false, isInternallyPaused = false;

    private void Start()
    {
        GetServerTimestamp( (dateTime, _) =>
        {
            realStartTime = dateTime;
            startTime = Time.time;
        });
        additiveDuration = 0;
        JuiceRuntime.ResetStolenSeconds();
        GameManager.instance.EventManager.AddListener<PlayerDiedEvent>(OnGameOver);
        GameManager.instance.EventManager.AddListener<GamePauseEvent>( (obj) =>
        {
            isInternallyPaused = obj.IsPaused;
            OnGamePaused(obj, false);
        });
    }

    private void OnApplicationPause(bool pause)
    {
        if (isInternallyPaused)
            return;

        OnGamePaused(new GamePauseEvent(pause), true);
    }

    private bool objPauseActive = false;

    private void OnGamePaused(GamePauseEvent obj, bool isApplicationFlag = false)
    {
        if (obj.IsPaused == objPauseActive)
            return;

        if (Time.timeScale == 0 || (isApplicationFlag && obj.IsPaused))
        {
            if (!wasPaused)
            {
                wasPaused = true;
                pausedTime = Time.realtimeSinceStartup;
            }
        }
        else if (wasPaused)
        {
            wasPaused = false;
            additiveDuration += Time.realtimeSinceStartup - pausedTime;
        }

        objPauseActive = obj.IsPaused;
    }

    private void OnGameOver(PlayerDiedEvent obj)
    {
        GetServerTimestamp( (dateTime,code) =>
        {
            realEndTime = dateTime;
            endTime = Time.time;

            // The hit-stop in JuiceRuntime slows Time.timeScale, and this duration is measured
            // in SCALED time while the value it is compared against server-side is real elapsed
            // time. Without adding the shortfall back, a long run drifts steadily toward
            // "finished faster than physically possible" -- the exact shape of a speed hack.
            // The existing additiveDuration does the same job for pauses.
            var unityDuration = endTime - startTime + additiveDuration + JuiceRuntime.StolenSeconds;

            OnGameOverTimestampReceived.Invoke( unityDuration, (float) realEndTime.Subtract(realStartTime).Duration().TotalSeconds, code);
        });
    }

    public static void GetServerTimestamp(Action<DateTime, long> utc)
    {
        RestfulManager.Get(RestfulEndpoint.Timestamp, (response) =>
        {
            if (long.TryParse(response.Text, out var result))
            {
                var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(result);
                utc.Invoke(dateTimeOffset.DateTime, response.Code);
            }
            else
            {
                utc.Invoke(DateTime.UtcNow, response.Code);
            }
        });
    }
}
