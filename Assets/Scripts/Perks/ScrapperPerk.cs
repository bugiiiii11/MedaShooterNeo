using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrapperPerk : PassivePerkBehaviour
{
    public float Amount = 0.03f;
    protected override bool CanStack(PerkBehaviour other)
    {
        return other is ScrapperPerk && StackCount <= 0.1f / Amount;
    }
    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(0.1f / Amount);
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        var constants = GameManager.instance.GameConstants;

        // take away amount used before
        constants.EnemyDropShieldChance -= (StackCount - 1) * Amount;
        // add new amount
        constants.EnemyDropShieldChance += Amount * StackCount;

        constants.EnemyDropShieldChance = Mathf.Clamp(constants.EnemyDropShieldChance, 0, 0.1f);
    }
}
