using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ActivePerkBehaviour : PerkBehaviour
{
    public override bool CanRemoveStacks => false;

    public override bool RemoveStack()
    {
        // this is active perk, so no stacking
        return false;
    }

    public override bool Stack(PerkBehaviour other)
    {
        // this is active perk, so no stacking, just re-activating
        return false;
    }
}