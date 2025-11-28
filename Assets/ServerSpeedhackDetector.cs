using Cryptomeda.Minigames.BackendComs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

namespace Cryptomeda.Minigames.Detectors
{
    public class ServerSpeedhackDetector : MonoBehaviour
    {
        [Header("Setup")]
        [Tooltip("Check detection every X seconds")]
        public int DetectionRepeatTime = 5;
        [Tooltip("In seconds")]
        public float DetectionThreshold = 2;
        public int MaxFalsePositives = 2;

        private float startTime = 0;
        private DateTime startDateTime;
        private bool isFirstCoordinationRequest = true;
        private Coroutine coroutine;

        private int currentFalsePositives = 0;
        private float startupDetectionThreshold = 1;

        public UnityEvent OnCheatDetected;

        private bool isGamePaused = false, isInternallyPaused = false;

        public bool IsDetectionRunning { get; set; }
        private Stopwatch stopWatch;
        private void Awake()
        {
            startupDetectionThreshold = DetectionThreshold;
            stopWatch = Stopwatch.StartNew();

            // Add your listener to any event that pauses the game. When time flow is changed,
            // this detector has to adjust to changes or reset itself. By using OnGamePaused
            // method, we reset the detector when unpausing.
            GameManager.instance.EventManager.AddListener<GamePauseEvent>((ev) =>
            {
                isInternallyPaused = ev.IsPaused;
                OnGamePaused(ev);
            });

            GameManager.instance.EventManager.AddListener<PlayerDiedEvent>((ev) =>
            {
                Stop();
                enabled = false;
            });

            StartCoroutine(OffsetAwake());
        }

        private IEnumerator OffsetAwake()
        {
            MeasureTimeDifference();
            Stop();
            yield return new WaitForSecondsRealtime(1);
            ResetAndPlay();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (isInternallyPaused)
                return;
            OnGamePaused(new GamePauseEvent(!focus));
        }

        private void OnApplicationPause(bool pause)
        {
            if (isInternallyPaused)
                return;

            OnGamePaused(new GamePauseEvent(pause));
        }

        private void OnGamePaused(GamePauseEvent obj)
        {
            if (isGamePaused == obj.IsPaused)
                return;

            isGamePaused = obj.IsPaused;

            if (isGamePaused)
            {
                Stop();
            }
            else if(!IsDetectionRunning)
            {
                ResetAndPlay();
            }
        }

        public void Stop()
        {
            IsDetectionRunning = false;

            if(coroutine != null)
                StopCoroutine(coroutine);
        }

        public void ResetAndPlay()
        {
            DetectionThreshold = startupDetectionThreshold;
            isFirstCoordinationRequest = true;
            IsDetectionRunning = true;
            coroutine = StartCoroutine(InvokeRealtimeCo(MeasureTimeDifference, DetectionRepeatTime));
        }

        private IEnumerator InvokeRealtimeCo(Action action, float repeatTime)
        {
            while (true)
            {
                action?.Invoke();
                yield return new WaitForSecondsRealtime(repeatTime);
            }
        }

        private void MeasureTimeDifference()
        {
            stopWatch.Restart();
            RestfulManager.Get(RestfulEndpoint.Timestamp, OnGetTimestamp);
        }

        private void OnGetTimestamp(Response obj)
        {
            var fullRequestTime = stopWatch.Elapsed.TotalSeconds;

            if (long.TryParse(obj.Text, out var result))
            {
                var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(result);
                var dateTime = dateTimeOffset.DateTime;

                if (isFirstCoordinationRequest)
                {
                    isFirstCoordinationRequest = false;

                    startDateTime = dateTime;
                    startTime = Time.time;
                }

                var differenceUtc = (float)dateTime.Subtract(startDateTime).Duration().TotalSeconds;
                var differenceUnity = Time.time - startTime;
                var detectionSpan = Mathf.Abs(differenceUtc - differenceUnity);

              //  print($"UTC: {differenceUtc} UNT: {differenceUnity} -> {detectionSpan} < {DetectionThreshold + fullRequestTime}");

                if (detectionSpan > DetectionThreshold + fullRequestTime)
                {
                    currentFalsePositives++;
                    ResetAndPlay();
                    if (currentFalsePositives >= MaxFalsePositives)
                    {
                        OnCheatDetected.Invoke();
                    }
                }
            }
        }
    }
}