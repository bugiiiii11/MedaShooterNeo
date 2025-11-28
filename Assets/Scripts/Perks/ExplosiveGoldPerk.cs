using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveGoldPerk : ActiveTimedPerkBehaviour
{
    public override void OnEnd()
    {
        GameManager.instance.GameConstants.CoinDropAmountMultiplier = 1;
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        GameManager.instance.GameConstants.CoinDropAmountMultiplier = 3;
    }
}
