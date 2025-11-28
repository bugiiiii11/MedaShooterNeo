using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlailBombRunnerEnemy : SpeedrunnerEnemy
{

    public FlailBoss BossParent;
    public float PlayerCoordinateX = 0;

    protected override Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        target.y = _transform.position.y;
        target.x = LevelInfo.instance.BoundLowerLeft.position.x;
        return base.MoveTowards(current, target, maxDistanceDelta);
    }

    protected override void Update()
    {
        base.Update();
        if(_transform.position.x <= PlayerCoordinateX)
        {
            // damage player and send boss info
            BossParent.ReportBombRunnerNotKilled(this);
            ExplodeOnMelee();
        }
    }

    public override void Kill(bool withEffects = false)
    {
        base.Kill(withEffects);
        GetComponent<BossAddDamageReceiver>().OnKilled(this);
    }
}
