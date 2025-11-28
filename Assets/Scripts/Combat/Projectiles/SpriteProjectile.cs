using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteProjectile : MonoBehaviour
{
    protected Rigidbody2D _rBody;
    protected Collider2D _collider;
    protected Vector3 _origin;
    public float Force;
    private int numOfReflections = 0;
    internal WeaponType FiredType;
    public GameObject SelfPrefab = null;

    [NonSerialized]
    public ProjectileData Data;

    protected virtual void Awake()
    {
        _rBody = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _origin = transform.position;
    }

    protected virtual void OnCollisionEnter2D(Collision2D other)
    {
        var contacts = new ContactPoint2D[2];
        var contactsLength = other.GetContacts(contacts);

        if (contactsLength > 0)
        {
            var contact = other.contacts[0];

            SpawnProjectileHit(other, contact);

            HandleDamage(contact.collider);

            if (_rBody != null && _collider != null)
            {
                _rBody.isKinematic = true;
                _rBody.velocity *= 0;
                _collider.enabled = false;

                _rBody.simulated = false;
            }

            // despawn
            Destroy(gameObject, 0.4f);
        }
    }

    protected virtual bool IsReflecting()
    {
        return GameManager.instance.Player.IsChainGunActive && numOfReflections < 3;
    }

    protected virtual void SpawnProjectileHit(Collision2D other, ContactPoint2D contact)
    {
        if (other.collider.CompareTag("Player") || other.collider.CompareTag("Enemy"))
        {
            // decide based on weapon type
            var toSpawn = LevelProps.instance.HitPistol;

            switch (FiredType)
            {
                case WeaponType.Pistol:
                case WeaponType.ShotgunLaser:
                case WeaponType.Unknown:
                    break;

                case WeaponType.Assault:
                case WeaponType.AssaultLaser:
                    toSpawn = LevelProps.instance.HitAssaultNormal;
                    break;

                case WeaponType.AssaultPlasma:
                    toSpawn = LevelProps.instance.HitAssaultPlasma;
                    break;

                case WeaponType.Sniper:
                    toSpawn = LevelProps.instance.HitSniper;
                    break;
            }

            var hit = Instantiate(toSpawn, (Vector3)contact.point, Quaternion.identity);
            hit.transform.SetParent(other.collider.transform);
        }
        else if(other.collider.CompareTag("Shield"))
        {
            OneShotAudioPool.SpawnOneShot(LevelProps.instance.HitShield, 0.65f);
            GameEffectsPool.SpawnShieldAbsorb(contact.point, 0.5f);
        }
    }

    protected virtual void HandleDamage(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            // it is player damaged by enemy
            var player = collider.GetComponentInParent<PlayerMovement>();
            if (player)
            {
                if (player.MirrorBullets)
                {
                    MirrorBack();
                    player.MirroredBullet();
                }
                else
                {
                    player.ReceiveDamage(Data.ComputeDamage().DamageValue);
                }
            }

            return;
        }

        // it is enemy damaged by player
        var receiver = collider.GetComponent<DamageReceiver>();

        if (receiver)
        {
            receiver.ReceiveDamage(Data.ComputeDamage());

            if(GameManager.instance.Player.IsChainGunActive && numOfReflections < 3)
                ReflectToNextEnemy(collider);

            if (GameManager.instance.Player.IsDeepWoundActive)
            {
                receiver.ActivateDot(LevelProps.instance.DeepWoundScriptable);
            }
        }
    }

    protected void ReflectToNextEnemy(Collider2D myEnemy)
    {
        Transform nextEnemy = null;
        // find next enemy
        var enemyList = new List<Transform>();
        foreach (Transform enemy in GameManager.instance.EnemySpawner.AllEnemies)
        {
            if (enemy && enemy != myEnemy.transform && enemy.TryGetComponent<DamageReceiver>(out var receiver) && receiver.currentHitPoints > 0)
            {
                enemyList.Add(enemy);
            }
        }

        if (enemyList.Count > 0)
            nextEnemy = enemyList.Random();

        if (!nextEnemy)
            return;

        var tr = transform;
        var projectile = Instantiate(SelfPrefab, tr.position, tr.rotation).transform;

        // Set Attributes
        var projectileObject = projectile.GetComponent<SpriteProjectile>();
        projectileObject.Data = Data;
        projectileObject.FiredType = FiredType;
        projectileObject.SelfPrefab = SelfPrefab;
        projectileObject.Data.BaseDamage = Mathf.RoundToInt(this.Data.BaseDamage * 0.85f);
        projectileObject.numOfReflections = numOfReflections + 1;
        projectileObject.Force = Force * 0.8f;

        projectile.localScale = transform.localScale;
        var projRb = projectile.GetComponent<Rigidbody2D>();
        var projCol = projectile.GetComponent<Collider2D>();

        Physics2D.IgnoreCollision(myEnemy, projCol);
        Physics2D.IgnoreCollision(GameManager.instance.Player.GetComponentInChildren<Collider2D>(), projCol);

        // predict target's position if moving left
        var nextPosition = nextEnemy.position;

        // slightly offset to left if enemy is on the left side from this position
        var dirToLeft = nextPosition - tr.position;
        var offsetAmountX = 0f;

        if(dirToLeft.x < 0)
        {
            offsetAmountX = Mathf.Abs(dirToLeft.x) * 0.5f; // seems that half the X distance is enough 
        }

        Vector3 diff = (nextPosition + (Vector3.up * 0.4f) + (Vector3.left * offsetAmountX)) - tr.position;
        diff.Normalize();

        projRb.AddForce((Vector2)diff.normalized * Force, ForceMode2D.Force);

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        projectile.rotation = Quaternion.Euler(0f, 0f, rot_z);
    }

    private void MirrorBack()
    {
        if (!SelfPrefab)
            return;

        var tr = transform;
        var projectile = Instantiate(SelfPrefab, tr.position, tr.rotation).transform;
        projectile.gameObject.layer = LayerMask.NameToLayer("Projectile");
        // Set Attributes
        var projectileObject = projectile.GetComponent<SpriteProjectile>();
        projectileObject.Data = Data;
        projectileObject.FiredType = FiredType;
        projectileObject.SelfPrefab = SelfPrefab;
        projectileObject.Data.BaseDamage = Mathf.RoundToInt(this.Data.BaseDamage * 0.85f);
        projectileObject.numOfReflections = numOfReflections + 1;
        projectileObject.Force = Force * 0.8f;

        projectile.localScale = transform.localScale;
        var projRb = projectile.GetComponent<Rigidbody2D>();
        var projCol = projectile.GetComponent<Collider2D>();

        Physics2D.IgnoreCollision(GameManager.instance.Player.GetComponentInChildren<Collider2D>(), projCol);

        // predict target's position if moving left
        var nextPosition = ((Vector3)UnityEngine.Random.insideUnitCircle * 2) + Vector3.right * 7;

        Vector3 diff = (nextPosition + (Vector3.up * 0.4f)) - tr.position;
        diff.Normalize();

        projRb.AddForce((Vector2)diff.normalized * Force, ForceMode2D.Force);

        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        projectile.rotation = Quaternion.Euler(0f, 0f, rot_z);
    }
}
