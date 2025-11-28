using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NanoInjectionPerk : PassivePerkBehaviour
{
    public float AmountPercentage = 0.02f;

    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(0.2f / AmountPercentage);
    }

    protected override bool CanStack(PerkBehaviour other)
    {
        return other is NanoInjectionPerk && StackCount <= 0.2f / AmountPercentage;
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        var constants = GameManager.instance.GameConstants;
        
        // take away amount used before
        constants.PermanentFireRateMultiplier -= (StackCount - 1) * AmountPercentage;
        // add new amount
        constants.PermanentFireRateMultiplier += AmountPercentage * StackCount;

        constants.PermanentFireRateMultiplier = Mathf.Clamp(constants.PermanentFireRateMultiplier, 0, 0.2f);
        /*var bef = stats.Modifiers.CriticalChanceIncreaseFromPerks;
        // take away amount used before
        stats.Modifiers.CriticalChanceIncreaseFromPerks -= (StackCount - 1) * Amount;
        // add new amount
        stats.Modifiers.CriticalChanceIncreaseFromPerks += Amount * StackCount;

        Debug.Log($"increment {bef} to {stats.Modifiers.CriticalChanceIncreaseFromPerks}");

        stats.ScaleByPerks();*/
    }
}
