using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeWeapon : Weapon
{
    public Vector2Int UpToDownAnimsRange;
    public Vector2Int DownToUpAnimsRange;
    public Vector2Int StaysSameRange;
    public TrailRenderer TrailRenderer;
    private bool WasLastAnimDown = false;

    private TimeVariable swooshCooldown;
    private bool ignoreCollision = false;

    public float movementSpeedAddPercentage = 0.2f;

    public int DamageInSwooshes = 0;
    public float DodgeChanceIncreasePercentage = 0.15f;

    public WeaponEquipBehavior Behavior;

    private void Start()
    {
        swooshCooldown = new TimeVariable() { Cooldown = 0, LastTime = Time.time };
    }

    public override void Fire()
    { }

    public void Swoosh()
    {
        int indexToPlay;
        var playStaySameAnim = Random.Range(0, 50) < 13;

        if (playStaySameAnim)
        {
            indexToPlay = StaysSameRange.Random();
        }
        else
        {
            if (WasLastAnimDown)
            {
                indexToPlay = DownToUpAnimsRange.Random();
            }
            else
            {
                indexToPlay = UpToDownAnimsRange.Random();
            }

            WasLastAnimDown = !WasLastAnimDown;

        }

        Animator.SetFloat("randomAttackValue", indexToPlay);
        Animator.SetTrigger("FireTrigger");

        // play sound
        if (GameManager.instance.AreSoundEffectsAllowed)
        {
            var fxSource = GameManager.instance.Player.WeaponController.fxSource;
            var dict = GameManager.instance.Player.WeaponController.weaponFxDict;
            fxSource.clip = dict["sword_swoosh"].Random();
            fxSource.pitch = UnityEngine.Random.Range(0.96f, 1.08f);
            fxSource.Play();
        }
    }

    private void Update()
    {
        if (swooshCooldown.IsOver())
        {
            var rate = Mathf.Clamp(FireRate * GameConstants.Constants.FireRateMultiplier, 0.48f, 5f);
            Animator.speed = Mathf.Clamp(1 / (rate * 1.2f), 1, 2);
            swooshCooldown.Cooldown = rate;
            swooshCooldown.Reset();
            ignoreCollision = false;
            //var sqrDist = DistanceToClosestEnemy();

            // if (sqrDist < 5 * 5)
            {
                Swoosh();
            }
        }
    }

    private void OnEnable()
    {
        Animator.ResetTrigger("FireTrigger");
        Animator.SetFloat("randomAttackValue", 0);
        WasLastAnimDown = false;
        Animator.Play("Default");

        var spx = GameManager.instance.Player.defaultSpeed.x;
        var spy = GameManager.instance.Player.defaultSpeed.y;

        GameManager.instance.Player.speedX = Mathf.Clamp(spx + spx * movementSpeedAddPercentage, 3.3f, 6.5f);
        GameManager.instance.Player.speedY = Mathf.Clamp(spy + spy * movementSpeedAddPercentage, 3.7f, 6.5f);

        GameConstants.Constants.DodgeChance += DodgeChanceIncreasePercentage;

        if (Behavior)
        {
            CoroutineManager.InvokeAction(() => { Behavior.OnEquip(); }, new WaitForEndOfFrame());
        }
    }

    private void OnDisable()
    {
        GameManager.instance.Player.ResetSpeed();

        GameConstants.Constants.DodgeChance -= DodgeChanceIncreasePercentage;

        WasLastAnimDown = false;
        transform.localPosition = Vector3.zero;
        transform.localEulerAngles = Vector3.zero;

        if (Behavior)
            Behavior.OnUnequip();
    }

    private float DistanceToClosestEnemy()
    {
        var lowestDist = 60000f;
        foreach (Transform enemy in GameManager.instance.EnemySpawner.AllEnemies)
        {
            if (!enemy.gameObject.activeSelf)
                continue;

            // only on the right side
            if (enemy.position.x > transform.position.x + 1.5f)
            {
                var enemyComp = enemy.GetComponent<DamageReceiver>();
                if (enemyComp && enemyComp.currentHitPoints > 0)
                {
                    var distSqr = (enemy.position - transform.position).sqrMagnitude;
                    if (distSqr < lowestDist)
                    {
                        lowestDist = distSqr;
                    }
                }
                else if (!enemyComp)
                {
                    var child = enemy.GetChild(0);
                    if (child.TryGetComponent<BasicBoss>(out var _))
                    {
                        var distSqr = (child.position - transform.position).sqrMagnitude;
                        if (distSqr < lowestDist)
                        {
                            lowestDist = distSqr;
                        }
                    }
                }
            }
        }

        return lowestDist;
    }

    public override void MissileFire(GameObject missilePrefab, float damageModifier = 1)
    {
    }

    public override void TripleFire(int offset)
    {
    }

    public override void RoundFire()
    {
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!ignoreCollision && collision.CompareTag("Melee"))
        {
            ignoreCollision = true;
            var enemy = collision.GetComponentInParent<DamageReceiver>();

            int damage = 0;
            // add enemy hit points to increase damage
            var isCrit = false;

            if (DamageInSwooshes <= 0)
            {
                var playerStats = GameManager.instance.Player.PlayerStats;
                var baseDamageIncrement = playerStats.Modifiers.AllUpgrades.Find(x => x.WeaponType == TypeOfWeapon).DamageIncreasePerWave;
                damage = Mathf.RoundToInt(DamageRange.Random() + Mathf.RoundToInt(baseDamageIncrement));

                if(UnityEngine.Random.value < Data.AdditionalData.CriticalChance)
                {
                    damage += Data.AdditionalData.ComputeCriticalDamage();
                    isCrit = true;
                }
            }
            else
            {
                damage = (enemy.HitPoints / DamageInSwooshes) + 1;
            }

            enemy.ReceiveDamage(new DamageInfo(damage, isCrit));
            //CombatObserver.instance.DispatchDamage(damage.DamageValue);

            if (GameManager.instance.Player.IsDeepWoundActive)
            {
                enemy.ActivateDot(LevelProps.instance.DeepWoundScriptable);
            }
        }
    }
}
