using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public interface IShooter
{
    Vector2 ShootingCooldown { get; set; }
}

public class BasicEnemy : TimeCompute, ILocalizable
{
    public virtual bool IsShooter => false;

    public delegate void EnemyEventArgs(BasicEnemy enemy);
    public event EnemyEventArgs OnSendKilledDataToSpawner;

    public bool AnimateWalking = true;

    [MinMaxSlider(0, 3, true)]
    public Vector2 ChangeDirectionCooldownRange;

    [Tooltip("Time of how long it takes when reaching a distance to go to next point")]
    public Vector2 ThinkingTimeRange;

    public float MoveSpeed = 2;

    private float currentChangeDirectionCooldown;

    private float currentWaitingForThinkingTime, currentThinkingTime, lastWaitingForThinkingTime, lastThinkingTime;

    protected Vector3 CurrentPoint;

    public Animator _animator;

    protected bool IsThinking;
    public bool IsDead = false;

    public Vector2 GetToGameAreaTimeRange = new Vector2(0,3);
    private float getToGameAreaTime = 1;

    public GameObject DisarmIcon;
    private Enemy enemyScriptableObject;

    public Transform _transform { get; private set; }

    public bool AllowDrops = true;

    protected virtual void Start() 
    {
        _transform = transform;
        ComputeTime();
        ChangeDirection();
        currentWaitingForThinkingTime = ThinkingTimeRange.Random();
        currentThinkingTime = ThinkingTimeRange.Random();
        lastWaitingForThinkingTime = Time.time;
        lastThinkingTime = Time.time;

        getToGameAreaTime = GetToGameAreaTimeRange.Random();
        if (DisarmIcon)
            DisarmIcon.SetActive(GameConstants.Constants.DisarmEnemy);

        GameManager.instance.EventManager.AddListener<GamePauseEvent>(OnGamePaused);
        GameManager.instance.EventManager.AddListener<EnemyDisarmedEvent>(OnEnemyDisarmed);
    }

    protected virtual void OnEnemyDisarmed(EnemyDisarmedEvent obj)
    {
        if(DisarmIcon)
            DisarmIcon.SetActive(obj.Disarmed);

        if(!obj.Disarmed)
        {
            ResetTimers();
        }
    }

    protected virtual void ResetTimers()
    {
        ComputeTime();
        lastThinkingTime = Time.time;
        lastWaitingForThinkingTime = Time.time;
        currentWaitingForThinkingTime = ThinkingTimeRange.Random();
        currentThinkingTime = ThinkingTimeRange.Random();
    }

    private void OnDestroy()
    {
        GameManager.instance.EventManager.RemoveListener<GamePauseEvent>(OnGamePaused);
        GameManager.instance.EventManager.RemoveListener<EnemyDisarmedEvent>(OnEnemyDisarmed);
    }

    private void OnGamePaused(GamePauseEvent gpe)
    {
        if(gpe.IsPaused)
        {
            _animator.SetFloat("Speed", 0f);
        }
        else
        {

        }
    }

    public virtual void SetParams(Enemy enemy)
    {
        enemyScriptableObject = enemy;
        var hp = enemy.HitPointsRange.Random();
        GetComponent<DamageReceiver>().SetHp(hp);
    }

    internal void ExplodeOnMelee()
    {
        GameEffectsPool.SpawnElectricExplosion(_transform.position, 1.5f);
        ExplodeOnMeleeWithoutSound();
    }

    internal void ExplodeOnMeleeWithoutSound()
    {
        Destroy(gameObject);
        Kill(false);
    }

    internal void ExplodeOnSnap()
    {
        Destroy(gameObject);
        Kill(false);
    }

