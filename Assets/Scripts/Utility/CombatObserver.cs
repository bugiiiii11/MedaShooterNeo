using Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatObserver: Singleton<CombatObserver>
{
    public EventManager CombatObserverEvents = new();

    public void DispatchDamage(int damage)
    {
        CombatObserverEvents.Dispatch(new DamageReceivedOnEnemyEvent(damage));
    }
}

public struct DamageReceivedOnEnemyEvent
{
    public int Amount;
    public DamageReceivedOnEnemyEvent(int damage)
    {
        Amount = damage;
    }
}
