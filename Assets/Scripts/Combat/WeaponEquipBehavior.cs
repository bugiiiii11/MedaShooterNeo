using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponEquipBehavior : ScriptableObject
{
    public abstract void OnEquip();
    public abstract void OnUnequip();

}
