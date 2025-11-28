using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackingFlail : MonoBehaviour
{
    public Vector2 AttackCooldownRange;
    public Vector2 DamageRange;

    public float ProjectileForce = 2000;
    
    public TimedVariable currentAttack;
    public LayerMask ProjectileHitLayerMask;
    public Transform FXSocket;

    public GameObject Projectile;

    [SerializeField]
    private Animator animator;

    private void Start()
    {
        animator.SetFloat("offset", UnityEngine.Random.Range(0, 2f));
        currentAttack = new TimedVariable();
        currentAttack.CurrentValue = AttackCooldownRange.Random();
    }

    private void Update()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        if(Time.time - currentAttack.LastTime > currentAttack.CurrentValue)
        {
            currentAttack.LastTime = Time.time;
            currentAttack.CurrentValue = AttackCooldownRange.Random();

            Fire();
        }
    }

    private void Fire()
    {
        animator.SetTrigger("attack");
    }

    public void OnAttackAnimationBegin()
    {
        SpawnProjectile(Projectile);
    }

    protected void SpawnProjectile(GameObject projectilePrefab)
    {
        // Direction
        var _dir = -Mathf.Sign(FXSocket.parent.lossyScale.x);

        // Run close distance check from the bone pivot to the FXSocket twice long

        var closeCheckHit = Physics2D.LinecastAll(FXSocket.position - FXSocket.right * _dir,
            FXSocket.position + FXSocket.right * 0 * _dir, ProjectileHitLayerMask);

        // Keep the initial position, rotaion of the FXSocket so the projectile is launched in the correct _dir
        // Random Offset
        // Position
        var position = FXSocket.position;

        // Rotation
        var rotation = FXSocket.rotation;
        if (_dir < 0)
            rotation *= Quaternion.Euler(0, 0, 180);
        rotation *= Quaternion.Euler(Vector3.zero);

        // Spawn Delayed
        StartCoroutine(SpawnMultiProjectileDelayed(projectilePrefab, position, rotation, 0));
    }

    private IEnumerator SpawnMultiProjectileDelayed(GameObject projectilePrefab, Vector2 position, Quaternion rotation, float offset)
    {
        if (!projectilePrefab) yield break;

        var playerStats = GameManager.instance.Player.PlayerStats;

        // Lifetime
        var lifeTime = UnityEngine.Random.Range(3, 4);

        // Spawn
        var projectile = Instantiate(projectilePrefab, position, rotation).transform; //F3DSpawner.Spawn(projectilePrefab, position, rotation, null);

        // Set Attributes
        var projectileObject = projectile.GetComponent<SpriteProjectile>();

        projectileObject.Data.BaseDamage = Mathf.RoundToInt(DamageRange.Random());
        // Set AudioInfo
        //  projectileObject.AudioInfo = AudioInfo;

        // Scale
        var scale = projectile.localScale;
        projectile.localScale = scale;
        var projRb = projectile.GetComponent<Rigidbody2D>();

        var playerPosition = GameManager.instance.Player.transform.position;
        playerPosition.y += UnityEngine.Random.Range(0.3f, 1);
        var dirToPlayer = (playerPosition - FXSocket.position).normalized;

        // Launch  
        // var forceRandom = Random.Range(ProjectileForce.x, ProjectileForce.y);
        projRb.AddForce((((Vector2)dirToPlayer * ProjectileForce) + new Vector2(0, offset)), ForceMode2D.Force);
        projRb.RotateToVelocity();
    }
}

public class TimedVariable
{
    public float LastTime;
    public float CurrentValue;
}