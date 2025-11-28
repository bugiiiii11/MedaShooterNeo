using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyInjectionPerk : PassivePerkBehaviour
{
    public int Amount = 2;
    protected override bool CanStack(PerkBehaviour other)
    {
        return other is EnergyInjectionPerk && StackCount <= 20f / Amount;
    }
    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(20f / Amount);
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        var constants = GameManager.instance.GameConstants;

        // take away amount used before
        constants.UltimateCooldownReduction -= (StackCount - 1) * Amount;
        // add new amount
        constants.UltimateCooldownReduction += Amount * StackCount;

        constants.UltimateCooldownReduction = Mathf.RoundToInt(Mathf.Clamp(constants.UltimateCooldownReduction, 0, 20f));
    }
}
