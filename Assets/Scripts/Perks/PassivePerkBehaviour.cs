using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PassivePerkBehaviour : PerkBehaviour
{
    public override bool CanRemoveStacks => false;

    private PlayerMovement _player;

    public override void OnEnd() { }

    public abstract int RemainingStacks();

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
        if (CanStack(other))
        {
            StackCount++;
            UseEffect(_player.PlayerStats);
            return true;
        }

        return false;
    }

    protected abstract bool CanStack(PerkBehaviour other);

    protected virtual void UseEffect(PlayerStatsModule stats)
    {
        /*var bef = stats.Modifiers.CriticalChanceIncreaseFromPerks;
        // take away amount used before
        stats.Modifiers.CriticalChanceIncreaseFromPerks -= (StackCount - 1) * Amount;
        // add new amount
        stats.Modifiers.CriticalChanceIncreaseFromPerks += Amount * StackCount;

        Debug.Log($"increment {bef} to {stats.Modifiers.CriticalChanceIncreaseFromPerks}");

        stats.ScaleByPerks();*/
    }
}
