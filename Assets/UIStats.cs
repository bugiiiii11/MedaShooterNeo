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
            value = PlayerBaseStats.Damage.y*1.4f* weapStats.DamageFactor;
            if (value < 0.2f)
                sb.Append(Mathf.CeilToInt(PlayerBaseStats.Damage.y*1.4f + value)).Append("\n");
            else
                sb.Append("<color=green>").Append(Mathf.CeilToInt(PlayerBaseStats.Damage.y * 1.4f + value)).Append("</color>\n");

            // crit chance
            value = weapStats.CriticalChanceIncrease;
            if (value == 0)
                sb.Append(Mathf.RoundToInt((PlayerBaseStats.CritChancePerc + value) * 100)).Append("%\n");
            else
                sb.Append("<color=green>").Append( Mathf.RoundToInt((PlayerBaseStats.CritChancePerc + value)*100)).Append("%</color>\n");

            // firerate
            value = Mathf.RoundToInt(60f / (PlayerBaseStats.FireRatePerc - weapStats.FireRateIncrease)) - Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc);
            var current = Mathf.RoundToInt(60f / (PlayerBaseStats.FireRatePerc - weapStats.FireRateIncrease)) - Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc);
            if (value == 0)
                sb.Append(Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc)+current).Append("\n");
            else
                sb.Append("<color=green>").Append(Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc)+current).Append("</color>\n");


            //sb.Append(Mathf.RoundToInt(damageX)).Append("-").Append(Mathf.RoundToInt(damageY)).Append("<color=green> + ").Append(Mathf.CeilToInt(damageY * weapStats.DamageFactor)).Append("</color>\n")//#############
            //.Append(PlayerBaseStats.CritChancePerc * 100).Append("%").Append("<color=green> + ").Append(Mathf.CeilToInt(weapStats.CriticalChanceIncrease * 100)).Append("%</color>\n")//#############
            ////.Append(PlayerBaseStats.CritDamage.x).Append("-").Append(PlayerBaseStats.CritDamage.y).Append("\n")
            ////.Append("45s").Append(Mathf.RoundToInt(showedNft.ConvertToBoostedStats().CooldownReductionFactor * heroAbility.Cooldown))
            //.Append(Mathf.RoundToInt(60f/PlayerBaseStats.FireRatePerc)).Append("<color=green> + ").Append(Mathf.RoundToInt(60f / (PlayerBaseStats.FireRatePerc-weapStats.FireRateIncrease))- Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc) ).Append("</color>\n");
        }
        else
        {
            sb.Append(Mathf.RoundToInt(damageY)).Append("\n")//#############
            .Append(PlayerBaseStats.CritChancePerc*100).Append("%\n")//#############
            .Append(Mathf.RoundToInt(60f /PlayerBaseStats.FireRatePerc)).Append("\n");
        }

        StatsNumbersText.text = sb.ToString();
    }
}
