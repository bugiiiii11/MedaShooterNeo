using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldPowerup : MovingPowerup
{
    [Range(0,1)]
    public float ArmorAddAmount = 1;

    public override void ApplyPowerup(PlayerStatsModule stats)
    {
        GameManager.instance.GameStats.ShieldsCollected++;
        stats.AddArmor(Mathf.RoundToInt(ArmorAddAmount * stats.MaxArmor));
    }
}