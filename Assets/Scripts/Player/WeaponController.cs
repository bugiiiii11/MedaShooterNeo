using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Serialization;
using UnityEngine;
    
[Serializable]
public class Slot
{
    public string SlotName = "Slot";
    [SerializeField] public List<Weapon> Weapons;
    public int WeaponSlot = 0;
    public void Forward()
    {
        WeaponSlot++;
        if (WeaponSlot >= Weapons.Count)
            WeaponSlot = 0;
    }
}

[Serializable]
public class AudioSpecifier
{
    public string Identifier;
    public AudioClip[] Clips;
}

public class WeaponController : MonoBehaviour
{
    public UIWeaponCountdown WeaponCountdown;
    public int EquippedSlot;
    public int EquippedWeapon;
    public List<Slot> Slots;
    public float FireCooldown => Slots[EquippedSlot].Weapons[EquippedWeapon].FireRate;
    private float lastShotTime = 0.3f;

    private F3DCharacterAvatar _avatar;

    [Header("Sounds")]
    public List<AudioSpecifier> WeaponFx;

    [NonSerialized]
    public Dictionary<string, AudioClip[]> weaponFxDict;

    [NonSerialized]
    public AudioSource fxSource;

    public Animator DeepWoundShotAnimator, ChainGunShotAnimator;

    private void Awake()
    {
        _avatar = GetComponent<F3DCharacterAvatar>();
        ActivateWeapon(EquippedSlot, EquippedWeapon);

        weaponFxDict = WeaponFx.ToDictionary(x => x.Identifier, y => y.Clips);
        WeaponFx.Clear();
        WeaponFx = null;

        fxSource = gameObject.AddComponent<AudioSource>();
        fxSource.volume = 0.25f;
    }

    protected virtual void Update()
    {
        if(GameManager.instance.IsGamePaused)
            return;

        // auto fire
        var constants = GameManager.instance.GameConstants;
        var weapon = Slots[EquippedSlot].Weapons[EquippedWeapon];

        if (Time.time - lastShotTime > FireCooldown * constants.FireRateMultiplier * (1-constants.PermanentFireRateMultiplier))
        {
            if(weapon.TypeOfWeapon == WeaponType.ShotgunLaser)
                weapon.TripleFire(350);
            else
                weapon.Fire();

            if (weapon.TypeOfWeapon != WeaponType.Sword && weapon.TypeOfWeapon != WeaponType.PowerfulSword)
            {
                if (DeepWoundShotAnimator.isActiveAndEnabled)
                    DeepWoundShotAnimator.SetTrigger("shoot");
                else if (ChainGunShotAnimator.isActiveAndEnabled)
                    ChainGunShotAnimator.SetTrigger("shoot");
            }

            lastShotTime = Time.time;

            if (GameManager.instance.AreSoundEffectsAllowed && weapon.TypeOfWeapon != WeaponType.Sword && weapon.TypeOfWeapon != WeaponType.PowerfulSword)
            {
                fxSource.clip = weaponFxDict["pistol_shot"].Random();
                fxSource.pitch = UnityEngine.Random.Range(0.96f, 1.08f);
                fxSource.Play();
            }
        }

        if(weapon.TypeOfWeapon == WeaponType.Sniper)
        {
            var enemy = GetClosestEnemy();
            if (enemy)
            {
                // predict a unit ahead
                var pos = enemy.position;
                pos.x -= 0.8f;
                weapon.AimAt(pos, 20);
            }
        }
    }

    private Transform GetClosestEnemy()
    {
        var lowestDist = 60000f;
        Transform closestTr = null;
        foreach (Transform enemy in GameManager.instance.EnemySpawner.AllEnemies)
        {
            if (!enemy.gameObject.activeSelf)
                continue;

            // only on the right side
            if (enemy.position.x > transform.position.x + 1.5f)
            {
                var enemyComp = enemy.GetComponent<DamageReceiver>();
                if (enemyComp && enemyComp.currentHitPoints > 0)
                {
                    var distSqr = (enemy.position - transform.position).sqrMagnitude;
                    if (distSqr < lowestDist)
                    {
                        lowestDist = distSqr;
                        closestTr = enemy;
                    }
                }
                else if(!enemyComp)
                {
                    var child = enemy.GetChild(0);
                    if(child.TryGetComponent<BasicBoss>(out var _))
                    {
                        var distSqr = (child.position - transform.position).sqrMagnitude;
                        if (distSqr < lowestDist)
                        {
                            lowestDist = distSqr;
                            closestTr = child;
                        }
                    }
                }
            }
        }

        return closestTr;
    }

