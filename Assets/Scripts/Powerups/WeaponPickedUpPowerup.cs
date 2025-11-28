using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickedUpPowerup : MovingPowerup
{
    public string PlayerWeaponName;

    [Header("-1 until changed, otherwise time in seconds")]
    public int TimeToKeep = -1;

    public override void ApplyPowerup(PlayerStatsModule stats)
    {
        // change timetokeep
        if(TimeToKeep > 0)
        {
            TimeToKeep += GameConstants.Constants.AdditionalWeaponPowerupDuration;
        }

        var playerWeaponController = GameManager.instance.Player.GetComponent<WeaponController>();
        playerWeaponController.SetWeapon(PlayerWeaponName, TimeToKeep);

        // send event
        GameManager.instance.EventManager.Dispatch(new WeaponPowerupTimedEvent(TimeToKeep));
    }
}
