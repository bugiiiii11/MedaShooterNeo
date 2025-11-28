using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SniperEnemy : BasicEnemy, IShooter
{
    public override bool IsShooter => true;

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
    public Transform WeaponSocket;
    private float lastShootingCooldown, currentShootingCooldown;
    public float AimSpeed = 5;
    public EnemyWeaponController WeaponController;
    public bool UseAbilityBar = true;
    public UIGenericBar AbilityBar;

    public LineRenderer targetLine;
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
        if (obj.Disarmed && AbilityBar)
        {
            AbilityBar.Hide();
        }
    }
    public virtual bool CanShootFromPosition() => true;

    public override void SetParams(Enemy enemy)
    {
        base.SetParams(enemy);
        var dmg = enemy.DamageRange;
        WeaponController.GetCurrentWeapon().DamageRange = dmg;
        WeaponController.GetCurrentWeapon().Data = enemy.AdditionalAttackData;
    }

    protected override Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        target.y = _transform.position.y;
        return base.MoveTowards(current, target, maxDistanceDelta);
    }

    public override void Kill(bool withEffects = false)
    {
        base.Kill(withEffects);
        WeaponController.GetCurrentWeapon().gameObject.SetActive(false);
        WeaponController.enabled = false;
        UseAbilityBar = false;

        if(AbilityBar)
            AbilityBar.Hide();
        if (targetLine)
        {
            targetLine.enabled = false;
            targetLine.gameObject.SetActive(false);
        }
    }

    protected override void OnThinkingEvents(bool isThinking)
    {
        base.OnThinkingEvents(isThinking);

        if (GameConstants.Constants.DisarmEnemy)
        {
            AbilityBar.Hide();
            return;
        }

        if (Time.time - lastShootingCooldown > currentShootingCooldown)
        {
            currentShootingCooldown = ShootingCooldownRange.Random();
            lastShootingCooldown = Time.time;
            Fire();
        }
        else
        {
            WeaponController.StopFire();
            if(AbilityBar)
                AbilityBar.Hide();
        }
    }

    protected override void Update()
    {
        base.Update();

        if (GameConstants.Constants.DisarmEnemy)
        {
            if(targetLine)
                targetLine.enabled = false;
            return;
        }
        
        var playerPos = GameManager.instance.Player.transform.position + Vector3.up;
        var isInFrontOfPlayer = _transform.position.x > playerPos.x + 2;

        if (isInFrontOfPlayer && CanShootFromPosition())
        {
            if (targetLine)
            {
                targetLine.SetPosition(0, WeaponSocket.position);
                targetLine.SetPosition(1, playerPos);
                targetLine.enabled = true;
            }

            var realCooldown = Time.time - lastShootingCooldown;
            if (currentShootingCooldown > realCooldown)
            {
                var currentValue = realCooldown / currentShootingCooldown;

                if (UseAbilityBar && !GameConstants.Constants.DisarmEnemy)
                {
                    AbilityBar.Show();
                    AbilityBar.SetValue(1 - realCooldown / currentShootingCooldown);
                }

                if (targetLine)
                {
                    var col = targetLine.material.GetColor("_TintColor");
                    col.a = Mathf.Clamp01(currentValue - 0.3f) * (2 / 0.3f);
                    targetLine.material.SetColor("_TintColor", col);
                }
            }

            // Get Mouse to World Position
            playerPos.z = 0;

            AimAt(playerPos, AimSpeed);
        }
        else
        {
            currentShootingCooldown = 500;
            if (targetLine)
            {
                var col = targetLine.material.GetColor("_TintColor");
                col.a = 0;
                targetLine.material.SetColor("_TintColor", col);
            }
            if(AbilityBar)
            {
                AbilityBar.Hide();
            }

            AimAt(playerPos - Vector3.right*100, AimSpeed);
        }
    }

    private void AimAt(Vector3 aimPos, float speed)
    {
        // Look direction
        var dir = (aimPos - WeaponSocket.position).normalized;
        dir.z = 0;

        // Weapon socket to FX Socket offset
        var currentWeapon = WeaponController.GetCurrentWeapon();
        var offset = currentWeapon.FXSocket.position - WeaponSocket.position;
        offset.z = 0;
        var localOffset = WeaponSocket.InverseTransformVector(offset);
        localOffset.x = 0;
        localOffset.z = 0;
      
        //  Debug.DrawLine(WeaponSocket.position, currentWeapon.FXSocket.position, Color.yellow);
        var worldOffset = WeaponSocket.TransformVector(localOffset) - WeaponSocket.right * 5 * Mathf.Sign(dir.x);
        var weaponDir = (aimPos - (WeaponSocket.position + worldOffset)).normalized;
        var socketRotation = Quaternion.LookRotation(Vector3.forward,
            Mathf.Sign(dir.x) * Vector3.Cross(Vector3.forward, weaponDir));
        WeaponSocket.rotation = Quaternion.Lerp(WeaponSocket.rotation, socketRotation, Time.deltaTime * AimSpeed);

        // Lock Weapon Socket Angle
        var rot = WeaponSocket.rotation;
        const float z = 0.35f;

        if (WeaponSocket.rotation.z > z)
        {
            rot.z = z;
            WeaponSocket.rotation = rot;
        }
    }

    protected virtual void Fire()
    {
        WeaponController.Fire();
    }
}
