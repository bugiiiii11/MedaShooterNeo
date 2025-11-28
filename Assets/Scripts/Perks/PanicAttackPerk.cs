using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanicAttackPerk : ActiveTimedPerkBehaviour
{
    public int MultiplyFireRate = 3;
    public override void OnEnd()
    {
        GameManager.instance.GameConstants.FireRateMultiplier = 1;
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        GameManager.instance.GameConstants.FireRateMultiplier = 1f / MultiplyFireRate;
    }
}
