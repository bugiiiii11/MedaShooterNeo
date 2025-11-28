using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using System;

public enum NftFraction
{
    Goliath,
    Renegade,
    Neutral
}

public struct CardAttributes
{
    public ObscuredInt Security;

    public ObscuredInt Anonymity;

    public ObscuredInt Innovation;

    public ObscuredInt Power;

    public bool IsRevolution;

    public ObscuredInt Sum() => Security + Anonymity + Innovation + Power;

    public override bool Equals(object obj)
    {
        if (!(obj is CardAttributes other))
        {
            return false;
        }

        return other == this;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public static bool operator ==(CardAttributes a, CardAttributes b)
    {
        return a.Power == b.Power && a.Security == b.Security && a.Anonymity == b.Anonymity && a.Innovation == b.Innovation;
    }

    public static bool operator !=(CardAttributes a, CardAttributes b)
    {
        return a.Power != b.Power || a.Security != b.Security || a.Anonymity != b.Anonymity || a.Innovation != b.Innovation;
    }
}

[Serializable]
public class NftHero : INft
{
    public CardAttributes Attributes = new CardAttributes();
    public string OwnerWallet { get; set; }

    [SerializeField]
    private string name;
    public string Name { get => name; set => name = value; }

    [SerializeField]
    private Sprite visualization;
    public Sprite Visualization { get => visualization; set => visualization = value; }

    public NftFraction Fraction;

    public NftHero(int sec, int anon, int inno, int pwr, string fraction, string name, bool revolution)
    {
        Debug.Log($"🦸 DEBUG: Creating NftHero - Name: '{name}', Faction: '{fraction}', Revolution: {revolution}");
        Debug.Log($"🦸 DEBUG: Hero stats - Sec:{sec}, Ano:{anon}, Inn:{inno}, Pwr:{pwr}");

        try
        {
            // Set attributes first
            Attributes.Security = sec;
            Attributes.Anonymity = anon;
            Attributes.Innovation = inno;
            Attributes.Power = pwr;
            Attributes.IsRevolution = revolution;

            Debug.Log($"🦸 DEBUG: Hero attributes set successfully");

            // Set faction with enhanced error handling
            try
            {
                Fraction = fraction.ToLower() switch
                {
                    "goliath" => NftFraction.Goliath,
                    "renegade" => NftFraction.Renegade,
                    "neutral" => NftFraction.Neutral,
                    _ => NftFraction.Neutral,
                };
                Debug.Log($"🦸 DEBUG: Faction set to: {Fraction}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ ERROR: Failed to set faction for hero '{name}': {ex.Message}");
                Fraction = NftFraction.Neutral; // Default fallback
            }

            // Set name with validation
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning($"⚠️ WARNING: Hero name is null/empty, using default");
                Name = "Unknown Hero";
            }
            else
            {
                Name = name;

                // Handle special case
                if (string.Equals(name, "age of tanks", StringComparison.OrdinalIgnoreCase))
                {
                    Name = "Warpath";
                    Debug.Log($"🦸 DEBUG: Renamed 'age of tanks' to 'Warpath'");
                }
            }

            Debug.Log($"🦸 DEBUG: Hero name set to: '{Name}'");

            // Enhanced sprite loading with comprehensive error handling
            try
            {
                Debug.Log($"🎨 DEBUG: Attempting to load sprite for hero '{Name}'");

                if (UIInventory.instance == null)
                {
                    Debug.LogWarning($"⚠️ WARNING: UIInventory.instance is null, cannot load sprite for '{Name}'");
                    Visualization = null;
                }
                else
                {
                    Visualization = UIInventory.LoadIcon(Name.ToLower());

                    if (Visualization != null)
                    {
                        Debug.Log($"✅ SUCCESS: Sprite loaded successfully for hero '{Name}'");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ WARNING: No sprite found for hero '{Name}', but continuing...");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ ERROR: Failed to load sprite for hero '{Name}': {ex.Message}");
                Debug.LogError($"❌ ERROR: Sprite loading stack trace: {ex.StackTrace}");
                Visualization = null; // Set to null and continue
            }

            Debug.Log($"✅ SUCCESS: NftHero '{Name}' created successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ ERROR: Critical failure creating NftHero '{name}': {ex.Message}");
            Debug.LogError($"❌ ERROR: Constructor stack trace: {ex.StackTrace}");

            // Set safe defaults
            Name = name ?? "Unknown Hero";
            Fraction = NftFraction.Neutral;
            Visualization = null;

            throw; // Re-throw to be caught by the calling code
        }
    }

    public HeroBoostedStats ConvertToBoostedStats()
    {
        return new HeroBoostedStats(Attributes);
    }

