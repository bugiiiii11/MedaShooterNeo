using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyWeaponController : WeaponController
{
    public void Fire()
    {
        Slots[EquippedSlot].Weapons[EquippedWeapon].Fire();
        PlaySoundEffect("pistol_shot");
    }
    public void MissileFire(GameObject missilePrefab, float damageModifier = 1)
    {
        Slots[EquippedSlot].Weapons[EquippedWeapon].MissileFire(missilePrefab, damageModifier);
        PlaySoundEffect("pistol_shot");
    }


    internal void TripleFire()
    {
        Slots[EquippedSlot].Weapons[EquippedWeapon].TripleFire(600);

        PlaySoundEffect("pistol_shot");
    }

    internal void RoundedFire()
    {
        Slots[EquippedSlot].Weapons[EquippedWeapon].RoundFire();
        PlaySoundEffect("pistol_shot");
    }

    private void PlaySoundEffect(string name)
    {
        if (GameManager.instance.AreSoundEffectsAllowed)
        {
            fxSource.clip = weaponFxDict[name].Random();
            fxSource.pitch = UnityEngine.Random.Range(0.96f, 1.08f);
            fxSource.Play();
        }
    }

    // Update is called once per frame
    protected override void Update()
    {

    }

    internal void StopFire()
    {
        Slots[EquippedSlot].Weapons[EquippedWeapon].Stop();
    }
}
