using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;
using CodeStage.AntiCheat.ObscuredTypes;

public class UINumbersHandler : Singleton<UINumbersHandler>
{
    public ObscuredInt FullScore = 0, FullCoins = 0;

    public ObscuredInt realScoreValue = 0;

    [SerializeField]
    private Gradient killingSpreeGradient;

    [SerializeField]
    private TMPro.TextMeshProUGUI scoreText, coinsText, killingSpreeText, waveText;
    [SerializeField]
    private UIBossInfo bossInfo;

    [SerializeField]
    private Transform coinIcon;

    void Start()
    {
        realScoreValue = 50;
        GameManager.instance.EventManager.AddListener<RewardScoreEvent>( ev => 
        {
            if(ev.AllowMultiplier)
                AddScore(Mathf.FloorToInt(ev.ScoreReward * GameManager.instance.GameConstants.KillingSpreeMultiplier));
            else
                AddScore(ev.ScoreReward);
        });

        SetCoins(0);
        GameManager.instance.EventManager.AddListener<PlayerCurrenciesChangeEvent>(ev =>
        {
            if (ev.CurrentCurrencies.CoinAmount != FullCoins)
            {
                SetCoins(ev.CurrentCurrencies.CoinAmount);
            }
        });

        // The run counter, not the wave asset's index -- see RunWaveChangedEvent (S203). The two
        // agree in the campaign and diverge immediately in endless, where waves are drawn at
        // random and the index is not a wave number at all.
        //
        // S204: the "Wave" chip in develop_overhaul.unity was authored (same NumberBg frame as
        // Score and Coins) but shipped switched OFF and unfinished -- its "WAVE" label and its
        // number were two centre-aligned 336px text boxes 70px apart, so they overlapped, and the
        // chip sat inside the span the boss HP bar draws over. It is now on, the second label
        // object is off, and this one text carries the whole string.
        //
        // S205: the chip now sits DIRECTLY BELOW Score (founder call, wave-36 playtest). S204 had
        // put it in the band right of the boss bar, which is also where CollectedPerks lives --
        // that container is anchored top-RIGHT and spans canvas x 1545-2275, against wave text at
        // 1504-1840, so the two overlap by ~295px. It looked clean early only because a couple of
        // perk icons sit at the container's right end; the row fills leftward into the wave chip
        // as the run goes on. The chip is now aligned under Score's frame -- same bg sprite, same
        // scale, same x -- so the two read as one stacked HUD block. It shares a little vertical
        // band with the Boss row's RECT, which is a full-width stretch, but not with anything the
        // boss row draws: that bar is centred near x 1335 and this chip spans x 109-445.
        SetWave(1);
        GameManager.instance.EventManager.AddListener<RunWaveChangedEvent>(ev =>
        {
            SetWave(ev.WavesCleared + 1);
        });

        if (killingSpreeText != null)
        {
            killingSpreeText.text = "";
        }
    }

    private void SetCoins(int coinAmount)
    {
        FullCoins = coinAmount;
        coinsText.text = $"{coinAmount:0}/{GameManager.instance.Player.PlayerStats.Currencies.NextPerkCoins}";
        coinIcon.TweenLocalScale(new Vector3(1.14f, 1.14f, 1.14f), 0.15f).SetOnComplete(
            () =>
            {
                coinIcon.TweenLocalScale(new Vector3(1f, 1, 1f), 0.15f);
            });
    }

    private void SetWave(int waveNumber)
    {
        if (waveText != null)
        {
            waveText.text = $"WAVE {waveNumber}";
        }
    }

    public void AddScore(int score)
    {
        FullScore = Mathf.Clamp(FullScore + score, 0, int.MaxValue);

        /*if(FullScore > 16000)
        {
            GameManager.instance.GameConstants.EnemyDamage
        }*/

        scoreText.text = FullScore.ToString("N0");

        realScoreValue = Mathf.Clamp(realScoreValue + score, 0, int.MaxValue);

        /* var internalRealScore = DataEncryption.DecryptScore(realScoreValue);
         var value = internalRealScore + (uint)score;

         if( (int) internalRealScore + score < 0)
         {
             value = 0;
         }

         internalRealScore = value;

         realScoreValue = DataEncryption.EncryptScore(internalRealScore);*/
    }

    public void SetKillingSpree(int percentage, int max)
    {
        if (killingSpreeText == null)
        {
            return;
        }

        if(percentage == 0 || max == 0)
        {
            killingSpreeText.text = "";
            return;
        }

        killingSpreeText.text = $"+{percentage}%";
        killingSpreeText.color = killingSpreeGradient.Evaluate( (percentage*1.0f) / max);
    }

    public void SetBossInfo(bool show, int bossPoints = 0)
    {
        if(show)
        {
            bossInfo.SetValue(1);
            bossInfo.SetBossPoints(bossPoints);
        }

        var animator = GetComponent<Animator>();

        if(show)
        {
            animator.ResetTrigger("hide_boss_info");
            animator.SetTrigger("show_boss_info");
        }
        else
        {
            animator.ResetTrigger("show_boss_info");
            animator.SetTrigger("hide_boss_info");
        }
    }
}
