using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;

public class UIStats : MonoBehaviour
{
    public TextMeshProUGUI StatsNumbersText;
    public PlayerBaseStats PlayerBaseStats;

    private void Start()
    {
        RefreshStats();
    }

    /// <summary>
    /// Refreshes the stats display. Call this when boost status changes.
    /// </summary>
    public void RefreshStats()
    {
        WeaponBoostedStats wbs = null;
        HeroBoostedStats hbs = null;

        if (PlayerProfileInfo.instance.EquippedWeapon != null)
            wbs = (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();

        if (PlayerProfileInfo.instance.EquippedHero != null)
            hbs = (PlayerProfileInfo.instance.EquippedHero as NftHero)?.ConvertToBoostedStats();

        UpdateStats(hbs, wbs);
    }

    public void UpdateStats(HeroBoostedStats stats, WeaponBoostedStats weapStats)
    {
        var sb = new StringBuilder();
        AbilityConfig heroAbility = null;

        var coeffForBoss = 1.4f;
        var damageX = PlayerBaseStats.Damage.x * coeffForBoss;
        var damageY = PlayerBaseStats.Damage.y * coeffForBoss;
        float value = 0;
        if (stats != null && PlayerProfileInfo.instance.EquippedHero != null)
            heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbilityForFraction((PlayerProfileInfo.instance.EquippedHero as NftHero).Fraction);

        // Get combat boost effects if active
        BoostEffects boostEffects = null;
        bool hasActiveBoost = PlayerProfileInfo.instance != null && PlayerProfileInfo.instance.HasActiveCombatBoost;
        if (hasActiveBoost)
        {
            boostEffects = PlayerProfileInfo.instance.CombatBoostEffects;
            Debug.Log($"💊 UIStats: Combat boost is active, applying to display");
        }

        if (PlayerProfileInfo.instance.EquippedHero != null && PlayerProfileInfo.instance.EquippedWeapon != null)
        {
            stats.Join(weapStats);
            weapStats.Join(stats);
        }

        if (PlayerProfileInfo.instance.EquippedHero != null)
        {
            value = Mathf.RoundToInt((stats.MaxHealthFactor - 1) * PlayerBaseStats.Health);
            if(value == 0)
                sb.Append(PlayerBaseStats.Health + value).Append("\n");
            else
                sb.Append("<color=green>").Append(PlayerBaseStats.Health + value).Append("</color>\n");

            value = stats.ShieldAddition;
            if (value == 0)
                sb.Append(PlayerBaseStats.Shield + value).Append("\n");
            else
                sb.Append("<color=green>").Append(PlayerBaseStats.Shield + value).Append("</color>\n");

            value = (heroAbility.Cooldown - Mathf.RoundToInt(stats.CooldownReductionFactor * heroAbility.Cooldown));
            
            if (value == 0)
                sb.Append(heroAbility.Cooldown - value).Append("s\n");
            else
                sb.Append("<color=green>").Append(heroAbility.Cooldown - value).Append("s</color>\n");

            value = (float) System.Math.Round((stats.PlayerSpeedFactor * PlayerBaseStats.MovementSpeed) - PlayerBaseStats.MovementSpeed, 2);

            if (value == 0)
                sb.Append(System.Math.Round(PlayerBaseStats.MovementSpeed + value, 2)).Append("\n");
            else
                sb.Append("<color=green>").Append(System.Math.Round(PlayerBaseStats.MovementSpeed + value, 2)).Append("</color>\n");
        }
        else
        {
            sb.Append(PlayerBaseStats.Health).Append("\n")
            .Append(PlayerBaseStats.Shield).Append("\n")
            .Append("-\n")
            .Append(3.3f).Append("\n");
        }

        if (PlayerProfileInfo.instance.EquippedWeapon != null)
        {
            // === DAMAGE ===
            float baseDamage = PlayerBaseStats.Damage.y * 1.4f;
            float weaponDamageBonus = baseDamage * weapStats.DamageFactor;
            float totalDamage = baseDamage + weaponDamageBonus;

            // Apply combat boost to damage (multiply by damageMultiplier)
            if (hasActiveBoost && boostEffects != null)
            {
                totalDamage *= boostEffects.DamageMultiplier;
            }

            value = totalDamage - baseDamage;
            if (value < 0.2f)
                sb.Append(Mathf.CeilToInt(totalDamage)).Append("\n");
            else
                sb.Append("<color=green>").Append(Mathf.CeilToInt(totalDamage)).Append("</color>\n");

            // === CRITICAL CHANCE ===
            float baseCrit = PlayerBaseStats.CritChancePerc;
            float weaponCritBonus = weapStats.CriticalChanceIncrease;
            float totalCrit = baseCrit + weaponCritBonus;

            // Apply combat boost to crit (add critBonus)
            if (hasActiveBoost && boostEffects != null)
            {
                totalCrit += boostEffects.CritBonus;
            }

            value = totalCrit - baseCrit;
            if (value < 0.001f)
                sb.Append(Mathf.RoundToInt(totalCrit * 100)).Append("%\n");
            else
                sb.Append("<color=green>").Append(Mathf.RoundToInt(totalCrit * 100)).Append("%</color>\n");

            // === FIRE RATE ===
            float baseFireRateValue = 60f / PlayerBaseStats.FireRatePerc;
            float weaponFireRate = 60f / (PlayerBaseStats.FireRatePerc - weapStats.FireRateIncrease);
            float totalFireRate = weaponFireRate;

            // Apply combat boost to fire rate (multiply by fireRateMultiplier)
            if (hasActiveBoost && boostEffects != null)
            {
                totalFireRate *= boostEffects.FireRateMultiplier;
            }

            value = totalFireRate - baseFireRateValue;
            if (value < 0.2f)
                sb.Append(Mathf.RoundToInt(totalFireRate)).Append("\n");
            else
                sb.Append("<color=green>").Append(Mathf.RoundToInt(totalFireRate)).Append("</color>\n");


            //sb.Append(Mathf.RoundToInt(damageX)).Append("-").Append(Mathf.RoundToInt(damageY)).Append("<color=green> + ").Append(Mathf.CeilToInt(damageY * weapStats.DamageFactor)).Append("</color>\n")//#############
            //.Append(PlayerBaseStats.CritChancePerc * 100).Append("%").Append("<color=green> + ").Append(Mathf.CeilToInt(weapStats.CriticalChanceIncrease * 100)).Append("%</color>\n")//#############
            ////.Append(PlayerBaseStats.CritDamage.x).Append("-").Append(PlayerBaseStats.CritDamage.y).Append("\n")
            ////.Append("45s").Append(Mathf.RoundToInt(showedNft.ConvertToBoostedStats().CooldownReductionFactor * heroAbility.Cooldown))
            //.Append(Mathf.RoundToInt(60f/PlayerBaseStats.FireRatePerc)).Append("<color=green> + ").Append(Mathf.RoundToInt(60f / (PlayerBaseStats.FireRatePerc-weapStats.FireRateIncrease))- Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc) ).Append("</color>\n");
        }
        else
        {
            // No equipped weapon - show base stats with combat boost if active
            float baseDamage = Mathf.RoundToInt(damageY);
            float baseCrit = PlayerBaseStats.CritChancePerc;
            float baseFireRate = 60f / PlayerBaseStats.FireRatePerc;

            // Apply combat boost if active
            if (hasActiveBoost && boostEffects != null)
            {
                float boostedDamage = baseDamage * boostEffects.DamageMultiplier;
                float boostedCrit = baseCrit + boostEffects.CritBonus;
                float boostedFireRate = baseFireRate * boostEffects.FireRateMultiplier;

                sb.Append("<color=green>").Append(Mathf.RoundToInt(boostedDamage)).Append("</color>\n");
                sb.Append("<color=green>").Append(Mathf.RoundToInt(boostedCrit * 100)).Append("%</color>\n");
                sb.Append("<color=green>").Append(Mathf.RoundToInt(boostedFireRate)).Append("</color>\n");
            }
            else
            {
                // No boost - show base stats
                sb.Append(Mathf.RoundToInt(baseDamage)).Append("\n");
                sb.Append(Mathf.RoundToInt(baseCrit * 100)).Append("%\n");
                sb.Append(Mathf.RoundToInt(baseFireRate)).Append("\n");
            }
        }

        StatsNumbersText.text = sb.ToString();
    }
}
