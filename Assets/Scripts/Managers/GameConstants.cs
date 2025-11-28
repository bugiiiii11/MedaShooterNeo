using CodeStage.AntiCheat.ObscuredTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameConstants
{
    public static GameConstants Constants => GameManager.instance.GameConstants;

    [Header("Multiply speed of everything")]
    public ObscuredFloat GameSpeedMultiplier = 1;

    [Header("Temporary changes")]
    public ObscuredFloat EnemyProjectileSpeedMultiplier = 1.0f;

    public ObscuredInt CoinDropAmountMultiplier = 1;

    public ObscuredBool IsPlayerInvincible = false;

    public ObscuredFloat FireRateMultiplier = 1;

    public ObscuredFloat CriticalChanceProbabilityMultiplier = 1;

    public ObscuredInt CriticalChanceDamageMultiplier = 1;

    public ObscuredBool IsTntActive = false;

    public ObscuredBool IsFallenAngelActive = false;

    public ObscuredBool CanInterruptKillingSpree = true;

    public bool DisarmEnemy = false;


    [Header("Permanent changes")]
    public ObscuredFloat PermanentFireRateMultiplier = 0;
    public ObscuredFloat KillingSpreeMultiplier = 1;
    public ObscuredFloat DodgeChance = 0;
    public ObscuredInt AdditionalWeaponPowerupDuration = 0;
    public ObscuredFloat OverallCritChanceIncrease = 0;
    public ObscuredFloat InstantKillChance = 0;
    public ObscuredInt UltimateCooldownReduction = 0;
    public ObscuredFloat EnemyDropShieldChance = 0;

}
