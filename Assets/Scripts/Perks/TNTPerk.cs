using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TNTPerk : ActiveTimedPerkBehaviour
{
    public override void OnEnd()
    {
        GameManager.instance.GameConstants.IsTntActive = false;
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        GameManager.instance.GameConstants.IsTntActive = true;
    }
}
