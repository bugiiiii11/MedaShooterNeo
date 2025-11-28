using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriticalSituationPerk : ActiveTimedPerkBehaviour
{
    public override void OnEnd()
    {
        GameManager.instance.GameConstants.CriticalChanceProbabilityMultiplier = 1;
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        GameManager.instance.GameConstants.CriticalChanceProbabilityMultiplier = 100;
    }
}
