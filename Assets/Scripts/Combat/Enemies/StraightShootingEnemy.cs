using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class StraightShootingEnemy : BasicEnemy, IShooter
{
    public override bool IsShooter => true;
    public UIGenericBar AbilityBar;

    public Vector2 ShootingCooldown
    {
        get
        {
            return ShootingCooldownRange;
        }
        set
        {
            ShootingCooldownRange = value;
        }
    }

    public Vector2 ShootingCooldownRange;

    private float lastShootingCooldown, currentShootingCooldown;
    public EnemyWeaponController WeaponController;

    protected override void Start() 
    {
        base.Start();
        lastShootingCooldown = Time.time;
        currentShootingCooldown = ShootingCooldownRange.Random();

        if (GameConstants.Constants.DisarmEnemy && AbilityBar)
        {
            AbilityBar.Hide();
        }
    }

    protected override void ResetTimers()
    {
        base.ResetTimers();
        lastShootingCooldown = Time.time;
        currentShootingCooldown = ShootingCooldownRange.Random();
    }

    protected override void OnEnemyDisarmed(EnemyDisarmedEvent obj)
    {
        base.OnEnemyDisarmed(obj);
        if(obj.Disarmed && AbilityBar)
        {
            AbilityBar.Hide();
        }
    }

    public override void SetParams(Enemy enemy)
    {
        base.SetParams(enemy);
        var dmg = enemy.DamageRange;
        WeaponController.GetCurrentWeapon().DamageRange = dmg;
        WeaponController.GetCurrentWeapon().Data = enemy.AdditionalAttackData;
    }

    public override void Kill(bool withEffects = false)
    {
        base.Kill(withEffects);
        WeaponController.GetCurrentWeapon().gameObject.SetActive(false);
        WeaponController.enabled = false;

        if (AbilityBar)
            AbilityBar.Hide();
    }

    public virtual bool CanShootFromPosition() => _transform.position.x >= -3f;

    protected override void OnThinkingEvents(bool isThinking)
    {
        base.OnThinkingEvents(isThinking);

        if (GameConstants.Constants.DisarmEnemy)
        {
            AbilityBar.Hide();
            return;
        }

        if (CanShootFromPosition() && Time.time - lastShootingCooldown >= currentShootingCooldown)
        {
            currentShootingCooldown = ShootingCooldownRange.Random();
            lastShootingCooldown = Time.time;
            Fire();
        }
        else
        {
            WeaponController.StopFire();
            if (AbilityBar)
                AbilityBar.Hide();
        }
    }

    protected virtual void Fire()
    {
        WeaponController.Fire();
    }

    protected override void Update()
    {
        base.Update();
        var realCooldown = Time.time - lastShootingCooldown;
        if (currentShootingCooldown > realCooldown)
        {
            if (AbilityBar && !GameConstants.Constants.DisarmEnemy)
            {
                if (!IsDead && CanShootFromPosition())
                    AbilityBar.Show();

                AbilityBar.SetValue(1 - realCooldown / currentShootingCooldown);
            }
        }
    }
}
