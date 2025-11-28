using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossAddDamageReceiver : DamageReceiver
{
    public delegate void AddKilled(BossAddDamageReceiver receiver);
    public event AddKilled OnBossAddKilled;
    protected override void OnDied()
    {
        OnBossAddKilled?.Invoke(this);
        GameEffectsPool.SpawnNormalExplosion(transform.position, 2);

        Destroy(gameObject);
    }

    internal void Heal()
    {
        currentHitPoints = HitPoints;
        if (HitPointsBar)
        {
            HitPointsBar.SetPercentage(currentHitPoints, maxValue: HitPoints);
        }
    }
    
    public void OnKilled(BasicEnemy enemy)
    {
        OnBossAddKilled?.Invoke(this);
    }
}
