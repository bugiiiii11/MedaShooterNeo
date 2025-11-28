using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;

public class UICountdown : Singleton<UICountdown>
{
    public TMPro.TextMeshProUGUI countdownText;
    public CanvasGroup group;
    private bool canCountdown = true;

    public override void Awake()
    {
        base.Awake();
        GameManager.instance.EventManager.AddListener<SoundAndMusicSettingsEvent>(OnCountdownSettingChanged);
    }

    private void OnCountdownSettingChanged(SoundAndMusicSettingsEvent obj)
    {
        canCountdown = obj.CountdownsEnabled;
    }

    public static void InitCountdown(int counts, float timeBetween, Action callback)
    {
        if (instance.canCountdown)
            instance.StartCoroutine(instance.InitCountdownCo(counts, timeBetween, callback));
        else
            callback?.Invoke();
    }

    private IEnumerator InitCountdownCo(int counts, float timeBetween, Action callback)
    {
        group.alpha = 1;
        group.blocksRaycasts = true;

        countdownText.transform.localScale = Vector3.zero;
        var three = timeBetween * 3 / 4f;
        var third = timeBetween * 1 / 4f;
        for (var i = counts; i >= 0; i--)
        {
            countdownText.transform.localScale = Vector3.zero;
            var col = countdownText.color;
            col.a = 1;
            countdownText.color = col;

            if (i == 0)
                countdownText.text = "GO!";
            else
                countdownText.text = i.ToString();

            countdownText.TweenLocalScale(Vector3.one, three);
            yield return new WaitForSecondsRealtime(three);

            countdownText.TweenTextMeshAlpha(0, third);
            yield return new WaitForSecondsRealtime(third);
        }

        group.alpha = 0;
        group.blocksRaycasts = false;
        callback?.Invoke();
    }
}