    public virtual void Kill(bool withEffects = true)
    {
        if (IsDead)
            return;

        IsDead = true;

        OnSendKilledDataToSpawner?.Invoke(this);

        if(withEffects)
            GameEffectsPool.SpawnNormalExplosion(_transform.position, 1.5f);

        // set event driven behaviour
        if (enemyScriptableObject != null)
        {
            GameManager.instance.EventManager.Dispatch(new RewardScoreEvent(enemyScriptableObject.RewardPoints));

            // stats
            var nonScaled = enemyScriptableObject.RewardPoints;
            var scaled = Mathf.FloorToInt(nonScaled * GameManager.instance.GameConstants.KillingSpreeMultiplier);

            if (nonScaled > GameManager.instance.GameStats.MaxScorePerEnemy)
                GameManager.instance.GameStats.MaxScorePerEnemy = nonScaled;
            if (scaled > GameManager.instance.GameStats.MaxScorePerEnemyScaled)
                GameManager.instance.GameStats.MaxScorePerEnemyScaled = scaled;
        }

        if(AllowDrops)
            GameManager.instance.EventManager.Dispatch(new EnemyDropSpawnEvent(this));

        if(DisarmIcon)
            DisarmIcon.SetActive(false);

        GameManager.instance.EventManager.Dispatch(new EnemyKilledEvent(this));

        _animator.SetBool("DeathBack", true);
        gameObject.layer = LayerMask.NameToLayer("TransparentFX");
        GetComponent<Collider2D>().enabled = false;
        transform.Find("MeleeTrigger").gameObject.SetActive(false);
        var shadow = transform.GetChild(0).Find("Shadow");
        if (shadow)
            shadow.gameObject.SetActive(false);

        GetComponent<F3DCharacterAvatar>().TweenAlpha(0.3f, 0, 1, 0.5f);

        // perk behaviours
        if(GameManager.instance.GameConstants.IsTntActive)
        {
            Instantiate(LevelProps.instance.TntExplosion, _transform.position, Quaternion.identity);
        }

        if(UnityEngine.Random.value < GameConstants.Constants.EnemyDropShieldChance)
        {
            PowerupSpawner.Spawn(PowerupSpawner.Instance.Powerups.Powerups.Find(x => x.Title == "Shield Refill"), _transform.position + Vector3.up*UnityEngine.Random.Range(0.5f, 0.8f));
        }

        //if (NamesEasterEgg.instance.IsActive)
        //{
        //    var obj = transform.Find("EasterEggText(Clone)");
        //    if(obj)
        //        obj.gameObject.SetActive(false);
        //}
    }

    protected virtual void Update()
    {
        if(GameManager.instance.IsGamePaused)
            return;

        if(IsDead)
        {
            CurrentPoint.y = _transform.position.y;
            _transform.position = MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * GameManager.instance.GlobalSpeed);
            _animator.SetFloat("Speed", 0);
            return;
        }

        if(_transform.position.x < LevelInfo.instance.LeftDespawnCoordinateX)
        {
            OnSendKilledDataToSpawner?.Invoke(this);
            Destroy(gameObject);

            // take score
            GameManager.instance.EventManager.Dispatch(new RewardScoreEvent(Mathf.FloorToInt(-enemyScriptableObject.RewardPoints)));

        }

        // first thing is to get to the game area

        if (getToGameAreaTime > 0)
        {
            getToGameAreaTime-= Time.deltaTime;
            _transform.position = MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * MoveSpeed * GameManager.instance.GlobalSpeed);
            if(AnimateWalking)
                _animator.SetFloat("Speed", 2.1f);
            return;
        }

        // check if we are stopped
        if (!IsThinking && Time.time - lastThinkingTime > currentThinkingTime)
        {
            // we are stopped and thinking
            IsThinking = true;
            _animator.SetFloat("Speed", 0f);
            currentThinkingTime = ThinkingTimeRange.Random();
            lastThinkingTime = Time.time;
        }

         // determine for how long we will be waiting
        if (IsThinking && Time.time - lastWaitingForThinkingTime > currentWaitingForThinkingTime)
        {
            // reset thinking
            currentWaitingForThinkingTime = ThinkingTimeRange.Random();
            lastWaitingForThinkingTime = Time.time;
            IsThinking = false;
        }
        
        if(!IsThinking)
        {
            // we are not stopped, do walking
            if (TimeSinceLastCompute > currentChangeDirectionCooldown )
            {
                currentChangeDirectionCooldown = ChangeDirectionCooldownRange.Random();
                ComputeTime();
                ChangeDirection();
            }

            _transform.position = MoveTowards(_transform.position, CurrentPoint, Time.deltaTime * MoveSpeed * GameManager.instance.GlobalSpeed);
            if(AnimateWalking)
                _animator.SetFloat("Speed", 2.1f);
        }
        else
        {
            // if we are stopped, go to player because player is running towards enemy if parallax is not paused
            if (!GameManager.instance.IsParallaxPaused)
            {
                var goal = LevelInfo.instance.BoundLowerLeft.position;
                goal.y = _transform.position.y;
                _transform.position += GameManager.instance.GlobalSpeed * Time.deltaTime * Vector3.left; //GameManager.instance.Parallax.MainPlanes[0].Speed * GameConstants.Constants.GameSpeedMultiplier;
            }
        }

        OnThinkingEvents(true);
    }

    protected virtual Vector3 MoveTowards(Vector3 current, Vector3 target, float maxDistanceDelta)
    {
        return Vector3.MoveTowards(current, target, maxDistanceDelta);
    }

    protected virtual void ChangeDirection()
    {
        var point = LevelInfo.instance.GetRandomPointToLeftSide(_transform.position);
        CurrentPoint = point;
    }

    protected virtual void OnThinkingEvents(bool isThinking)
    {
    }
}
