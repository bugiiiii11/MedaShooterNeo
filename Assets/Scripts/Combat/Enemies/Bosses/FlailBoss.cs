using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FlailBoss : BasicBoss
{
    public enum State
    {
        GoToGameArea,
        AttackWithProjectiles,
        FlailShield,
        AssaultWithProjectiles,
        FlailsOnly,
        FinishingBossWithProjectiles
    }

    [Serializable]
    public class StateInfo
    {
        public State State;
        public bool IsEndingState = false;
    }

    public Vector3 GoToPosition, OutOfGamePosition;
    public Animator _animator;

    public bool IsImmuneToDamage = true;

    private Transform _transform;
    protected Vector3 CurrentPoint;

    public List<StateInfo> StateMachine;

    private StateInfo activeState;
    private int currentState = 0;

    public GameObject Flails, FlailShieldAdds, BombrunnerPrefab;
    public Transform SpawnBombRunnerTop, SpawnBombRunnerBottom;

    public Vector2 AttackWithProjectilesCooldownRange;
    private float lastAttackWithProjectilesTime = 0;
    private float currentAttackCooldown = 0;

    // movement
    public Vector2 MovementCooldownRange;
    private float lastChangeMovementTime = 0;
    private float currentMovementCooldown = 0;

    public Vector2 DamageRangePhase2;
    private Enemy enemyScriptableObject;
    private EnemyWeaponController weaponController;

    public override void Initialize(EnemySpawner spawner)
    {
        base.Initialize(spawner);

        _transform = transform;

        // switch music
        DoubleAudioSource.instance.CrossFade(DoubleAudioSource.instance.Clips[1], 0.45f,6f);

        weaponController = GetComponent<EnemyWeaponController>();

        weaponController.GetCurrentWeapon().DamageRange = DamageRangePhase2;
    }

    protected override void OnDied()
    {
        base.OnDied();
        
        DoubleAudioSource.instance.CrossFade(DoubleAudioSource.instance.Clips[0], 0.45f, 6f);

        // animation of dying
        var weaponController = GetComponent<EnemyWeaponController>();
        weaponController.GetCurrentWeapon().gameObject.SetActive(false);
        weaponController.enabled = false;
    }

    public override void ReceiveDamage(DamageInfo damage)
    {
        if (!IsImmuneToDamage)
        {
            base.ReceiveDamage(damage);

            if(currentHitPoints <= 0)
            {
                Kill();
            }
        }
        else
        {
            DamageTextSpawner.Spawn("<size=23><color=white>Immune</color></size>", _transform.position);
        }
    }


    private void Update()
    {
        if (GameManager.instance.IsGamePaused)
        {
            _animator.SetFloat("Speed", 0f);
            return;
        }

        if (currentHitPoints <= 0)
            return;

        var state = StateMachine[currentState];

        if(state != activeState)
        {
            activeState = state;
            OnEnterState(activeState);
        }

        switch (state.State)
        {
            case State.GoToGameArea:
                // ignore duration
                _transform.position = Vector3.MoveTowards(_transform.position, GoToPosition, Time.deltaTime);
                _animator.SetFloat("Speed", 2.2f);

                if (_transform.position == GoToPosition)
                {
                    // next state
                    NextState();
                }

                break;
            case State.AttackWithProjectiles:

                if (Time.time - lastAttackWithProjectilesTime > currentAttackCooldown)
                {
                    currentAttackCooldown = AttackWithProjectilesCooldownRange.Random();
                    lastAttackWithProjectilesTime = Time.time;

                    var attackType = UnityEngine.Random.Range(0, 3);

                    if (attackType == 0)
                    {
                        weaponController.Fire();
                    }
                    else if (attackType == 1)
                    {
                        weaponController.TripleFire();
                    }
                    else
                    {
                        weaponController.RoundedFire();
                    }
                }
                else
                {
                    weaponController.StopFire();
                }

                //move

                _transform.position = Vector3.MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * 1.8f * GameConstants.Constants.GameSpeedMultiplier);
                if (_transform.position == CurrentPoint)
                    _animator.SetFloat("Speed", 0f);
                else
                    _animator.SetFloat("Speed", 2.2f);


                if (Time.time - lastChangeMovementTime > currentMovementCooldown)
                {
                    currentMovementCooldown = MovementCooldownRange.Random();
                    lastChangeMovementTime = Time.time;
                    ChangeDirection();
                }

                // check transition to other state at 75%
                if (currentHitPoints <= HitPoints * 0.75f)
                {
                    NextState();
                }

                break;
            case State.FlailShield:
                _transform.position = Vector3.MoveTowards(_transform.position, GoToPosition, Time.deltaTime);
                if (_transform.position == GoToPosition)
                    _animator.SetFloat("Speed", 0f);
                else
                    _animator.SetFloat("Speed", 2.2f);

                break;

            case State.AssaultWithProjectiles:
                if (Time.time - lastAttackWithProjectilesTime > currentAttackCooldown)
                {
                    currentAttackCooldown = AttackWithProjectilesCooldownRange.Random() * 0.9f;
                    lastAttackWithProjectilesTime = Time.time;

                    var attackType = UnityEngine.Random.Range(0, 3);

                    if (attackType == 0)
                    {
                        weaponController.Fire();
                    }
                    else if (attackType == 1)
                    {
                        weaponController.TripleFire();
                    }
                    else
                    {
                        weaponController.RoundedFire();
                    }
                }
                else
                {
                    weaponController.StopFire();
                }

                //move
                _transform.position = Vector3.MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * 1.8f * GameConstants.Constants.GameSpeedMultiplier);
                if (_transform.position == CurrentPoint)
                    _animator.SetFloat("Speed", 0f);
                else
                    _animator.SetFloat("Speed", 2.2f);


                if (Time.time - lastChangeMovementTime > currentMovementCooldown)
                {
                    currentMovementCooldown = MovementCooldownRange.Random();
                    lastChangeMovementTime = Time.time;
                    ChangeDirection();
                }

                // check transition to other state at 25%
                if (currentHitPoints <= HitPoints * 0.25f)
                {
                    NextState();
                }

                break;

            case State.FlailsOnly:
                _transform.position = Vector3.MoveTowards(_transform.position, OutOfGamePosition, Time.deltaTime * 1.8f);
                _animator.SetFloat("Speed", 2.2f);

                if (_transform.position == OutOfGamePosition)
                {
                    _animator.SetFloat("Speed", 0f);
                    if (!Flails.activeSelf)
                    {
                        Flails.SetActive(true);
                        // create offset in animations
                        var flailAnimators = Flails.GetComponentsInChildren<Animator>();
                        foreach (var animator in flailAnimators)
                        {
                            animator.SetFloat("offset", UnityEngine.Random.Range(0, 1.2f));
                        }
                    }
                }

                break;
            case State.FinishingBossWithProjectiles:
                if (Time.time - lastAttackWithProjectilesTime > currentAttackCooldown)
                {
                    currentAttackCooldown = AttackWithProjectilesCooldownRange.Random() * 0.9f;
                    lastAttackWithProjectilesTime = Time.time;

                    var attackType = UnityEngine.Random.Range(0, 2);

                    if (attackType == 0)
                    {
                        weaponController.TripleFire();
                    }
                    else if (attackType == 1)
                    {
                        weaponController.RoundedFire();
                    }
                }
                else
                {
                    weaponController.StopFire();
                }

                //move
                _transform.position = Vector3.MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * 1.8f * GameConstants.Constants.GameSpeedMultiplier);
                if (_transform.position == CurrentPoint)
                    _animator.SetFloat("Speed", 0f);
                else
                    _animator.SetFloat("Speed", 2.2f);


                if (Time.time - lastChangeMovementTime > currentMovementCooldown)
                {
                    currentMovementCooldown = MovementCooldownRange.Random();
                    lastChangeMovementTime = Time.time;
                    ChangeDirection();
                }
              
                break;
        }


        //_transform.position = MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * MoveSpeed * GameConstants.Constants.GameSpeedMultiplier);

    }

    private List<BossAddDamageReceiver> flailsOnlyReceivers = new();
    private List<BossAddDamageReceiver> flailShieldAdds = new();
    private void OnEnterState(StateInfo state)
    {
        Vector3 scale;

        switch (state.State)
        {
            case State.GoToGameArea:
                IsImmuneToDamage = true;

                // check if orientation is good
                scale = _transform.localScale;
                if(scale.x < 0)
                {
                    scale.x *= -1;
                    _transform.localScale = scale;
                }
                break;

            case State.AttackWithProjectiles:
                IsImmuneToDamage = false;
                lastAttackWithProjectilesTime = Time.time;
                currentAttackCooldown = AttackWithProjectilesCooldownRange.Random();
                break;

            case State.FlailShield:
                FlailShieldAdds.gameObject.SetActive(true);
                IsImmuneToDamage = true;
                SpawnFlailShieldAdds();

                break;

            case State.FlailsOnly:
                scale = _transform.localScale;
                scale.x *= -1;
                _transform.localScale = scale;

                // add listeners for flail damage
                var flailReceivers = Flails.GetComponentsInChildren<BossAddDamageReceiver>();
                foreach(var rec in flailReceivers)
                {
                    flailsOnlyReceivers.Add(rec);
                    rec.OnBossAddKilled += (a) => { ReportAddKilled(state.State, a); }; 
                }
                break;

            case State.AssaultWithProjectiles:
                IsImmuneToDamage = false;
                lastAttackWithProjectilesTime = Time.time;
                currentAttackCooldown = AttackWithProjectilesCooldownRange.Random();

                // reset previous listeners
                flailsOnlyReceivers.Clear();

                // spawn assault
                var powerupSpawner = PowerupSpawner.Instance;
                var myPowerup = powerupSpawner.Powerups.Powerups.Find(x => x.Title == "Assault Rifle");
                var pos = Vector3.Lerp(LevelInfo.instance.BoundLowerLeft.position, LevelInfo.instance.BoundUpperRight.position, 0.5f);
                
                pos.z = 0;
                var spawnedPoweup = Instantiate(myPowerup.AssetPrefab, pos, Quaternion.identity).GetComponent<WeaponPickedUpPowerup>();
                spawnedPoweup.TimeToKeep = 25;

                break;

            case State.FinishingBossWithProjectiles:
                IsImmuneToDamage = false;
                lastAttackWithProjectilesTime = Time.time;
                currentAttackCooldown = AttackWithProjectilesCooldownRange.Random();
                break;
        }
    }


    public void ReportBombRunnerNotKilled(FlailBombRunnerEnemy flailBombRunnerEnemy)
    {
        foreach(var enemy in flailShieldAdds.ToList().Where(x => x.gameObject != flailBombRunnerEnemy.gameObject))
        {
            enemy.GetComponent<FlailBombRunnerEnemy>().ExplodeOnMelee();
        }

        flailShieldAdds.Clear();

        SpawnFlailShieldAdds();

        //damage player
        GameManager.instance.Player.ReceiveDamage(Mathf.FloorToInt(GameManager.instance.Player.PlayerStats.MaxHp * 0.3f), true);
    }

    private void SpawnFlailShieldAdds()
    {
        var topAdd = Instantiate(BombrunnerPrefab, SpawnBombRunnerTop.position, Quaternion.identity).GetComponent<BossAddDamageReceiver>();
        var botAdd = Instantiate(BombrunnerPrefab, SpawnBombRunnerBottom.position, Quaternion.identity).GetComponent<BossAddDamageReceiver>();

        topAdd.transform.SetParent(GameManager.instance.EnemySpawner.AllEnemies);
        botAdd.transform.SetParent(GameManager.instance.EnemySpawner.AllEnemies);

        flailShieldAdds.Add(topAdd);
        flailShieldAdds.Add(botAdd);

        topAdd.OnBossAddKilled += (a) => { ReportAddKilled(State.FlailShield, a); };
        botAdd.OnBossAddKilled += (a) => { ReportAddKilled(State.FlailShield, a); };

        topAdd.GetComponent<FlailBombRunnerEnemy>().BossParent = this;
        botAdd.GetComponent<FlailBombRunnerEnemy>().BossParent = this;
    }

    private void ReportAddKilled(State state, BossAddDamageReceiver add)
    {
        if(state == State.FlailsOnly)
        {
            // each kill should heal other flails
            foreach(var flail in flailsOnlyReceivers.Where(x => x != add))
            {
                flail.Heal();
            }

            flailsOnlyReceivers.Remove(add);

            if(flailsOnlyReceivers.Count == 0)
            {
                // next phase
                NextState();
            }
        }

        else if(state == State.FlailShield)
        {
            flailShieldAdds.Remove(add);

            if(flailShieldAdds.Count == 0)
            {
                NextState(); 
                flailShieldAdds = null;

                // damage boss with 25% maxHp damage
                IsImmuneToDamage = false;
                ReceiveDamage(new DamageInfo( Mathf.FloorToInt(HitPoints * 0.25f), false));
                GameEffectsPool.SpawnElectricExplosion(_transform.position, 1.5f);
            }
        }
    }

    public void Kill()
    {
        GameEffectsPool.SpawnNormalExplosion(_transform.position, 1.5f);

        // add score but ignore multiplier..so we wont get values like 250 * 3
        var scoreEvent = new RewardScoreEvent(enemyScriptableObject.RewardPoints);
        scoreEvent.AllowMultiplier = false;
        GameManager.instance.EventManager.Dispatch(scoreEvent);
        Destroy(transform.parent.gameObject);
    }

    private void NextState()
    {
        currentState = (currentState + 1) % StateMachine.Count;
    }

    protected override void OnGamePaused(GamePauseEvent obj)
    {

    }

    public override void SetParams(Enemy enemy)
    {
        enemyScriptableObject = enemy;
    }

    protected virtual void ChangeDirection()
    {
        var ll = LevelInfo.instance.BoundLowerLeft.position.y;
        var ur = LevelInfo.instance.BoundUpperRight.position.y;


        CurrentPoint = new Vector3(transform.position.x, UnityEngine.Random.Range(ll, ur));
    }
}
