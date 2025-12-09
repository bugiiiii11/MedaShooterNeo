# MedaShooterNeo - Game Balance Documentation

This document outlines all modifiable game mechanics for balancing purposes.

---

## Table of Contents
1. [Player Stats System](#1-player-stats-system)
2. [NFT System & Stat Modifications](#2-nft-system--stat-modifications)
3. [Weapon System](#3-weapon-system)
4. [Abilities System](#4-abilities-system)
5. [Perks System](#5-perks-system)
6. [Difficulty Scaling](#6-difficulty-scaling)
7. [Enemy Configuration](#7-enemy-configuration)
8. [Game Constants & Modifiers](#8-game-constants--modifiers)
9. [Scoring System](#9-scoring-system)
10. [Clamping Limits](#10-clamping-limits)
11. [File Locations](#11-file-locations)

---

## 1. Player Stats System

**File:** `Assets/Scripts/Player/PlayerBaseStats.cs`

### Base Player Stats
| Stat | Description |
|------|-------------|
| Health | Base hit points (modified by NFT and perks) |
| Shield | Armor/shield value (added via NFT bonuses) |
| FireRatePerc | Fire rate percentage modifier |
| CritChancePerc | Critical hit chance percentage |
| MovementSpeed | Base movement speed |
| Damage Range | Min-max damage per shot (Vector2Int) |
| CritDamage Range | Min-max critical damage bonus (Vector2Int) |

### Player Stats Module
| Property | Description |
|----------|-------------|
| CurrentHp / MaxHp | Current and maximum hit points |
| CurrentArmor / MaxArmor | Armor absorption system |
| HitPointsMultiplier | Stacking multiplier from perks |
| CriticalChanceIncreaseFromPerks | Direct crit chance from perk stacking |

---

## 2. NFT System & Stat Modifications

### 2.1 NFT Hero Stats

**File:** `Assets/Scripts/NFT/NftHero.cs`

| Attribute | Base Range | Effect | Output Range |
|-----------|------------|--------|--------------|
| Security | 0-300 | Shield Addition | 0-60 shields |
| Anonymity | 0-300 | Player Speed Factor | 1.0x - 1.1x |
| Innovation | 0-300 | Cooldown Reduction | 0% - 8% reduction |
| Power | 400-1200 | Max Health Factor | 1.33x - 1.55x |
| Power | 400-1200 | Epic Perk Drop Chance | +0.03 |

**Revolution NFTs:** Get +100 to each attribute (clamped to 200 max)

**Scaling Formula:**
```
Output = (Value - FromMin) / (FromMax - FromMin) * (ToMax - ToMin) + ToMin
```

### 2.2 NFT Weapon Stats

**File:** `Assets/Scripts/NFT/NftWeapon.cs`

| Attribute | Base Range | Effect | Output Range |
|-----------|------------|--------|--------------|
| Anonymity | 0-300 | Critical Chance Increase | 0 - 2.5% |
| Innovation | 0-300 | Damage Factor | 0 - 7.5% |
| Security | 0-300 | Fire Rate Increase | 0 - 1.5% |

### Special Weapon Effects

| Weapon | Special Effect |
|--------|---------------|
| Ryoshi Katana | Reflect projectiles (5% duration cost), high movement speed |
| Gladiator's Greatsword | High damage, slow attack, absorbs projectiles (15% duration per hit) |
| Blessed Blade | Reflect projectiles, high dodge chance |
| Tactician's Claymore | Disarm all enemies, mid damage/speed |
| Viper | High-speed assault rifle |
| Adept's Repeater | Very high speed, close-range |
| Underdog Meda-Gun | Shotgun-like (3 projectiles) |
| Sandcrawler's Sniper Rifle | Auto-aiming |

---

## 3. Weapon System

**File:** `Assets/Scripts/Player/Weapon.cs`

### Weapon Types
1. Pistol
2. Assault
3. AssaultPlasma
4. AssaultLaser
5. ShotgunLaser (fires 3 projectiles with 350 offset)
6. Sniper (auto-aiming)
7. Sword (melee)
8. PowerfulSword (melee variant)

### Weapon Properties
| Property | Description |
|----------|-------------|
| DamageRange | Min-max damage (Vector2) |
| FireRate | Time between shots (seconds) |
| ProjectileForce | Launch velocity |
| ProjectileOffset | Positional offset (Vector2) |
| ProjectileRotation | Random rotation range (-0.05 to 0.05) |
| ProjectileLifeTime | Duration range (0.1-0.15s default) |
| ProjectileBaseScale | Scale multiplier (1.0-2.0x) |
| ProjectileCloseRange | Minimum distance for close combat |

### Damage Calculation

**File:** `Assets/Scripts/Combat/Projectiles/ProjectileData.cs`

```
Base Damage = DamageRange.Random() + weapon upgrades

Critical Check:
- Base critical chance × CriticalChanceProbabilityMultiplier
- + OverallCritChanceIncrease (max 0.35 from perks)
- + CriticalChanceIncreaseFromPerks

Critical Damage = CriticalChanceBonusDamage × CriticalChanceDamageMultiplier
```

---

## 4. Abilities System

**File:** `Assets/Scripts/Player/AbilityConfig.cs`

### Base Ability Properties
| Property | Default Value |
|----------|---------------|
| Cooldown | 60 seconds |
| ActivatedTime | 7 seconds |
| UseCooldownReduction | true |

**Computed Cooldown:** `Cooldown - GameConstants.UltimateCooldownReduction`

### 4.1 OhShit Ability (Shield/Invulnerability)
- **Type:** Active Timed
- **Duration:** 7 seconds
- **Effects:**
  - Player becomes immune to damage
  - Shield visual with alpha tween
  - Player color changes to light blue (134, 215, 255)

### 4.2 DeepWound Ability (Weapon Modification)
- **Type:** Active Timed
- **Duration:** 7 seconds
- **Effects:**
  - Enables deep wound effect on attacks
  - Activates special shot VFX
  - Modifies projectile behavior

### 4.3 ChainButton Ability (Chain Gun)
- **Type:** Active Timed
- **Duration:** 7 seconds
- **Effects:**
  - Enables chain gun mode
  - Special continuous fire pattern

### 4.4 Snap Ability (Mass Kill)
- **Type:** Active (No duration)
- **Effects:**
  - Cannot be used during boss fight
  - Kills 50% of current enemies on field
  - Triggers snap explosion effects
  - No cooldown reduction applied

---

## 5. Perks System

**File:** `Assets/Scripts/PerkDatabaseAsset.cs`

### Perk Rarities
- Basic
- Rare
- Epic

### Passive Perks (Permanent, Stackable)

| Perk Name | Amount | Max Stacks | Max Effect | GameConstant Modified |
|-----------|--------|------------|------------|----------------------|
| Adrenaline Boost | +0.02 crit | 17 | +35% crit | `OverallCritChanceIncrease` |
| CritDamage Increase | +Amount | Unlimited | - | `CriticalChanceIncreaseFromPerks` |
| Dodge Chance | +0.03 | 7 | 20% dodge | `DodgeChance` |
| Instant Kill | +0.03 | 7 | 20% instakill | `InstantKillChance` |
| Health Increase | +2% MaxHP | Unlimited | - | `HitPointsMultiplier` |
| Hunger | +2% MaxHP | 10 | +20% HP | Direct MaxHp addition |
| Energy Injection | +2 CD reduction | 10 | -20s cooldown | `UltimateCooldownReduction` |
| Lock And Load | +2s weapon dur | 8 | +16s duration | `AdditionalWeaponPowerupDuration` |
| Nano Injection | +2% fire rate | 10 | +20% fire rate | `PermanentFireRateMultiplier` |
| Scrapper | +3% shield drop | 4 | 10% drop chance | `EnemyDropShieldChance` |

### Active Timed Perks (Duration-Based)

| Perk Name | Duration | Effect | GameConstant Modified |
|-----------|----------|--------|----------------------|
| Invincible Barrier | 30s | Full invincibility | `IsPlayerInvincible = true` |
| Slow Time | 30s | 30% slower projectiles | `EnemyProjectileSpeedMultiplier = 0.7` |
| Critical Situation | 30s | Guaranteed crits | `CriticalChanceProbabilityMultiplier = 100` |
| Panic Attack | 30s | 3x fire rate | `FireRateMultiplier = 0.333` |
| TNT | 30s | Explosive kills | `IsTntActive = true` |
| Fallen Angel | 30s | Special effect | `IsFallenAngelActive = true` |
| Explosive Gold | 30s | 3x coin drops | `CoinDropAmountMultiplier = 3` |
| Vampire Bullets | 30s | Heal 10% damage dealt | Direct healing on hit |
| Killing Spree | 30s | Uninterruptible spree | `CanInterruptKillingSpree = false` |
| Heartless | 30s | Extra enemy spawns | Spawns heartless spawner |

---

## 6. Difficulty Scaling

**File:** `Assets/Scripts/Player/DifficultyScaling.cs`

### Scaling Formulas
```
PlayerDamageFactor(wave) = wave^(3/2.3)
EnemyDamageFactor(wave)  = wave^(3/2.02)   // Scales faster than player!
PlayerCritFactor(wave)   = wave^(3/2.4)
```

### Example Scaling Values

| Wave | Player Damage | Enemy Damage | Player Crit |
|------|---------------|--------------|-------------|
| 1 | 1.0x | 1.0x | 1.0x |
| 5 | ~3.5x | ~4.5x | ~3.1x |
| 10 | ~7.5x | ~11.8x | ~6.8x |
| 15 | ~12.5x | ~24.5x | ~11.0x |
| 20 | ~18.5x | ~42.0x | ~15.5x |

**Note:** Enemy damage scales faster than player damage as waves progress!

---

## 7. Enemy Configuration

**File:** `Assets/Scripts/Waves/EnemyWavesProfile.cs`

### Enemy Properties
```csharp
class Enemy {
    DamageRange: Vector2Int     // Min-max damage
    HitPointsRange: Vector2Int  // Min-max HP
    RewardPoints: int           // Score when killed
    ProbabilityInWave: float    // Spawn likelihood (0-1)
    AdditionalAttackData        // Projectile configuration
}
```

### Enemy Types
| Type | Description |
|------|-------------|
| BasicEnemy | Standard grunt |
| StraightShootingEnemy | Direct fire |
| RoundedShootingEnemy | Circular fire pattern |
| SniperEnemy | Long-range shots |
| TripleShootingEnemy | 3-way fire |
| Mine | Proximity explosive |
| MinibossAdvancedShooter | Complex attack patterns |
| MinibossMissiler | Missile launcher |
| MinibossSniper | Advanced sniper |
| Boss (FlailBoss) | End-wave boss |

### Wave Configuration
| Property | Description |
|----------|-------------|
| MaxEnemyCount | Concurrent enemies on screen |
| EnemyQuantity | Total spawns before wave ends |
| SpawnCooldownRange | Min-max seconds between spawns |
| IsBoss | Boss wave flag |
| IsSilent | Doesn't trigger events/perks |
| SpawnMines | Enables mine spawning |
| MineSpawnCooldownRange | Mine spawn timing |

### Current Boss Scores
| Boss | Wave Profile | Score |
|------|--------------|-------|
| Main Boss | DefaultEnemyWaveProfile | 500 pts |
| Miniboss #1 | UnendingWavesProfile (Wave 15) | 1000 pts |
| Miniboss #2 | UnendingWavesProfile (Wave 20) | 2500 pts |
| Miniboss #3 | UnendingWavesProfile (Wave 25) | 5000 pts |

---

## 8. Game Constants & Modifiers

**File:** `Assets/Scripts/Managers/GameConstants.cs`

### Temporary Modifiers (Reset per run)

| Constant | Type | Default | Description |
|----------|------|---------|-------------|
| GameSpeedMultiplier | float | 1.0 | Global game speed |
| EnemyProjectileSpeedMultiplier | float | 1.0 | Slow time effect (0.7 with perk) |
| CoinDropAmountMultiplier | int | 1 | Coin multiplier (3 with Explosive Gold) |
| IsPlayerInvincible | bool | false | Invincibility state |
| FireRateMultiplier | float | 1.0 | Fire rate modifier (0.333 = 3x faster) |
| CriticalChanceProbabilityMultiplier | float | 1.0 | Crit chance modifier (100 = guaranteed) |
| CriticalChanceDamageMultiplier | int | 1 | Crit damage multiplier |
| IsTntActive | bool | false | TNT perk state |
| IsFallenAngelActive | bool | false | Fallen Angel state |
| CanInterruptKillingSpree | bool | true | Spree interrupt flag |
| DisarmEnemy | bool | false | Tactician's Claymore effect |

### Permanent Modifiers (Persist through run)

| Constant | Type | Default | Max | Description |
|----------|------|---------|-----|-------------|
| PermanentFireRateMultiplier | float | 0 | 0.2 | Nano Injection bonus |
| KillingSpreeMultiplier | float | 1.0 | - | Spree score multiplier |
| DodgeChance | float | 0 | 0.2 | Dodge probability |
| AdditionalWeaponPowerupDuration | int | 0 | 16 | Extra weapon duration |
| OverallCritChanceIncrease | float | 0 | 0.35 | Adrenaline crit bonus |
| InstantKillChance | float | 0 | 0.2 | Instant kill probability |
| UltimateCooldownReduction | int | 0 | 20 | Ability cooldown reduction |
| EnemyDropShieldChance | float | 0 | 0.1 | Shield drop probability |

---

## 9. Scoring System

**File:** `Assets/Scripts/Utility/GameStats.cs`

### Tracked Statistics
| Stat | Description |
|------|-------------|
| EnemiesSpawned | Total enemies spawned |
| EnemiesKilled | Total kills |
| WavesCount | Waves completed |
| DistanceTraveled | Cumulative movement |
| PerksCollected | Total perks collected |
| ShieldsCollected | Total shields collected |
| AbilityUseCount | Ability activations |
| MaxScorePerEnemy | Highest single kill value |
| MaxScorePerEnemyScaled | Scaled by difficulty |
| LongestKillingSpreeMult | Best spree multiplier |
| LongestKillingStreeDuration | Longest spree duration |
| MaxKillingSpree | Max consecutive kills |
| EnemiesKilledWhileKillingSpree | Kills during spree |

### Point Mechanics
- Each enemy has a `RewardPoints` value
- Points dispatch via `RewardScoreEvent`
- Killing spree applies multiplier to points
- Enemy escape results in negative points (penalty)

---

## 10. Clamping Limits

| System | Min | Max | Notes |
|--------|-----|-----|-------|
| Dodge Chance | 0 | 20% | Hard cap |
| Instant Kill Chance | 0 | 20% | Hard cap |
| Crit Increase (Adrenaline) | 0 | 35% | Hard cap |
| Fire Rate Reduction (Nano) | 0 | 20% | Hard cap |
| Shield Bonus (Security NFT) | 0 | 60 | Hard cap |
| Speed Multiplier (Anonymity) | 1.0x | 1.10x | 10% max boost |
| Cooldown Reduction (Innovation) | 0 | 8% | Hard cap |
| Health Multiplier (Power NFT) | 1.0x | 1.55x | 55% max boost |
| Weapon Damage Factor | 0 | 7.5% | Hard cap |
| Weapon Crit Increase | 0 | 2.5% | Hard cap |
| Weapon Fire Rate | 0 | 1.5% | Hard cap |

---

## 11. File Locations

### Core Scripts
| System | File Path |
|--------|-----------|
| Player Stats | `Assets/Scripts/Player/PlayerBaseStats.cs` |
| Player Movement | `Assets/Scripts/Player/PlayerMovement.cs` |
| NFT Hero | `Assets/Scripts/NFT/NftHero.cs` |
| NFT Weapon | `Assets/Scripts/NFT/NftWeapon.cs` |
| Weapons | `Assets/Scripts/Player/Weapon.cs` |
| Weapon Controller | `Assets/Scripts/Player/WeaponController.cs` |
| Game Constants | `Assets/Scripts/Managers/GameConstants.cs` |
| Difficulty Scaling | `Assets/Scripts/Player/DifficultyScaling.cs` |
| Projectile Data | `Assets/Scripts/Combat/Projectiles/ProjectileData.cs` |

### Perks (in `Assets/Scripts/Perks/`)
- AdrenalineBoostPerk.cs
- CritDamageIncreasePerk.cs
- CriticalSituationPerk.cs
- DodgeChancePerk.cs
- EnergyInjectionPerk.cs
- ExplosiveGoldPerk.cs
- FallenAngelPerk.cs
- HeartlessPerk.cs
- HealthIncreasePerk.cs
- HungerPerk.cs
- InstantKillPerk.cs
- InvincibleBarrierPerk.cs
- KillingSpreePerk.cs
- LockAndLoadPerk.cs
- NanoInjectionPerk.cs
- PanicAttackPerk.cs
- ScrapperPerk.cs
- SlowTimePerk.cs
- TntPerk.cs
- VampireBulletsPerk.cs

### Abilities
| Ability | File |
|---------|------|
| OhShit | `Assets/Scripts/Player/OhShitAbility.cs` |
| DeepWound | `Assets/Scripts/Player/DeepWoundAbility.cs` |
| ChainButton | `Assets/Scripts/Player/ChainButtonAbility.cs` |
| Snap | `Assets/Scripts/Player/SnapAbility.cs` |

### Enemy & Wave Data
| Data | File Path |
|------|-----------|
| Enemy Base | `Assets/Scripts/Combat/Enemies/BasicEnemy.cs` |
| Wave Profiles | `Assets/Scripts/Waves/EnemyWavesProfile.cs` |
| Default Waves | `Assets/Data/DefaultEnemyWaveProfile.asset` |
| Endless Waves | `Assets/Data/UnendingWavesProfile.asset` |

---

## Modifier Interaction Chain

```
DAMAGE CHAIN:
Base Damage
  → Weapon NFT Boost (Innovation: +7.5% max)
  → Difficulty Scaling (wave-based multiplier)
  → Critical Hit Check
  → Final Damage

SPEED CHAIN:
Base Speed
  → Hero NFT Boost (Anonymity: +10% max)
  → Perk Multipliers
  → Final Speed

FIRE RATE CHAIN:
Base Fire Rate
  → Hero NFT (Security: +1.5% max)
  → Nano Injection (up to -20%)
  → Panic Attack (x3 during perk)
  → Final Fire Rate

COOLDOWN CHAIN:
Base Cooldown (60s)
  → Hero NFT (Innovation: -8% max)
  → Energy Injection (-20s max)
  → Final Cooldown

HEALTH CHAIN:
Base MaxHP
  → Hero NFT (Power: 1.33-1.55x)
  → Health Increase Perk (% stacking)
  → Hunger Perk (direct addition)
  → Final MaxHP

CRITICAL HIT CHAIN:
1. Base Critical Chance
2. × CriticalChanceProbabilityMultiplier
3. + OverallCritChanceIncrease (max 0.35)
4. + CriticalChanceIncreaseFromPerks
5. If hit: damage × CriticalChanceDamageMultiplier
```

---

*Last updated: December 2, 2025*
