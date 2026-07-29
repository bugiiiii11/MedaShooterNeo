using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasicBoss : DamageReceiver
{
    protected EnemySpawner spawner;

    /// <summary>
    /// Set by the subclass on the killing blow, checked at the top of its ReceiveDamage.
    ///
    /// DamageReceiver has no dead-guard: it clamps currentHitPoints to 0 and calls OnDied()
    /// again for every further hit. BasicEnemy absorbs that downstream because BasicEnemy.Kill
    /// gates on its own IsDead, but the boss classes derive straight from DamageReceiver and had
    /// nothing equivalent -- and Destroy() is deferred to the end of the frame, so the boss is
    /// still a live collider for the rest of it. Every projectile landing in that same frame
    /// therefore re-ran the entire death path.
    /// </summary>
    protected bool IsDead;
    protected override void Start()
    {
        base.Start();

        HitPointsBar = UINumbersHandler.instance.transform.Find("Boss").GetComponent<UIBossInfo>();

        GameManager.instance.EventManager.AddListener<GamePauseEvent>(OnGamePaused);
    }

    protected abstract void OnGamePaused(GamePauseEvent obj);

    public virtual void Initialize(EnemySpawner spawner)
    {
        this.spawner = spawner;
        int bossPoints = UIBossInfo.CalculateBossPoints(spawner.waveNumber);
        UINumbersHandler.instance.SetBossInfo(true, bossPoints);
        // stop parallax
        BackgroundResolver.Pause(true);
        // stop powerups
        PowerupSpawner.Instance.IsActive = false;

        GameManager.instance.EnemySpawner.KillAllEnemies(gameObject);
    }

    protected override void OnDied()
    {
        base.OnDied();
        UINumbersHandler.instance.SetBossInfo(false);
        BackgroundResolver.Pause(false);
        PowerupSpawner.Instance.IsActive = true;
        spawner.OnEnemyKilled(this);
    }

    public abstract void SetParams(Enemy enemy);
}
