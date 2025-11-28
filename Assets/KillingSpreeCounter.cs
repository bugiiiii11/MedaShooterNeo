using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillingSpreeCounter : MonoBehaviour
{
    public bool IsKillingSpree => CurrentKillingSpree >= 4;
    public int CurrentKillingSpree = 0;

    private float killingSpreeStart, longestKillingSpree;

    private void Start()
    {
        GameManager.instance.EventManager.AddListener<EnemyKilledEvent>(OnEnemyKilled);
        GameManager.instance.EventManager.AddListener<PlayerDamagedEvent>(OnPlayerDamaged);
    }

    private void OnPlayerDamaged(PlayerDamagedEvent obj)
    {
        if (GameConstants.Constants.CanInterruptKillingSpree)
        {
            // send to stats
            InterruptKillingSpree();

        }
    }

    public void InterruptKillingSpree()
    {
        if (IsKillingSpree && Time.time - killingSpreeStart > longestKillingSpree)
        {
            var multValue = Mathf.RoundToInt((((float)GameManager.instance.GameConstants.KillingSpreeMultiplier) - 1f) * 100);

            longestKillingSpree = Time.time - killingSpreeStart;
            GameManager.instance.GameStats.LongestKillingSpreeDuration = Mathf.RoundToInt(longestKillingSpree);
            GameManager.instance.GameStats.LongestKillingSpreeMult = multValue;
        }

        if(IsKillingSpree)
        {
            OneShotAudioPool.SpawnOneShot(LevelProps.instance.KillingSpreeOver, volume: 0.5f, secondsAlive: 2);
        }

        GameManager.instance.GameConstants.KillingSpreeMultiplier = 1;
        CurrentKillingSpree = 0;
        UINumbersHandler.instance.SetKillingSpree(0, 100);
    }

    private void OnEnemyKilled(EnemyKilledEvent obj)
    {
        CurrentKillingSpree++;

        if(CurrentKillingSpree == 4)
        {
            OneShotAudioPool.SpawnOneShot(LevelProps.instance.KillingSpree,0.5f);
            killingSpreeStart = Time.time;
        }

        if (IsKillingSpree)
        {
            GameManager.instance.GameStats.EnemiesKilledWhileKillingSpree++;

            // killing spree was changed so we have to start counting again
            killingSpreeStart = Time.time;

            var value = GameManager.instance.GameConstants.KillingSpreeMultiplier;
            value += 0.1f;
            value = Mathf.Clamp(value, 1, 2);
            GameManager.instance.GameConstants.KillingSpreeMultiplier = value;

            UINumbersHandler.instance.SetKillingSpree( Mathf.RoundToInt(value * 100) - 100 , 100);
            var maxKillingSpreeValue = Mathf.RoundToInt((value - 1) * 100);
            if (maxKillingSpreeValue > GameManager.instance.GameStats.MaxKillingSpree)
            {
                GameManager.instance.GameStats.MaxKillingSpree = maxKillingSpreeValue;
            }
        }
    }
}
