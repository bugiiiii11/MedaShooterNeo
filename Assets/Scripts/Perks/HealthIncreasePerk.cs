using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class HealthIncreasePerk : PerkBehaviour
{
    public float Amount;
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
        if(other is HealthIncreasePerk hip)
        {
            StackCount++;
            UseEffect(_player.PlayerStats);
            return true;
        }

        return false;
    }

    private void UseEffect(PlayerStatsModule stats)
    {
        // take away amount used before
        stats.Modifiers.HitPointsMultiplier -= (StackCount -1) * Amount;
        // add new amount
        stats.Modifiers.HitPointsMultiplier += Amount * StackCount;

        stats.ScaleByPerks();
        GameManager.instance.EventManager.Dispatch(new PlayerHealthChangeEvent(stats));
    }
}
