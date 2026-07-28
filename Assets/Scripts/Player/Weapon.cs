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

    // ---------------------------------------------------------------------
    // Muzzle flash (GDD 3.2 item 5)
    //
    // The effects come from Assets/FORGE3D/2D Sci-Fi Platformer/Resources/Effects/MuzzleFlash/,
    // which is a Resources root -- so they load by path with no serialized reference, no prefab
    // edit and no scene edit. They already ship inside the WebGL build (Resources folders are
    // always included) and nothing has ever spawned them: F3DGenericWeapon's SpawnMuzzleFlash
    // call sits inside a commented-out block, and this Weapon class, which replaced it, never had
    // a muzzle-flash field at all. They also draw on a dedicated "MuzzleFlash" sorting layer that
    // still exists in TagManager, so they need no sorting configuration.
    // ---------------------------------------------------------------------

    private const string MuzzleFlashResourceRoot = "Effects/MuzzleFlash/MuzzleFlash_";

    private static GameObject[] _muzzleFlashCache;
    private static bool[] _muzzleFlashResolved;

    /// <summary>Scratch buffer for DampenGlow, so per-shot tuning allocates nothing.</summary>
    private static readonly List<ParticleSystem> _flashSystems = new List<ParticleSystem>(4);

    private bool _isEnemyWeapon;

    /// <summary>
    /// This weapon's one muzzle-flash instance, reused for every shot it ever fires (S205).
    ///
    /// Held per weapon rather than per weapon TYPE because the flash is parented to FXSocket so it
    /// rides the recoil animation -- two enemies firing the same gun need their own. It is a child
    /// of the socket, so it dies with the weapon and needs no cleanup path of its own.
    /// </summary>
    private GameObject _muzzleFlashInstance;

    private void Awake()
    {
        _colliders = transform.root.GetComponentsInChildren<Collider2D>();

        // EnemyWeaponController derives from WeaponController and drives the same Fire methods,
        // so without this every enemy would emit muzzle flashes at the same cost as the player.
        _isEnemyWeapon = GetComponentInParent<EnemyWeaponController>() != null;

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

    /// <summary>
    /// Resolves (and caches) the flash prefab for a weapon type. Indexed by the enum value rather
    /// than held in a Dictionary so the lookup is allocation-free on the hot fire path.
    /// </summary>
    private static GameObject GetMuzzleFlashPrefab(WeaponType type)
    {
        if (_muzzleFlashCache == null)
        {
            // Enum.GetValues is reflection and allocates -- it must run once, not on every shot.
            var count = System.Enum.GetValues(typeof(WeaponType)).Length;
            _muzzleFlashCache = new GameObject[count];
            _muzzleFlashResolved = new bool[count];
        }

        var index = (int)type;

        if (index < 0 || index >= _muzzleFlashCache.Length)
            return null;

        if (_muzzleFlashResolved[index])
            return _muzzleFlashCache[index];

        _muzzleFlashResolved[index] = true;

        string variant;
        switch (type)
        {
            case WeaponType.Pistol: variant = "Pistol"; break;
            case WeaponType.Assault: variant = "Assault"; break;
            case WeaponType.AssaultPlasma: variant = "AssaultPlasma"; break;
            case WeaponType.AssaultLaser: variant = "AssaultLaser"; break;
            case WeaponType.ShotgunLaser: variant = "Shotgun"; break;

            // The FORGE3D library has no sniper flash; the machinegun one is the closest read.
            case WeaponType.Sniper: variant = "Machinegun"; break;

            // Swords and unresolved types get nothing.
            default: variant = null; break;
        }

        _muzzleFlashCache[index] = variant == null
            ? null
            : Resources.Load<GameObject>(MuzzleFlashResourceRoot + variant);

        return _muzzleFlashCache[index];
    }

    /// <summary>
    /// Plays a muzzle flash at the barrel. Parented to FXSocket so it rides the weapon's recoil
    /// animation, which is the whole reason it reads as a gunshot rather than a decal.
    ///
    /// Called from Fire/TripleFire/RoundFire/MissileFire rather than from the shared
    /// SpawnMultiProjectileDelayed tail, because that tail runs once per PROJECTILE -- it would
    /// fire three flashes for a shotgun volley and four for a round-fire enemy.
    /// MeleeWeapon overrides all four of those methods with empty bodies, so swords are excluded
    /// for free.
    ///
    /// S205 -- this used to Instantiate a fresh flash and Destroy it 0.32s later, ON EVERY SHOT,
    /// for every weapon in the scene. That was the heaviest recurring allocation in the game and
    /// the best candidate for the founder's intermittent "control movement delay" at wave 36:
    ///
    ///  - the player's fire cooldown starts at 0.65s and per-wave upgrades plus NFT boosts drive
    ///    it toward 0.1s, so the rate GROWS through a run -- matching "sometimes";
    ///  - enemy weapons are only skipped on the Low preset, and DetectQuality returns High for
    ///    every WebGL client, so on the shipping platform each enemy shot allocated one too. Late
    ///    waves put many shooters on screen at once;
    ///  - each instance is a multi-ParticleSystem prefab, and WebGL runs a single-threaded
    ///    collector, so the reclaim lands as a frame-time spike rather than background work.
    ///    A spike in a frame is felt as input lag, because Update samples SimpleInput once a frame.
    ///
    /// Every system under Resources/Effects/MuzzleFlash is looping: 0 (verified across the whole
    /// folder), so a retained instance emits its burst and then sits idle and invisible -- which
    /// is what makes reuse possible here and is why the flash is replayed rather than respawned.
    /// The old Destroy timer was the only thing MuzzleFlashSeconds governed; the prefabs' own
    /// particle lifetimes have always been what the player actually sees.
    /// </summary>
    protected void SpawnMuzzleFlash()
    {
        if (!JuiceSettings.MuzzleFlashEnabled || FXSocket == null || FXSocket.parent == null)
            return;

        if (_isEnemyWeapon && !JuiceSettings.IsHigh)
            return;

        // Same direction/rotation maths the projectile spawn paths use: FXSocket's parent carries
        // a negative lossyScale.x when the character faces left, and the flash has to be flipped
        // with it. Recomputed per shot -- the character can turn between two shots.
        var dir = Mathf.Sign(FXSocket.parent.lossyScale.x);
        var position = FXSocket.position + FXSocket.right * ProjectileOffset.x * dir;
        var rotation = FXSocket.rotation;

        if (dir < 0)
            rotation *= Quaternion.Euler(0, 0, 180);

        if (_muzzleFlashInstance == null)
        {
            var prefab = GetMuzzleFlashPrefab(TypeOfWeapon);
            if (prefab == null)
                return;

            // These prefabs have stopAction None, so they never clean themselves up -- which is
            // exactly the behaviour being relied on now.
            _muzzleFlashInstance = Instantiate(prefab, position, rotation, FXSocket);

            // Once per instance, never per shot: DampenGlow MULTIPLIES startSize and alpha, so
            // re-running it on a reused flash would compound 0.4x per shot and the pistol's glow
            // would vanish within a few rounds. Safe at this point in the frame -- playOnAwake has
            // started the system, but a ParticleSystem does not emit until its own simulation
            // step, which runs after Update.
            if (TypeOfWeapon == WeaponType.Pistol)
                DampenGlow(_muzzleFlashInstance, JuiceSettings.PistolGlowSizeScale, JuiceSettings.PistolGlowAlphaScale);

            return;
        }

        _muzzleFlashInstance.transform.SetPositionAndRotation(position, rotation);
        ReplayFlash(_muzzleFlashInstance);
    }

    /// <summary>
    /// Restarts a retained flash's burst.
    ///
    /// Clear before Play so a shot fired while the previous burst is still fading starts clean
    /// instead of stacking particles -- at a 0.1s cooldown against particle lifetimes several
    /// times that, overlap is the normal case, not the edge case.
    ///
    /// withChildren: false on both calls, because the systems are collected as a flat list here;
    /// letting each call recurse would re-clear and re-play every descendant once per level of
    /// nesting.
    /// </summary>
    private static void ReplayFlash(GameObject flash)
    {
        // Same non-allocating overload and same scratch list as DampenGlow -- this runs on the
        // hot fire path, and the two never overlap (DampenGlow runs only on the creation branch,
        // which returns before reaching here).
        flash.GetComponentsInChildren(true, _flashSystems);

        for (var i = 0; i < _flashSystems.Count; i++)
        {
            var ps = _flashSystems[i];
            ps.Clear(false);
            ps.Play(false);
        }

        // Leaving the list populated would keep this flash's components referenced until the next
        // shot, and hand DampenGlow a stale buffer if a weapon is created later.
        _flashSystems.Clear();
    }

    /// <summary>
    /// Shrinks and dims the wide additive halo inside a muzzle flash, leaving the sharp flash
    /// sprite untouched (GDD 3.2, S203).
    ///
    /// Matched by name because every FORGE3D flash names that child MuzzleFlashGlow_* -- and
    /// Instantiate renames only the ROOT to "(Clone)", so children keep their authored names.
    /// Tuned per instance rather than on the prefab: Resources.Load hands back the shared asset,
    /// and mutating that would dirty the real file in the Editor.
    /// </summary>
    private static void DampenGlow(GameObject flash, float sizeScale, float alphaScale)
    {
        // Non-allocating overload against a reused list -- this runs on the fire path.
        flash.GetComponentsInChildren(true, _flashSystems);

        for (var i = 0; i < _flashSystems.Count; i++)
        {
            var ps = _flashSystems[i];

            if (ps.name.IndexOf("Glow", StringComparison.Ordinal) < 0)
                continue;

            var main = ps.main;

            var size = main.startSize;
            switch (size.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    size.constant *= sizeScale;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    size.constantMin *= sizeScale;
                    size.constantMax *= sizeScale;
                    break;
                default:
                    // Curve modes scale through the multiplier, not the keyframes.
                    size.curveMultiplier *= sizeScale;
                    break;
            }
            main.startSize = size;

            var color = main.startColor;
            switch (color.mode)
            {
                case ParticleSystemGradientMode.Color:
                    color.color = Fade(color.color, alphaScale);
                    break;
                case ParticleSystemGradientMode.TwoColors:
                    color.colorMin = Fade(color.colorMin, alphaScale);
                    color.colorMax = Fade(color.colorMax, alphaScale);
                    break;
                // Gradient modes hold alpha in gradient keys, which this struct cannot scale. No
                // FORGE3D glow uses one, and the size cut still lands if that ever changes.
            }
            main.startColor = color;
        }

        // The list is static: leaving it populated would keep the last flash's (soon destroyed)
        // components referenced until the next shot.
        _flashSystems.Clear();
    }

    /// <summary>
    /// The glow materials are on the built-in additive particle shader, where alpha scales how
    /// much light the particle adds -- so this dims rather than fades.
    /// </summary>
    private static Color Fade(Color color, float alphaScale)
    {
        color.a *= alphaScale;
        return color;
    }

    public virtual void TripleFire(int baseOffset = 600)
    {
        // Check before firing
        if (!Animator.isInitialized) return;

        Animator.SetTrigger("FireTrigger");
        Animator.SetBool("Fire", true);
        SpawnMuzzleFlash();

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
        SpawnMuzzleFlash();

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
        SpawnMuzzleFlash();

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
        SpawnMuzzleFlash();

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

        // The lifetime above was computed and then discarded for as long as this class has
        // existed, which is why a projectile that missed never despawned. SpriteProjectile
        // already applies a hard ceiling; this narrows it to what the weapon actually authored.
        projectileObject.SetMaxLifetime(lifeTime);

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
