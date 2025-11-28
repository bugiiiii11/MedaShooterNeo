using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstaKillPerk : PassivePerkBehaviour
{
    public float Amount = 0.03f;
    protected override bool CanStack(PerkBehaviour other)
    {
        return other is InstaKillPerk && StackCount <= 0.201f / Amount;
    }
    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(0.2f / Amount);
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        var constants = GameManager.instance.GameConstants;

        // take away amount used before
        constants.InstantKillChance -= (StackCount - 1) * Amount;
        // add new amount
        constants.InstantKillChance += Amount * StackCount;

        constants.InstantKillChance = Mathf.Clamp(constants.InstantKillChance, 0, 0.2f);
    }
}
