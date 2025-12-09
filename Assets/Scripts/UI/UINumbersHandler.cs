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
    private TMPro.TextMeshProUGUI scoreText, coinsText, killingSpreeText;
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

        killingSpreeText.text = "";
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
