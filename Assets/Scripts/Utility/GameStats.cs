using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameStats
{
    public int EnemiesSpawned = 0;
    public int EnemiesKilled = 0;
    public int WavesCount = 0;
    public float DistanceTraveled = 0;
    public int PerksCollected = 0;
    public int ShieldsCollected = 0;
    public int AbilityUseCount = 0;

    public int MaxScorePerEnemy = 0, MaxScorePerEnemyScaled = 0;

    public int LongestKillingSpreeMult = 0, LongestKillingSpreeDuration = 0, MaxKillingSpree = 0;

    // scaled by x100
    public int AttackSpeed => Mathf.RoundToInt(GameManager.instance.Player.GetComponent<WeaponController>().FireCooldown * GameConstants.Constants.FireRateMultiplier * (1 - GameConstants.Constants.PermanentFireRateMultiplier)*100);

    public int EnemiesKilledWhileKillingSpree = 0;
}
