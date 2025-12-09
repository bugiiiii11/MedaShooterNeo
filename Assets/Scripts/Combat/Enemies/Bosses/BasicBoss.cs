using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BasicBoss : DamageReceiver
{
    protected EnemySpawner spawner;
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
