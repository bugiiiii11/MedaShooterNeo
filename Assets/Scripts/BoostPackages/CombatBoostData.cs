using Newtonsoft.Json;
using System;
using UnityEngine;

/// <summary>
/// Response from /api/boosts/active endpoint
/// </summary>
[Serializable]
public class CombatBoostResponse
{
    [JsonProperty("hasActiveBoost")]
    public bool HasActiveBoost { get; set; }

    [JsonProperty("boost")]
    public CombatBoost Boost { get; set; }
}

/// <summary>
/// Combat boost details with expiration and effects
/// </summary>
[Serializable]
public class CombatBoost
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("activatedAt")]
    public string ActivatedAt { get; set; }

    [JsonProperty("expiresAt")]
    public string ExpiresAt { get; set; }

    [JsonProperty("remainingSeconds")]
    public int RemainingSeconds { get; set; }

    [JsonProperty("effects")]
    public BoostEffects Effects { get; set; }
}

/// <summary>
/// Boost effects: multipliers and bonuses to apply to player stats
/// </summary>
[Serializable]
public class BoostEffects
{
    /// <summary>
    /// Damage multiplier (e.g., 1.3 = +30% damage)
    /// Apply as: baseDamage * damageMultiplier
    /// </summary>
    [JsonProperty("damageMultiplier")]
    public float DamageMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Fire rate multiplier (e.g., 1.15 = +15% fire rate = faster shooting)
    /// Apply as: baseFireRate * fireRateMultiplier
    /// Note: Lower fire rate value = faster shooting, so use division if needed
    /// </summary>
    [JsonProperty("fireRateMultiplier")]
    public float FireRateMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Critical chance bonus (e.g., 0.07 = +7% crit chance)
    /// Apply as: baseCrit + critBonus
    /// </summary>
    [JsonProperty("critBonus")]
    public float CritBonus { get; set; } = 0.0f;
}
