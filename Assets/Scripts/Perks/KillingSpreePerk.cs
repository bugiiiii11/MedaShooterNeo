using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillingSpreePerk : ActiveTimedPerkBehaviour
{
    public override void OnEnd()
    {
        GameManager.instance.GameConstants.CanInterruptKillingSpree = true;
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        GameManager.instance.GameConstants.CanInterruptKillingSpree = false;
    }
}
