using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdrenalineBoostPerk : PassivePerkBehaviour
{
    public float Amount = 0.02f;

    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(0.35f / Amount);
    }

    protected override bool CanStack(PerkBehaviour other)
    {
        return other is AdrenalineBoostPerk && StackCount <= 0.36f / Amount;
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        var constants = GameManager.instance.GameConstants;

        // take away amount used before
        constants.OverallCritChanceIncrease -= (StackCount - 1) * Amount;
        // add new amount
        constants.OverallCritChanceIncrease += Amount * StackCount;

        constants.OverallCritChanceIncrease = Mathf.Clamp(constants.OverallCritChanceIncrease, 0, 0.35f);
    }
}
