using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TripleShootingEnemy : StraightShootingEnemy
{
    public override bool CanShootFromPosition() => _transform.position.x > (LevelInfo.instance.BoundLowerLeft.position.x + LevelInfo.instance.BoundUpperRight.position.x) / 2 + 3;

    protected override void Fire()
    {
        if (GameConstants.Constants.DisarmEnemy)
            return;

        if (CanShootFromPosition())
            WeaponController.TripleFire();
    }
}
