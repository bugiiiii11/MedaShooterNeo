using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerProfileInfo : Singleton<PlayerProfileInfo>
{
    [SerializeField]
    private string walletAddress;

    public bool IsUserValid = false;
    public bool IsUserStaker = false;
    public bool IsUserFarmer = false;
    public bool IsLandTicketOwner = false;
    public long NftLandCount = 0; // Total staked amount for shield duration calculation

    public InventoryConfig NftHeroes;
    public InventoryConfig NftWeapons;
    public List<BoostPackage> BoostPackages = new List<BoostPackage>();

    private INft equippedHero;
    public INft EquippedHero
    {
        get
        {
#if UNITY_EDITOR
            EqHero = equippedHero?.Name;
#endif
            return equippedHero;
        }
        set
        {
#if UNITY_EDITOR
            EqHero = "";
#endif
            equippedHero = value;
        }
    }

    private INft equippedWeapon;
    public INft EquippedWeapon
    { 
        get
        {
#if UNITY_EDITOR
            EqWeapon = equippedWeapon?.Name;
#endif
            return equippedWeapon;
        }

        set
        {
#if UNITY_EDITOR
            EqWeapon = "";
#endif
            equippedWeapon = value;
        }
    }

    [SerializeField]
    public NftHandler NftHandler;

#if UNITY_EDITOR
    [ReadOnly]
    public string EqHero;
    [ReadOnly]
    public string EqWeapon;
#endif

    public string WalletAddress
    {
        get
        {
            return walletAddress;
        }
        set
        {
            Debug.Log("Wallet address has been set: " + value);
            walletAddress = value;
            IsUserValid = true;
            /*
            // validation
            var validation = GetComponent<WalletClosedAccessValidator>();

            if(validation)
            {
                validation.Validate(walletAddress);
            }*/
        }
    }

    internal AbilityConfig GetWeaponAbilityDescriptor(string n)
    {
        return NftHandler.otherAbilities.Find(x => x.AbilityName == n);
    }
}

[Serializable]
public class NftHandler
{
    [Serializable]
    public struct FractionAbilityPair
    {
        public NftFraction Fraction;
        public AbilityConfig Config;
    }

    public List<FractionAbilityPair> fractionAbilities;
    public List<AbilityConfig> otherAbilities;

    public AbilityConfig GetAbilityForFraction(NftFraction fraction) => fractionAbilities.Find(a => a.Fraction == fraction).Config;
    public AbilityConfig GetAbility(string n) => fractionAbilities.Select(a => a.Config).FirstOrDefault(a => a.AbilityName == n);
}