using CodeStage.AntiCheat.ObscuredTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NftWeapon : INft
{
    public ObscuredInt Security;

    public ObscuredInt Anonymity;

    public ObscuredInt Innovation;

    public ObscuredInt Power;
    public string OptionalDescription = "";

    public string OwnerWallet { get; set; }

    [SerializeField]
    private string name;
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private Sprite visualization;
    public Sprite Visualization { get => visualization; set => visualization = value; }

    public ObscuredInt Sum() => Security + Anonymity + Innovation + Power;

    public NftWeapon(int sec, int anon, int inno, int pwr, string name)
    {
        Security = sec;
        Anonymity = anon;
        Innovation = inno;
        Power = pwr;
        Name = name;
        Visualization = UIInventory.LoadIcon(Name.ToLower());

        switch (name.ToLower())
        {
            // swords
            case "ryoshi katana":
                OptionalDescription = "Reflect any projectile. It takes 5% from current active time. Also provides high movement speed.";
                break;
            case "gladiator's greatsword":
                OptionalDescription = "Very high damage but slow attack speed. Absorbs enemy projectiles but decreases active time by 15% for each.";
                break;
            case "blessed blade":
                OptionalDescription = "Reflect any projectile. It takes 5% from current active time. Also provides high dodge for any damage.";
                break;
            case "tactician's claymore":
                OptionalDescription = "Disarm all enemies for the active time of the weapon. Provides mid damage and mid move speed.";
                break;
            // weapons

            case "viper":
                OptionalDescription = "High speed assault rifle.";
                break;
            case "adept's repeater":
                OptionalDescription = "Very high speed, close-range rifle.";
                break;
            case "underdog meda-gun":
                OptionalDescription = "Shotgun-like plasma rifle with three projectiles.";
                break;
            case "sandcrawler's sniper rifle":
                OptionalDescription = "Auto-aiming sniper rifle.";
                break;
        }
    }

    public WeaponBoostedStats ConvertToBoostedStats()
    {
        return new WeaponBoostedStats(Anonymity, Innovation, Security);
    }

    public void Boost(float multiplier)
    {
        Anonymity = ((ObscuredInt)(Anonymity + (Anonymity * multiplier)));
        Innovation = ((ObscuredInt)(Innovation + (Innovation * multiplier)));
        Security = ((ObscuredInt)(Security + (Security * multiplier)));
    }
}

public class WeaponBoostedStats
{
    public ObscuredFloat CriticalChanceIncrease;
    public ObscuredFloat DamageFactor;
    public ObscuredFloat FireRateIncrease;

    public CardAttributes ClampedAttributes;

    // anonymity
    private const float CritChanceMax = 0.025f;

    // innovation
    private const float DamageFactorMax = 0.075f;

    // security
    private const float FireRateIncreaseMax = 0.015f;

    private const int AttributeMax = 100;

    private WeaponBoostedStats() { }
    public WeaponBoostedStats(int ano, int inn, int sec)
    {
        ClampedAttributes = new CardAttributes();

        var clampedAno = Mathf.Clamp(ano, 0, AttributeMax*3);
        var clampedInn = Mathf.Clamp(inn, 0, AttributeMax*3);
        var clampedSec = Mathf.Clamp(sec, 0, AttributeMax*3);

        ClampedAttributes.Innovation = clampedInn;
        ClampedAttributes.Security = clampedSec;
        ClampedAttributes.Power = 0;
        ClampedAttributes.Anonymity = clampedAno;

        CriticalChanceIncrease = Remap(clampedAno, 0, AttributeMax, 0, CritChanceMax);
        DamageFactor = Remap(clampedInn, 0, AttributeMax, 0, DamageFactorMax);
        FireRateIncrease = Remap(clampedSec, 0, AttributeMax, 0, FireRateIncreaseMax);
    }

    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public void Join(HeroBoostedStats heroStats)
    {
        CriticalChanceIncrease = Remap(ClampedAttributes.Anonymity + heroStats.ClampedAttributes.Anonymity, 0, AttributeMax, 0, CritChanceMax);
        DamageFactor = Remap(ClampedAttributes.Innovation + heroStats.ClampedAttributes.Innovation, 0, AttributeMax, 0, DamageFactorMax);
        FireRateIncrease = Remap(ClampedAttributes.Security + heroStats.ClampedAttributes.Security, 0, AttributeMax, 0, FireRateIncreaseMax);
    }

    internal WeaponBoostedStats Clone()
    {
        var ret = new WeaponBoostedStats
        {
            CriticalChanceIncrease = this.CriticalChanceIncrease,
            DamageFactor = this.DamageFactor,
            FireRateIncrease = this.FireRateIncrease,
            ClampedAttributes = this.ClampedAttributes,
        };

        return ret;
    }
}