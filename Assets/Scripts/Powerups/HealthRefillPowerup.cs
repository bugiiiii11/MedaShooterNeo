using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class HealthRefillPowerup : MovingPowerup
{
    public bool IsPercentageUsed = false;

    [ShowIf("@IsPercentageUsed == false", true)]
    public int HpRefillAmount;

    [ShowIf("@IsPercentageUsed == true", true), Range(0, 1)]
    public float PercentageRefill;

    public override void ApplyPowerup(PlayerStatsModule stats)
    {
        int amount;
        if(IsPercentageUsed)
        {
            // compute amount to add
            amount = Mathf.CeilToInt(stats.MaxHp * PercentageRefill);
        }
        else
        {
            amount = HpRefillAmount;
        }

       /* if (GameConstants.Constants.HeartsAtMaxIncreaseHp && stats.CurrentHp == stats.MaxHp)
        {
            stats.SetNewMaxHp(Mathf.RoundToInt(stats.MaxHp + stats.MaxHp * 0.03f));
            stats.AddHp(stats.MaxHp);
            DamageTextSpawner.Spawn("<size=26><color=green>+2% max hp</color></size>", GameManager.instance.Player.transform.position + (Vector3.up * 2.5f));
        }
        else*/
        {
            stats.AddHp(amount);
            
            //show text
            if (stats.CurrentHp != stats.MaxHp)
                DamageTextSpawner.Spawn(new HealInfo(amount, false), GameManager.instance.Player.transform.position + (Vector3.up * 2.2f));
        }
    }
}