    public void Boost(float multiplier)
    {
        Attributes.Anonymity = ((ObscuredInt)(Attributes.Anonymity + (Attributes.Anonymity * multiplier)));
        Attributes.Innovation = ((ObscuredInt)(Attributes.Innovation + (Attributes.Innovation * multiplier)));
        Attributes.Security = ((ObscuredInt)(Attributes.Security + (Attributes.Security * multiplier)));
    }
}

public class HeroBoostedStats
{
    public ObscuredFloat PlayerSpeedFactor;
    public ObscuredFloat CooldownReductionFactor;
    public ObscuredInt ShieldAddition;
    public ObscuredFloat MaxHealthFactor;
    public float EpicPerkDropChanceAddition;

    public CardAttributes ClampedAttributes;

    private const float PlayerSpeedFactorMax = 1.1f; // 100% + 10
    private const float CooldownReductionFactorMax = 0.08f;
    private const int ShieldAdditionMax = 60;
    private const float MaxHealthFactorMax = 1.55f;
    private const int AttributeMax = 100;
    private const float EpicPerkProbMax = 0.03f;

    private HeroBoostedStats() { }

    public HeroBoostedStats(CardAttributes attrs)
    {
        ClampedAttributes = new CardAttributes();

        var clampedAno = Mathf.Clamp(attrs.Anonymity, 0, 300);
        var clampedInn = Mathf.Clamp(attrs.Innovation, 0, 300);
        var clampedSec = Mathf.Clamp(attrs.Security, 0, 300);

        var clampedPower = 0;

        if (attrs.Power > 0)
            clampedPower = Mathf.Clamp(attrs.Power, 400, 1200);

        if (attrs.IsRevolution)
        {
            clampedAno = Mathf.Clamp(Mathf.RoundToInt(clampedAno + (attrs.Anonymity - 100)), 100, 200);
            clampedInn = Mathf.Clamp(Mathf.RoundToInt(clampedInn + (attrs.Innovation - 100)), 100, 200);
            clampedSec = Mathf.Clamp(Mathf.RoundToInt(clampedSec + (attrs.Security - 100)), 100, 200);
        }

        ClampedAttributes.Innovation = clampedInn;
        ClampedAttributes.Security = clampedSec;
        ClampedAttributes.Power = clampedPower;
        ClampedAttributes.Anonymity = clampedAno;

        Scale(clampedAno, clampedInn, clampedSec, clampedPower);
    }

    private void Scale(int ano, int inn, int sec, int power)
    {
        PlayerSpeedFactor = Remap(ano, 0, AttributeMax, 1, PlayerSpeedFactorMax);
        CooldownReductionFactor = 1 - Remap(inn, 0, AttributeMax, 0, CooldownReductionFactorMax);
        ShieldAddition = Mathf.RoundToInt(Remap(sec, 0, AttributeMax, 0, ShieldAdditionMax)); // added to the base shield, also shield should be refilled

        if (power == 0)
        {
            MaxHealthFactor = 1;
            EpicPerkDropChanceAddition = 0;
        }
        else
        {
            EpicPerkDropChanceAddition = Remap(power, 400, 1200, 0.03f, EpicPerkProbMax);
            MaxHealthFactor = Remap(power, 400, 1200, 1.33f, MaxHealthFactorMax);
        }
    }

    internal HeroBoostedStats Clone()
    {
        var ret = new HeroBoostedStats
        {
            PlayerSpeedFactor = this.PlayerSpeedFactor,
            CooldownReductionFactor = this.CooldownReductionFactor,
            ShieldAddition = this.ShieldAddition,
            MaxHealthFactor = this.MaxHealthFactor,
            EpicPerkDropChanceAddition = this.EpicPerkDropChanceAddition,
            ClampedAttributes = this.ClampedAttributes,
        };

        return ret;
    }

    public static float Remap(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }

    public void Join(WeaponBoostedStats weapStats)
    {
        PlayerSpeedFactor = Remap(ClampedAttributes.Anonymity + weapStats.ClampedAttributes.Anonymity, 0, AttributeMax, 1, PlayerSpeedFactorMax);
        CooldownReductionFactor = 1 - Remap(ClampedAttributes.Innovation + weapStats.ClampedAttributes.Innovation, 0, AttributeMax, 0, CooldownReductionFactorMax);
        ShieldAddition = Mathf.RoundToInt(Remap(ClampedAttributes.Security + weapStats.ClampedAttributes.Security, 0, AttributeMax, 0, ShieldAdditionMax));
    }

    public void Unjoin(WeaponBoostedStats weapStats)
    {
        PlayerSpeedFactor = Remap(ClampedAttributes.Anonymity - weapStats.ClampedAttributes.Anonymity, 0, AttributeMax, 1, PlayerSpeedFactorMax);
        CooldownReductionFactor = 1 - Remap(ClampedAttributes.Innovation - weapStats.ClampedAttributes.Innovation, 0, AttributeMax, 0, CooldownReductionFactorMax);
        ShieldAddition = Mathf.RoundToInt(Remap(ClampedAttributes.Security - weapStats.ClampedAttributes.Security, 0, AttributeMax, 0, ShieldAdditionMax));
    }
}