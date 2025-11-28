using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedrunnerEnemy : BasicEnemy
{
    public override bool IsShooter => false;

    public override void Kill(bool withEffects = false)
    {
        base.Kill(withEffects);
        GetComponent<WeaponController>().GetCurrentWeapon().gameObject.SetActive(false);
    }
}
