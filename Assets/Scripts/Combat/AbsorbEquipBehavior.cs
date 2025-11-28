using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbsorbEquipBehavior : WeaponEquipBehavior
{
    public override void OnEquip()
    {
        GameManager.instance.Player.InvincibleFromWeapon = true;
        GameManager.instance.Player.MirrorVfx.SetActive(true);
    }

    public override void OnUnequip()
    {
        GameManager.instance.Player.InvincibleFromWeapon = false;
        GameManager.instance.Player.MirrorVfx.SetActive(false);
    }
}
