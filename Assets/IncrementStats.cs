using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class IncrementStats : MonoBehaviour
{
    public TextMeshProUGUI StatsText, StatsNumbers;
    public PlayerBaseStats PlayerBaseStats;

    public HeroBoostedStats CurrentHeroStats;
    public WeaponBoostedStats CurrentWeapStats;

    public void ReportHeroEquip(HeroBoostedStats stats)
    {
        CurrentHeroStats = stats;

        if (CurrentWeapStats != null)
        {
            CurrentHeroStats.Join(CurrentWeapStats);
            CurrentWeapStats.Join(CurrentHeroStats);
        }
    }

    public void ReportWeaponEquip(WeaponBoostedStats stats)
    {
        CurrentWeapStats = stats;

        if (CurrentHeroStats != null)
        {
            CurrentHeroStats.Join(CurrentWeapStats);
            CurrentWeapStats.Join(CurrentHeroStats);
        }
    }

    public void ReportWeaponUnequip(WeaponBoostedStats stats, HeroBoostedStats heroStats)
    {
        CurrentWeapStats = null;

        if(CurrentHeroStats != null)
        {
            CurrentHeroStats = heroStats;

           // CurrentHeroStats.Unjoin(stats);
        }
    }

    public void ReportHeroUnequip(WeaponBoostedStats stats, HeroBoostedStats heroStats)
    {
        CurrentHeroStats = null;

        if (CurrentWeapStats != null)
        {
            CurrentWeapStats = stats;
        }
    }


    public void ShowEmptyHeroStats()
    {
        StatsText.text = "Armor\nMovement speed\nCooldown reduction\nMax HP increase";
        StatsNumbers.text = "-\n-\n-\n-";
    }

    public void ShowEmptyWeaponStats()
    {
        StatsText.text = "Damage\nCritical Chance\nFire rate";
        StatsNumbers.text = "-\n-\n-";
    }

    public void ShowWeaponStats(NftWeapon hero, HeroBoostedStats stats, WeaponBoostedStats weapStats)
    {
        StatsText.text = "Damage\nCritical Chance\nFire rate";
        var sb = new StringBuilder();

        var coeffForBoss = 1.4f;
        var damageX = PlayerBaseStats.Damage.x * coeffForBoss;
        var damageY = PlayerBaseStats.Damage.y * coeffForBoss;

        if (stats != null && weapStats != null)
        {
            stats.Join(weapStats);
            weapStats.Join(stats);
        }

        HeroBoostedStats usedStats = null;
        WeaponBoostedStats usedWeapStats = null;

        if (CurrentWeapStats != null)
        {
            usedWeapStats = CurrentWeapStats;
        }
        if (CurrentHeroStats != null)
        {
            usedStats = CurrentHeroStats;
        }

        // damage
        var currentDamage = usedWeapStats != null ? Mathf.RoundToInt(damageY * usedWeapStats.DamageFactor) : 0;
        var newDamage = Mathf.RoundToInt(damageY * weapStats.DamageFactor);
        var increase = newDamage - currentDamage;
        sb.Append("<sprite name=\"").Append(increase > 0 ? "stat_up" : (increase == 0 ? "stat_zero" : "stat_down")).Append("\"/>").Append(increase).Append('\n');

        // critchance
        var currentCrit = usedWeapStats != null ? Mathf.RoundToInt(usedWeapStats.CriticalChanceIncrease * 100) : 0;
        var newCrit = Mathf.RoundToInt(weapStats.CriticalChanceIncrease * 100);
        var critIncrease = newCrit - currentCrit;
        sb.Append("<sprite name=\"").Append(critIncrease > 0 ? "stat_up" : (critIncrease == 0 ? "stat_zero" : "stat_down")).Append("\"/>").Append(critIncrease).Append('\n');

        // fire rate
        var currentRate = usedWeapStats != null ? Mathf.RoundToInt(60f / (PlayerBaseStats.FireRatePerc - usedWeapStats.FireRateIncrease)) - Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc) : 0;
        var newRate = Mathf.RoundToInt(60f / (PlayerBaseStats.FireRatePerc - weapStats.FireRateIncrease)) - Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc);
        var rateIncrease = newRate - currentRate;
        sb.Append("<sprite name=\"").Append(rateIncrease > 0 ? "stat_up" : (rateIncrease == 0 ? "stat_zero" : "stat_down")).Append("\"/>").Append(rateIncrease).Append('\n');


        StatsNumbers.text = sb.ToString();
    }
    public void ShowHeroStats(NftHero hero, HeroBoostedStats stats, WeaponBoostedStats weapStats)
    {
        StatsText.text = "Armor\nMovement speed\nCooldown reduction\nMax HP increase";
        var sb = new StringBuilder();
        AbilityConfig heroAbility = null;

        var coeffForBoss = 1.4f;
        var damageX = PlayerBaseStats.Damage.x * coeffForBoss;
        var damageY = PlayerBaseStats.Damage.y * coeffForBoss;

        if (stats != null && hero != null)
            heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbilityForFraction(hero.Fraction);

        if (weapStats != null && weapStats != null)
        {
            stats.Join(weapStats);
            weapStats.Join(stats);
        }

        HeroBoostedStats usedStats = null;
        WeaponBoostedStats usedWeapStats = null;

        if(CurrentWeapStats != null)
        {
            usedWeapStats = CurrentWeapStats;
        }
        if(CurrentHeroStats != null)
        {
            usedStats = CurrentHeroStats;
        }

        // shield
        var currentShield = usedStats != null ? usedStats.ShieldAddition : (ObscuredInt)0;
        var newShield = stats.ShieldAddition;
        var increase = newShield - currentShield;
        sb.Append("<sprite name=\"").Append(increase > 0 ? "stat_up" : (increase == 0 ? "stat_zero" : "stat_down")).Append("\"/>").Append(increase).Append('\n');

        // player speed
        var currentSpeed = usedStats != null ? System.Math.Round((usedStats.PlayerSpeedFactor * PlayerBaseStats.MovementSpeed) - PlayerBaseStats.MovementSpeed, 2) : 0;
        var newSpeed = System.Math.Round((stats.PlayerSpeedFactor * PlayerBaseStats.MovementSpeed) - PlayerBaseStats.MovementSpeed, 2);
        var speedIncrease = System.Math.Round(newSpeed - currentSpeed, 2);

        sb.Append("<sprite name=\"").Append(speedIncrease > 0 ? "stat_up" : (speedIncrease == 0 ? "stat_zero" : "stat_down")).Append("\"/>").Append(speedIncrease).Append('\n');

        // cooldowns
        if (!heroAbility)
        {
            sb.Append("-\n");
        }
        else
        {
            var currentCd = usedStats != null ? heroAbility.Cooldown - Mathf.RoundToInt(usedStats.CooldownReductionFactor * heroAbility.Cooldown) : 0;
            var newCd = heroAbility.Cooldown - Mathf.RoundToInt(stats.CooldownReductionFactor * heroAbility.Cooldown);
            var cdIncrease = Mathf.CeilToInt(newCd - currentCd);

            sb.Append("<sprite name=\"").Append(cdIncrease > 0 ? "stat_up" : (cdIncrease == 0 ? "stat_zero" : "stat_down")).Append("\"/>").Append(cdIncrease).Append("s\n");
        }

        // health
        var currentHealth = usedStats != null ? Mathf.RoundToInt((usedStats.MaxHealthFactor - 1) * PlayerBaseStats.Health) : 0;
        var newHealth = Mathf.RoundToInt((stats.MaxHealthFactor - 1) * PlayerBaseStats.Health);
        var healthIncrease = newHealth - currentHealth;

        sb.Append("<sprite name=\"").Append(healthIncrease > 0 ? "stat_up" : (healthIncrease == 0 ? "stat_zero" : "stat_down")).Append("\"/>").Append(healthIncrease).Append("\n");


        /*
        sb.Append(PlayerBaseStats.Health).Append("<color=green> + ").Append(Mathf.RoundToInt((usedStats.MaxHealthFactor - 1) * PlayerBaseStats.Health)).Append("</color>\n");
        */
        StatsNumbers.text = sb.ToString();
    }
}
