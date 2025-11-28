using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InvincibleBarrierPerk : ActiveTimedPerkBehaviour
{
    public override void OnEnd()
    {
        GameManager.instance.GameConstants.IsPlayerInvincible = false;
        GameManager.instance.Player.GetComponent<F3DCharacterAvatar>().SetColor(Color.white);
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);
        GameManager.instance.GameConstants.IsPlayerInvincible = true;
        GameManager.instance.Player.GetComponent<F3DCharacterAvatar>().SetColor(new Color32(134, 215, 255, 255));
    }
}
