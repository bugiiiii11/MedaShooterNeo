using CodeStage.AntiCheat.ObscuredTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBaseStats : ScriptableObject
{
    public ObscuredInt Health, Shield;
    public ObscuredFloat FireRatePerc, CritChancePerc, MovementSpeed;
    public Vector2Int Damage;
    public Vector2Int CritDamage;

    [NonSerialized]
    public bool IsShieldActiveFromStart = false;
    internal void UpdateStats(HeroBoostedStats stats)
    {
        if (stats == null)
            return;
        Health += Mathf.RoundToInt((stats.MaxHealthFactor - 1) * Health);
        Shield += stats.ShieldAddition;
        if (stats.ShieldAddition > 0.01)
            IsShieldActiveFromStart = true;

        MovementSpeed += (float)System.Math.Round((stats.PlayerSpeedFactor * 3.3f) - 3.3f, 2);
    }

    internal void UpdateStats(WeaponBoostedStats stats)
    {
        if (stats == null)
            return;

        CritChancePerc += stats.CriticalChanceIncrease;
        Damage += new Vector2Int(
            Mathf.CeilToInt(Damage.x * stats.DamageFactor),
            Mathf.CeilToInt(Damage.y * stats.DamageFactor));

        FireRatePerc -= stats.FireRateIncrease;
    }
}
