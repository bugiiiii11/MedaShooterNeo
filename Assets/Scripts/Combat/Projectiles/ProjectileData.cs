using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public struct ProjectileData
{
    [NonSerialized]
    public int BaseDamage;

    public DamageData AdditionalData;

    internal DamageInfo ComputeDamage()
    {
        var critical = AdditionalData.ComputeCriticalDamage();
        var damageInfo = new DamageInfo(BaseDamage + critical, critical > 0);

        var critSize = (critical - AdditionalData.CricitalChanceBonusDamage.x)*1.3f / AdditionalData.CricitalChanceBonusDamage.y;
        damageInfo.CriticalSize = critSize;

        return damageInfo;
    }

    public ProjectileData(ProjectileData other) 
    {
        BaseDamage = other.BaseDamage;
        AdditionalData = other.AdditionalData;
    }
}

[Serializable]
public struct DamageData
{
    public float CriticalChance;

    [MinMaxSlider(0, 100)]
    public Vector2Int CricitalChanceBonusDamage;

    internal int ComputeCriticalDamage()
    {
        var damage = 0;

        if(UnityEngine.Random.value < CriticalChance * GameManager.instance.GameConstants.CriticalChanceProbabilityMultiplier + GameConstants.Constants.OverallCritChanceIncrease)
        {
            damage = CricitalChanceBonusDamage.Random() * GameManager.instance.GameConstants.CriticalChanceDamageMultiplier;
        }

        return damage;
    }
}

public class DamageInfo
{
    public int DamageValue{get;set;}
    public bool IsCritical {get;set;}

    public float CriticalSize {get;set;}

    public DamageInfo(int value, bool critical)
    {
        DamageValue = value;
        IsCritical = critical;
    }
}

public class HealInfo
{
    public int HealValue{get;set;}
    public bool IsCritical {get;set;}

    public float CriticalSize {get;set;}

    public HealInfo(int value, bool critical)
    {
        HealValue = value;
        IsCritical = critical;
    }
}