    private void ActivateSlot(int slot)
    {
        if (slot > Slots.Count - 1) return;
        if (slot == EquippedSlot)
            Slots[EquippedSlot].Forward();
        EquippedSlot = slot;
        EquippedWeapon = Slots[EquippedSlot].WeaponSlot;
        ActivateWeapon(EquippedSlot, EquippedWeapon);
    }

    public void SetBool(string var, bool value)
    {
        Slots[EquippedSlot].Weapons[EquippedWeapon].Animator.SetBool(var, value);
    }

    public void SetFloat(string var, float value)
    {
        Slots[EquippedSlot].Weapons[EquippedWeapon].Animator.SetFloat(var, value);
    }
    //////////////////////////////////////////////////////////////

    // Weapon activation
    private void ActivateWeapon(int slot, int weapon)
    {
        (int s, int w) prev = (EquippedSlot, EquippedWeapon);

        EquippedSlot = slot;
        EquippedWeapon = weapon;
        for (var i = 0; i < Slots.Count; i++)
        {
            for (var y = 0; y < Slots[i].Weapons.Count; y++)
                Slots[i].Weapons[y].gameObject.SetActive(slot == i && weapon == y);
        }

        // set previous weap to correct position
        Slots[prev.s].Weapons[prev.w].transform.GetChild(0).GetChild(0).rotation = Quaternion.Euler(Vector2.zero);

        UpdateCharacterHands(SkinManager.Skins[_avatar.SkinUsed]);

        // adjust shot vfx position
        if (DeepWoundShotAnimator)
        {
            var abilityVfx = DeepWoundShotAnimator.transform.parent;
            abilityVfx.transform.position = Slots[EquippedSlot].Weapons[EquippedWeapon].FXSocket.position;
        }
    }
    
    public Weapon GetCurrentWeapon()
    {
        return Slots[EquippedSlot].Weapons[EquippedWeapon];
    }

    public IEnumerable<Weapon> GetAllWeapons()
    {
        foreach(var slot in Slots)
        {
            foreach (var weapon in slot.Weapons)
            {
                yield return weapon;
            }
        }
    }

    // Avatar Hands
    public void UpdateCharacterHands(F3DCharacterAvatar.CharacterArmature armature)
    {
        var myWeapon = GetCurrentWeapon();
        myWeapon.LeftHand.sprite = GetSpriteFromHandId(myWeapon.LeftHandId, armature);
        myWeapon.RightHand.sprite = GetSpriteFromHandId(myWeapon.RightHandId, armature);
    }

    private Sprite GetSpriteFromHandId(int id, F3DCharacterAvatar.CharacterArmature armature)
    {
        switch (id)
        {
            case 0:
                return armature.Hand1.sprite;
            case 1:
                return armature.Hand2.sprite;
            case 2:
                return armature.Hand3.sprite;
            case 3:
                return armature.Hand4.sprite;
            default:
                return armature.Hand1.sprite;
        }
    }

    (int slot, int weap) previousWeapon;
    private float switchedWeaponTime = 0;
    internal void SetWeapon(string playerWeaponName, int timeToKeep)
    {
        var slot = 0;
        var weap = 0;

        var prevWeapon = (EquippedSlot, EquippedWeapon);
        foreach (var s in Slots)
        {
            foreach (var w in s.Weapons)
            {
                if (string.Equals(w.name, playerWeaponName, StringComparison.OrdinalIgnoreCase))
                {
                    ActivateWeapon(slot, weap);
                    break;
                }

                weap++;
            }
            weap = 0;
            slot++;
        }

        // if there is timeToKeep, reset the weap afterwards
        CancelInvoke(nameof(ResetWeaponToPrevious));
        if (timeToKeep > 0)
        {
            if(!isLastWeaponTemporary)
            {
                previousWeapon = prevWeapon;
            }
            
            switchedWeaponTime = Time.time;
            Invoke(nameof(ResetWeaponToPrevious), timeToKeep);
            isLastWeaponTemporary = true;
        }
        else
            isLastWeaponTemporary = false;
    }

    private bool isLastWeaponTemporary = false;

    private void ResetWeaponToPrevious()
    {
        ActivateWeapon(previousWeapon.slot, previousWeapon.weap);
    }

    internal void Mirrored()
    {
        // mirror should take from weapon duration
        var currentTime = WeaponCountdown.CurrentTime;
        TakeFromCurrentWeapon(0.05f);
    }

    public void TakeFromCurrentWeapon(float factor)
    {
       /* var currentTime = WeaponCountdown.CurrentTime;
        if (currentTime > 0)
        {
            currentTime -= WeaponCountdown.maxTime * factor;
            CancelInvoke(nameof(ResetWeaponToPrevious));
            Invoke(nameof(ResetWeaponToPrevious), currentTime);
            WeaponCountdown.ChangeCooldown(currentTime);
        }*/
    }
}
