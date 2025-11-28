using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockAndLoadPerk : PassivePerkBehaviour
{
    public int Amount = 2;
    protected override bool CanStack(PerkBehaviour other)
    {
        return other is LockAndLoadPerk;
    }
    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(16f / Amount);
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        var constants = GameManager.instance.GameConstants;

        // take away amount used before
        constants.AdditionalWeaponPowerupDuration -= (StackCount - 1) * Amount;
        // add new amount
        constants.AdditionalWeaponPowerupDuration += Amount * StackCount;

        constants.AdditionalWeaponPowerupDuration = Mathf.RoundToInt(Mathf.Clamp(constants.AdditionalWeaponPowerupDuration, 0, 16f));
    }
}
