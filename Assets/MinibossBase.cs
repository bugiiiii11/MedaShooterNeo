using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinibossBase : BasicBoss
{
    protected EnemyWeaponController weaponController;
    public Enemy enemyScriptableObject;
    public Animator _animator;
    protected Transform _transform;

    public override void SetParams(Enemy enemy)
    {
        enemyScriptableObject = enemy;
        var hp = enemy.HitPointsRange.Random();
        GetComponent<DamageReceiver>().SetHp(hp);

        weaponController.GetCurrentWeapon().DamageRange = enemy.DamageRange;
        weaponController.GetCurrentWeapon().Data = enemy.AdditionalAttackData;
    }

    protected override void OnGamePaused(GamePauseEvent obj)
    {

    }

    public override void Initialize(EnemySpawner spawner)
    {
        base.Initialize(spawner);
        _transform = transform;
        weaponController = GetComponent<EnemyWeaponController>();
        GameManager.instance.EnemySpawner.KillAllEnemies(gameObject);
    }

    public override void ReceiveDamage(DamageInfo damage)
    {
        base.ReceiveDamage(damage);
        if (currentHitPoints <= 0)
        {
            Kill();
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

    protected override void OnDied()
    {
        base.OnDied();

        // animation of dying
        weaponController.GetCurrentWeapon().gameObject.SetActive(false);
        weaponController.enabled = false;
    }
}
