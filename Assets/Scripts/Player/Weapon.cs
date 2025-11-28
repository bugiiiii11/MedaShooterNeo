using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour
{

    public Vector2 DamageRange;
    public bool IgnorePlayerAttributes = false;

    public WeaponType TypeOfWeapon;

    public ProjectileData Data;

    public Animator Animator;
    public SpriteRenderer LeftHand;
    public SpriteRenderer RightHand;
    public SpriteRenderer WeaponRenderer;
    public int LeftHandId;
    public int RightHandId;

    public float FireRate;

    public bool AnimationFireEvent;
    public bool AnimationReadyEvent;

    [Header("Sockets")] public Transform FXSocket;

    public Transform Projectile;

    [Header("Projectile")] 
    public float ProjectileForce;

    public float ProjectileCloseRange;

    public LayerMask ProjectileHitLayerMask;
    public float ProjectileDelay;
    public Vector2 ProjectileOffset;
    public Vector2 ProjectileRotation = new Vector2(-0.05f, 0.05f);
    public Vector2 ProjectileLifeTime = new Vector2(0.1f, 0.15f);
    public Vector2 ProjectileBaseScale = new Vector2(1f, 2f);
    public Vector2 ProjectileScaleX = new Vector2(0f, 0.5f);

    private float _dir;

    private void Awake() 
    {
        _colliders = transform.root.GetComponentsInChildren<Collider2D>();

        // try to solve weapon type if unknown
        if (TypeOfWeapon == WeaponType.Unknown)
        {
            foreach (var e in Enum.GetValues(typeof(WeaponType)))
            {
                var enumVal = (WeaponType)e;

                if (enumVal.ToString().Contains(name, StringComparison.OrdinalIgnoreCase))
                {
                    TypeOfWeapon = enumVal;
                    break;
                }
            }
        }
    }

    public virtual void OnAnimationReadyEvent()
    {
        if (!AnimationReadyEvent) return;
    }

    
    protected virtual void OnAnimationFireEvent()
    {
        if (!AnimationFireEvent) return;
        OnFire();
    }

    public virtual void Stop()
    {
        Animator.SetBool("Fire", false);
    }

    public virtual void TripleFire(int baseOffset = 600)
    {
        // Check before firing
        if (!Animator.isInitialized) return;

        Animator.SetTrigger("FireTrigger");
        Animator.SetBool("Fire", true);

        if(!AnimationFireEvent)
        {
            SpawnProjectiles(Projectile, 3, baseOffset);
        }
    }

    public virtual void RoundFire()
    {
        // Check before firing
        if (!Animator.isInitialized) return;

        Animator.SetTrigger("FireTrigger");
        Animator.SetBool("Fire", true);

        if (!AnimationFireEvent)
        {
            SpawnProjectiles(Projectile, 4, 600);
        }
    }
    
    public virtual void MissileFire(GameObject missilePrefab, float damageModifier = 1)
    {
        // Check before firing
        if (!Animator.isInitialized) return;

        // Trigger shot animator
        Animator.SetTrigger("FireTrigger");
        Animator.SetBool("Fire", true);
        if (!AnimationFireEvent)
        {
            SpawnMissile(missilePrefab, damageModifier);
        }
    }

    private void SpawnMissile(GameObject projectile, float damageModifier = 1)
    {
        // Direction
        _dir = Mathf.Sign(FXSocket.parent.lossyScale.x);

        // Position
        var position = FXSocket.position + FXSocket.right * ProjectileOffset.x * _dir;

        // Rotation
        var rotation = FXSocket.rotation;
        if (_dir < 0)
            rotation *= Quaternion.Euler(0, 0, 180);
        rotation *= Quaternion.Euler(ProjectileRotation);

        var missile = Instantiate(projectile.gameObject, position, rotation);

        var rot = missile.transform.eulerAngles;
        rot.z = Random.Range(0, 200);
        missile.transform.eulerAngles = rot;

        var projectileObject = missile.GetComponent<HomingMissile>();
        projectileObject.Data = Data;
        projectileObject.FiredType = TypeOfWeapon;
        projectileObject.Data.BaseDamage = Mathf.RoundToInt(DamageRange.Random()* damageModifier);
    }

    public virtual void Fire()
    {
        // Check before firing
        if (!Animator.isInitialized) return;

        // Trigger shot animator
        Animator.SetTrigger("FireTrigger");
        Animator.SetBool("Fire", true);
        if (!AnimationFireEvent)
            OnFire();
    }

    protected virtual void OnFire()
    {
        SpawnProjectile(Projectile);
    }

    private Collider2D[] _colliders;

    protected void SpawnProjectiles(Transform projectilePrefab, int number, int baseForceOffset = 200)
    {
        float currentProjectileOffsetDirection = -baseForceOffset*number/2;
        for(var projectileCounter = 0; projectileCounter < number; projectileCounter++)
        {
            // Direction
            _dir = Mathf.Sign(FXSocket.parent.lossyScale.x);

            // Position
            var position = FXSocket.position + FXSocket.right * ProjectileOffset.x * _dir;

            // Rotation
            var rotation = FXSocket.rotation;
            if (_dir < 0)
                rotation *= Quaternion.Euler(0, 0, 180);
            rotation *= Quaternion.Euler(ProjectileRotation);

            // Spawn Delayed
            SpawnMultiProjectileDelayed(projectilePrefab, position, rotation, currentProjectileOffsetDirection);

            currentProjectileOffsetDirection += baseForceOffset;
        }
    }

    protected void SpawnProjectile(Transform projectilePrefab)
    {
        // Direction
        _dir = Mathf.Sign(FXSocket.parent.lossyScale.x);

        // Keep the initial position, rotaion of the FXSocket so the projectile is launched in the correct _dir
        // Random Offset
        // Position
        var position = FXSocket.position + FXSocket.right * ProjectileOffset.x * _dir;

        // Rotation
        var rotation = FXSocket.rotation;
        if (_dir < 0)
            rotation *= Quaternion.Euler(0, 0, 180);
        rotation *= Quaternion.Euler(ProjectileRotation);

        // Spawn Delayed
        SpawnMultiProjectileDelayed(projectilePrefab, position, rotation, 0);
    }

    private void SpawnMultiProjectileDelayed(Transform projectilePrefab, Vector2 position, Quaternion rotation, float offset)
    {
        if (!projectilePrefab) return;
       
        var playerStats = GameManager.instance.Player.PlayerStats;

        // Lifetime
        var lifeTime = Random.Range(ProjectileLifeTime.x, ProjectileLifeTime.y);

        // Spawn
        var projectile = Instantiate(projectilePrefab.gameObject, position, rotation).transform; //F3DSpawner.Spawn(projectilePrefab, position, rotation, null);

        // Set Attributes
        var projectileObject = projectile.GetComponent<SpriteProjectile>();
        projectileObject.Data = Data;
        projectileObject.FiredType = TypeOfWeapon;
        projectileObject.SelfPrefab = projectilePrefab.gameObject;
        projectileObject.Force = ProjectileForce;

        if (!IgnorePlayerAttributes)
        {
            var baseDamageIncrement = playerStats.Modifiers.AllUpgrades.Find(x => x.WeaponType == TypeOfWeapon).DamageIncreasePerWave;
            projectileObject.Data.BaseDamage = Mathf.RoundToInt(DamageRange.Random() + Mathf.RoundToInt(baseDamageIncrement));//playerStats.Modifiers.BaseDamageIncrement;
            //projectileObject.Data.BaseDamage = DamageRange.Random() + playerStats.Modifiers.BaseDamageIncrement;
            var critBonus = new Vector2Int();
            var critX = projectileObject.Data.AdditionalData.CricitalChanceBonusDamage.x + playerStats.Modifiers.CriticalChanceIncreaseFromPerks;
            var critY = projectileObject.Data.AdditionalData.CricitalChanceBonusDamage.y + playerStats.Modifiers.CriticalChanceIncreaseFromPerks;
            critBonus.x = critX;
            critBonus.y = critY;
            projectileObject.Data.AdditionalData.CricitalChanceBonusDamage = critBonus;
        }
        else
        {
            projectileObject.Data.BaseDamage = Mathf.RoundToInt(DamageRange.Random());
        }
        // Set AudioInfo
        //  projectileObject.AudioInfo = AudioInfo;

        // Scale
        var scale = projectile.localScale * Random.Range(ProjectileBaseScale.x, ProjectileBaseScale.y);
        projectile.localScale = scale;
        var projRb = projectile.GetComponent<Rigidbody2D>();
        var collider = projectile.GetComponent<Collider2D>();

        // Ignore Self
        for (var j = 0; j < _colliders.Length; j++)
            Physics2D.IgnoreCollision(collider, _colliders[j]);

        // Launch  
        // var forceRandom = Random.Range(ProjectileForce.x, ProjectileForce.y);
        if (IgnorePlayerAttributes)
        {
            projRb.AddForce((((Vector2)projectile.right * ProjectileForce) + new Vector2((transform.right.x-1)* offset * (180-transform.parent.eulerAngles.z)/180f, (transform.right.y+1) * offset)) * GameManager.instance.GameConstants.EnemyProjectileSpeedMultiplier, ForceMode2D.Force);
        }
        else
        {
            projRb.AddForce(((Vector2)projectile.right * ProjectileForce) + new Vector2(0, offset), ForceMode2D.Force);
        }
        projRb.RotateToVelocity();
    }

    public void AimAt(Vector3 aimPos, float speed)
    {
        // Look direction
        var tr = transform;
        var dir = (aimPos - tr.position).normalized;
        dir.z = 0;

        var offset = FXSocket.position - tr.position;
        offset.z = 0;
        var localOffset = tr.InverseTransformVector(offset);
        localOffset.x = 0;
        localOffset.z = 0;

        //  Debug.DrawLine(WeaponSocket.position, currentWeapon.FXSocket.position, Color.yellow);
        var worldOffset = tr.TransformVector(localOffset) - tr.right * 5 * Mathf.Sign(dir.x);
        var weaponDir = (aimPos - (tr.position + worldOffset)).normalized;
        var socketRotation = Quaternion.LookRotation(Vector3.forward,
            Mathf.Sign(dir.x) * Vector3.Cross(Vector3.forward, weaponDir));
        tr.rotation = Quaternion.Lerp(tr.rotation, socketRotation, Time.deltaTime * speed);

        // Lock Weapon Socket Angle
        var rot = tr.rotation;
        const float z = 0.35f;

        if (tr.rotation.z > z)
        {
            rot.z = z;
            tr.rotation = rot;
        }
    }


#if UNITY_EDITOR
    [Button("Populate")]
    public void PopulateFromDuplicate()
    {
        var fd = GetComponent<F3DGenericWeapon>();
        Animator = fd.Animator;
        LeftHand = fd.LeftHand;
        RightHand = fd.RightHand;
        LeftHandId = fd.LeftHandId;
        RightHandId = fd.RightHandId;
        FireRate = fd.FireRate;
        FXSocket = fd.FXSocket;
        ProjectileForce = fd.ProjectileForce.x;
        ProjectileCloseRange = fd.ProjectileCloseRange;
        ProjectileHitLayerMask = fd.ProjectileHitLayerMask;
        ProjectileDelay = fd.ProjectileDelay;
        ProjectileOffset = fd.ProjectileOffset;
        ProjectileRotation = fd.ProjectileRotation;
        ProjectileLifeTime = fd.ProjectileLifeTime;
        ProjectileBaseScale = fd.ProjectileBaseScale;
        ProjectileScaleX = fd.ProjectileScaleX;
    }
#endif
}
