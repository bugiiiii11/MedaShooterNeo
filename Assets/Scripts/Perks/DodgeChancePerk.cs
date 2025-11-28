using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DodgeChancePerk : PassivePerkBehaviour
{
    public float Amount = 0.03f;
    protected override bool CanStack(PerkBehaviour other)
    {
        return other is DodgeChancePerk && StackCount <= 0.21f / Amount;
    }
    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(0.2f / Amount);
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        var constants = GameManager.instance.GameConstants;

        // take away amount used before
        constants.DodgeChance -= (StackCount - 1) * Amount;
        // add new amount
        constants.DodgeChance += Amount * StackCount;

        constants.DodgeChance = Mathf.Clamp(constants.DodgeChance, 0, 0.2f);
    }
}