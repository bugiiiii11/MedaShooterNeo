using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HungerPerk : PassivePerkBehaviour
{
    public float Amount = 0.02f;
    protected override bool CanStack(PerkBehaviour other)
    {
        return other is HungerPerk && StackCount <= 0.201f / Amount;
    }
    public override int RemainingStacks()
    {
        return Mathf.FloorToInt(0.2f / Amount);
    }

    protected override void UseEffect(PlayerStatsModule stats)
    {
        stats.SetNewMaxHp(Mathf.RoundToInt(stats.MaxHp + stats.MaxHp * Amount));
    }
}
