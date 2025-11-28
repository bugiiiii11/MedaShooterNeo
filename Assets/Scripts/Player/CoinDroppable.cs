using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinDroppable : Droppable
{
    public int CoinAddAmount = 1;

    public override void ApplyDroppable(PlayerStatsModule playerStats)
    {
        playerStats.Currencies.AddCoins(CoinAddAmount /** GameManager.instance.GameConstants.CoinDropAmountMultiplier*/);
    }
}
