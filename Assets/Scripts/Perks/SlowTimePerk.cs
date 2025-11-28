using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlowTimePerk : ActiveTimedPerkBehaviour
{
    public override void OnEnd()
    {
        GameManager.instance.GameConstants.EnemyProjectileSpeedMultiplier = 1f;
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        GameManager.instance.GameConstants.EnemyProjectileSpeedMultiplier = 0.7f;
    }
}
