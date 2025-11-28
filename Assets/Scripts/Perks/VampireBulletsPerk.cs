using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class VampireBulletsPerk : ActiveTimedPerkBehaviour
{
    public override void OnEnd()
    {
        CombatObserver.instance.CombatObserverEvents.RemoveListener<DamageReceivedOnEnemyEvent>(OnDamageReceivedOnEnemy);
    }

    public override void OnInitialize(PlayerMovement player)
    {
        base.OnInitialize(player);

        CombatObserver.instance.CombatObserverEvents.RemoveListener<DamageReceivedOnEnemyEvent>(OnDamageReceivedOnEnemy);
        CombatObserver.instance.CombatObserverEvents.AddListener<DamageReceivedOnEnemyEvent>(OnDamageReceivedOnEnemy);
    }

    private void OnDamageReceivedOnEnemy(DamageReceivedOnEnemyEvent obj)
    {
        var stats = GameManager.instance.Player.PlayerStats;

        // add portion of HP
        var amount = Mathf.CeilToInt(obj.Amount * 0.1f);
        stats.AddHp(amount);
        if (stats.CurrentHp != stats.MaxHp)
            DamageTextSpawner.Spawn(new HealInfo(amount, false), GameManager.instance.Player.transform.position + (Vector3.up * 2f));

        GameManager.instance.EventManager.Dispatch(new PlayerHealthChangeEvent(stats));
    }
}
