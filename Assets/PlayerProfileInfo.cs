using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerProfileInfo : Singleton<PlayerProfileInfo>
{
    // Ensure this singleton persists between scenes (inventory -> game)
    public override void Awake()
    {
        isPersistant = true; // Must persist to carry boost data from inventory to game scene
        base.Awake();

#if UNITY_EDITOR
        // Apply debug combat boost if enabled (for testing without backend - EDITOR ONLY)
        if (UseDebugCombatBoost)
        {
            ApplyDebugCombatBoost();
        }
        else
        {
            // Clear any leftover debug boost data when debug mode is disabled
            ClearCombatBoostData();
        }
#else
        // In builds, always disable debug mode and clear any leftover test data
        UseDebugCombatBoost = false;
        ClearCombatBoostData();
#endif
    }

    /// <summary>
    /// Clears combat boost data (for production/when debug mode is disabled)
    /// </summary>
    private void ClearCombatBoostData()
    {
        HasActiveCombatBoost = false;
        ActiveCombatBoost = null;
        CombatBoostEffects = null;
        Debug.Log("💊 Combat boost data cleared on Awake (production mode or debug disabled)");
    }

    /// <summary>
    /// Creates dummy combat boost data for testing purposes
    /// </summary>
    private void ApplyDebugCombatBoost()
    {
        Debug.Log("🧪 <color=yellow>DEBUG MODE: Applying dummy Combat Boost data for testing</color>");

        HasActiveCombatBoost = true;

        ActiveCombatBoost = new CombatBoost
        {
            Id = "debug-boost-001",
            Type = "combat",
            ActivatedAt = DateTime.UtcNow.ToString("o"),
            ExpiresAt = DateTime.UtcNow.AddHours(24).ToString("o"),
            RemainingSeconds = 86400, // 24 hours
            Effects = new BoostEffects
            {
                DamageMultiplier = 1.3f,    // +30% damage
                FireRateMultiplier = 1.15f,  // +15% fire rate
                CritBonus = 0.07f            // +7% crit chance
            }
        };

        CombatBoostEffects = ActiveCombatBoost.Effects;

        Debug.Log($"🧪 Debug Boost Effects Applied:\n" +
                  $"   • Damage Multiplier: x{CombatBoostEffects.DamageMultiplier}\n" +
                  $"   • Fire Rate Multiplier: x{CombatBoostEffects.FireRateMultiplier}\n" +
                  $"   • Crit Bonus: +{CombatBoostEffects.CritBonus * 100}%");
    }

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

    // Combat Boost (time-limited boosts from backend)
    [BoxGroup("Combat Boost")]
    [Button("🧪 Apply Debug Combat Boost", ButtonSizes.Large)]
    [GUIColor(0.3f, 1f, 0.3f)]
    private void ApplyDebugBoostButton()
    {
        ApplyDebugCombatBoost();

        // Update UI indicator
        if (UIInventory.instance != null)
        {
            UIInventory.instance.UpdateCombatBoostIndicator(true);
            Debug.Log("✅ UI Combat Boost Indicator updated to ACTIVE");
        }
    }

    [BoxGroup("Combat Boost")]
    [Button("❌ Clear Combat Boost", ButtonSizes.Large)]
    [GUIColor(1f, 0.3f, 0.3f)]
    private void ClearDebugBoostButton()
    {
        HasActiveCombatBoost = false;
        ActiveCombatBoost = null;
        CombatBoostEffects = null;

        // Update UI indicator
        if (UIInventory.instance != null)
        {
            UIInventory.instance.UpdateCombatBoostIndicator(false);
            Debug.Log("❌ Combat boost cleared, UI indicator set to INACTIVE");
        }

        Debug.Log("💊 Combat boost data cleared manually");
    }

    [BoxGroup("Combat Boost")]
    [Tooltip("Auto-apply debug boost on Awake (for testing without backend)")]
    public bool UseDebugCombatBoost = false;

    [BoxGroup("Combat Boost")]
    [ReadOnly]
    public bool HasActiveCombatBoost = false;

    [BoxGroup("Combat Boost")]
    [ReadOnly]
    public CombatBoost ActiveCombatBoost = null;

    [BoxGroup("Combat Boost")]
    [ReadOnly]
    public BoostEffects CombatBoostEffects = null;

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