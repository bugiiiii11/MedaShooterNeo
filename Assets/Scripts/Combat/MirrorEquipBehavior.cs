using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorEquipBehavior : WeaponEquipBehavior
{
    public override void OnEquip()
    {
        Debug.Log("equip " + name);
        GameManager.instance.Player.MirrorBullets = true;
        GameManager.instance.Player.MirrorVfx.SetActive(true);
    }

    public override void OnUnequip()
    {
        Debug.Log("unequip " + name);
        GameManager.instance.Player.MirrorBullets = false;
        GameManager.instance.Player.MirrorVfx.SetActive(false);
    }
}
