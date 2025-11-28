using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    public int HitPoints = 15;
    public bool FlashOnHit = true;
    internal int currentHitPoints = 15;
    public UIGenericBar HitPointsBar;

    protected virtual void Start()
    {
        currentHitPoints = HitPoints;
    }

    public void SetHp(int value)
    {
        currentHitPoints = value;
        HitPoints = value;
    }

    public virtual void ActivateDot(DotBase dot)
    {
        if (GameManager.instance.EnemySpawner.IsBossActive())
            return;

        DamageOverTimeHandler doth;
        if (!TryGetComponent<DamageOverTimeHandler>(out doth))
        {
            doth = gameObject.AddComponent<DamageOverTimeHandler>();
            doth.Bar = HitPointsBar.transform.parent.GetComponentInChildren<UIDotBar>();
            if (!doth.Bar)
                doth.Bar = transform.GetComponentInChildren<UIDotBar>();
        }

        doth.InitializeTick(dot, this, (val) => {
            if(currentHitPoints > 0)
                ReceiveDamage(new DamageInfo(val, false));
        });
    }

    public virtual void ReceiveDamage(DamageInfo damage)
    {
        bool isInstaKill = false;
        // check for instant kill
        if (currentHitPoints >= HitPoints)
        {
            // insta kill can be achieved only when attacking enemy for the first time
            if (!GameManager.instance.EnemySpawner.IsBossActive())
            {
                var instakillChance = GameConstants.Constants.InstantKillChance;
                if (UnityEngine.Random.value < instakillChance)
                {
                    DamageTextSpawner.Spawn("<size=24>Instakill</size>", transform.position + Vector3.one * 0.3f);
                    damage.IsCritical = false;
                    damage.DamageValue = HitPoints + damage.DamageValue;
                    isInstaKill = true;
                }
            }
        }

        if (!isInstaKill)
        {
            DamageTextSpawner.Spawn(damage, transform.position + Vector3.one * 0.35f);
            CombatObserver.instance.DispatchDamage(damage.DamageValue);
        }

        currentHitPoints -= damage.DamageValue;

        if (currentHitPoints <= 0)
        {
            currentHitPoints = 0;
            OnDied();
        }

        if(HitPointsBar)
        {
            HitPointsBar.SetPercentage(currentHitPoints, maxValue: HitPoints);
        }

        if (FlashOnHit && !isInstaKill)
        {
            var avatar = GetComponent<F3DCharacterAvatar>();
            if (avatar)
            {
                avatar.TweenColor(new Color32(255, 160, 160, 255), 0.07f); 
            }
            else
            {

            }
        }
    }

    protected virtual void OnDied()
    {
        Destroy(gameObject, 1);

        var enemy = GetComponent<BasicEnemy>();

        if(enemy)
        {
            enemy.Kill();
            HitPointsBar.Hide();

            var dotBar = transform.GetComponentInChildren<UIDotBar>();
            if (dotBar)
                dotBar.Hide();
        }
    }
}
