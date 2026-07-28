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

        var killed = currentHitPoints <= 0;

        if (killed)
        {
            currentHitPoints = 0;
            OnDied();
        }

        if(HitPointsBar)
        {
            HitPointsBar.SetPercentage(currentHitPoints, maxValue: HitPoints);
        }

        // Flash only a survivor. On the killing blow OnDied has already started the death fade
        // (BasicEnemy.Kill -> TweenAlpha to alpha 0) and a flash would be a second writer of
        // SpriteRenderer.color fighting it back to opaque. The kill gets its own, louder
        // feedback from the burst in BasicEnemy.Kill.
        if (FlashOnHit && !isInstaKill && !killed)
        {
            PlayHitFlash();
        }

        // Hit-stop is deliberately rare: kills and crits only, never every bullet. It is the
        // difference between weight and stutter, and every millisecond of it has to be handed
        // back to the anti-cheat duration accounting (see JuiceRuntime.StolenSeconds).
        if (killed || isInstaKill)
        {
            JuiceRuntime.RequestHitStop(JuiceSettings.HitStopKillSeconds, JuiceSettings.HitStopScale);
        }
        else if (damage.IsCritical)
        {
            JuiceRuntime.RequestHitStop(JuiceSettings.HitStopCritSeconds, JuiceSettings.HitStopScale);
        }
    }

    private HitFlash _hitFlash;
    private bool _hitFlashResolved;

    /// <summary>
    /// Runs the per-enemy flash, attaching the component on first use. Same runtime-attachment
    /// pattern this class already uses for DamageOverTimeHandler in ActivateDot.
    /// </summary>
    private void PlayHitFlash()
    {
        if (!JuiceSettings.HitFlashEnabled)
            return;

        if (!_hitFlashResolved)
        {
            _hitFlashResolved = true;

            // Flail.prefab has a BossAddDamageReceiver but no avatar, so it stays unflashed --
            // exactly as it was before Phase 1.
            if (GetComponent<F3DCharacterAvatar>() != null && !TryGetComponent(out _hitFlash))
                _hitFlash = gameObject.AddComponent<HitFlash>();
        }

        if (_hitFlash != null)
            _hitFlash.Play();
    }

    /// <summary>Stops any flash in flight so it cannot fight a death fade.</summary>
    public void StopHitFlash()
    {
        if (_hitFlash != null)
            _hitFlash.Stop();
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
