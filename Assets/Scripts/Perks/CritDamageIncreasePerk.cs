using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class CritDamageIncreasePerk : PerkBehaviour
{
    public int Amount;
    public override bool CanRemoveStacks => false;

    private PlayerMovement _player;

    public override void OnEnd() { }

    public override void OnInitialize(PlayerMovement player)
    {
        _player = player;
        StackCount = 1;
        UseEffect(player.PlayerStats);
    }

    // no update required
    public override void OnUpdate() { }

    // cant be removed
    public override bool RemoveStack() { return false; }

    public override bool Stack(PerkBehaviour other)
    {
        if(other is CritDamageIncreasePerk cdip)
        {
            StackCount++;
            UseEffect(_player.PlayerStats);
            return true;
        }
        return false;
    }

    private void UseEffect(PlayerStatsModule stats)
    {
        var bef = stats.Modifiers.CriticalChanceIncreaseFromPerks;
        // take away amount used before
        stats.Modifiers.CriticalChanceIncreaseFromPerks -= (StackCount -1) * Amount;
        // add new amount
        stats.Modifiers.CriticalChanceIncreaseFromPerks += Amount * StackCount;

        Debug.Log($"increment {bef} to {stats.Modifiers.CriticalChanceIncreaseFromPerks}");

        stats.ScaleByPerks();
    }
}
