using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDisarmedEvent
{
    public bool Disarmed;
    public EnemyDisarmedEvent(bool disarmed)
    {
        Disarmed = disarmed;
    }
}

public class DisarmEquipBehavior : WeaponEquipBehavior
{
    public override void OnEquip()
    {
        GameConstants.Constants.DisarmEnemy = true;
        GameManager.instance.EventManager.Dispatch(new EnemyDisarmedEvent(true));
    }

    public override void OnUnequip()
    {
        GameConstants.Constants.DisarmEnemy = false;
        GameManager.instance.EventManager.Dispatch(new EnemyDisarmedEvent(false));
    }
}
