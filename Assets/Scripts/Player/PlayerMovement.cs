using System;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Events;
using UnityEngine;

public enum ChangedStatsEventType
{
    Armor,
    Hitpoints,
    Currencies
}

public class PlayerMovement : MonoBehaviour
{
    public Vector2 BoundaryX;
    public float speedX = 2f, speedY = 4;
    private Transform _transform;
    public SpriteRenderer BoundaryRenderer;
    public Animator _animator;
    public PlayerBaseStats BaseStats;
    public PlayerStatsModule PlayerStats;
    public WeaponController WeaponController;
    public UpgradeModule PlayerUpgrades;

    public ObscuredBool IsImmuneToDamage = false;
    public ObscuredBool IsChainGunActive = false;
    public ObscuredBool IsDeepWoundActive = false;

    public GameObject MirrorVfx;
    private float lastY = 0, lastChange = 0;

    public bool InvincibleFromWeapon = false;
    public bool MirrorBullets = false;
    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        // update stats
        BaseStats = Instantiate(BaseStats);
        HeroBoostedStats boostedStats = null;
        WeaponBoostedStats weaponStats = null;
        WeaponController = GetComponent<WeaponController>();
        if (PlayerProfileInfo.instance)
        {
            boostedStats = (PlayerProfileInfo.instance.EquippedHero as NftHero)?.ConvertToBoostedStats();
            weaponStats = (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();

            if(PlayerProfileInfo.instance.IsUserFarmer)
            {
                GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, "<size=80>Farming bonus</size>\n<b>Choose your perk!<b>"));
            }
        }

        if (boostedStats != null && weaponStats != null)
        {
            boostedStats.Join(weaponStats);
            weaponStats.Join(boostedStats);
        }

        if (PlayerProfileInfo.instance != null && PlayerProfileInfo.instance.EquippedHero != null && boostedStats != null)
        {
            speedX *= boostedStats.PlayerSpeedFactor;
            speedY *= boostedStats.PlayerSpeedFactor;
            BaseStats.UpdateStats(boostedStats);

            UIPerkManager.instance.PerkDatabase = Instantiate(UIPerkManager.instance.PerkDatabase);
            UIPerkManager.instance.PerkDatabase.RarityProbabilities[Rarity.Epic] += boostedStats.EpicPerkDropChanceAddition;

            GetComponent<AbilitySwitcher>().SetAbilityCooldownFactor(boostedStats.CooldownReductionFactor);

            if (PlayerProfileInfo.instance.EquippedHero.Name == "Zombie Chad")
            {
                GetComponent<F3DCharacterAvatar>().SkinUsed = F3DCharacterAvatar.SkinName.Zombiechad;
                GetComponent<F3DCharacterAvatar>().SwitchToChar();
            }
        }

        if (PlayerProfileInfo.instance != null && PlayerProfileInfo.instance.EquippedWeapon != null && weaponStats != null)
        {
            BaseStats.UpdateStats(weaponStats);
        }

        PlayerStats.SetFrom(BaseStats);
        PlayerStats.Upgrade(PlayerUpgrades);

        SendChangedStatsEvents();
        GameManager.instance.EventManager.AddListener<NextWaveEvent>(OnNextWaveSpawned);
        GameManager.instance.EventManager.AddListener<PowerupCollectedEvent>(OnPowerupCollected);
        GameManager.instance.EventManager.AddListener<DroppableCollectedEvent>(OnDroppableCollected);
        GameManager.instance.EventManager.AddListener<EnemyKilledEvent>(OnEnemyKilled);

        // set main weapon
        var weap = WeaponController.GetCurrentWeapon();
        weap.FireRate = Mathf.Clamp(BaseStats.FireRatePerc, 0.3f, 5);
        weap.DamageRange = BaseStats.Damage;
        weap.Data.AdditionalData.CriticalChance = BaseStats.CritChancePerc;

        // set other weapons
        if (weaponStats != null)
        {
            foreach (var weapon in WeaponController.GetAllWeapons())
            {
                //var weap = WeaponController.GetCurrentWeapon();
                weapon.FireRate -= weaponStats.FireRateIncrease * 0.5f;
                weapon.FireRate = Mathf.Clamp(weapon.FireRate, 0.1f, 5);
                weapon.DamageRange += weaponStats.DamageFactor * 0.9f * weapon.DamageRange;
                weapon.Data.AdditionalData.CriticalChance += weaponStats.CriticalChanceIncrease * 0.8f;
            }
        }

        defaultSpeed = new Vector2(speedX, speedY);
        //weap.Data.AdditionalData.CricitalChanceBonusDamage = BaseStats.CritDamage;
    }

    internal void MirroredBullet()
    {
        WeaponController.Mirrored();
    }

    internal Vector2 defaultSpeed;
    internal void ResetSpeed()
    {
        if (WeaponController.GetCurrentWeapon().TypeOfWeapon != WeaponType.Sword && WeaponController.GetCurrentWeapon().TypeOfWeapon != WeaponType.PowerfulSword)
        {
            speedX = defaultSpeed.x;
            speedY = defaultSpeed.y;
        }
    }

    private void OnEnemyKilled(EnemyKilledEvent ev)
    {
        GameManager.instance.GameStats.EnemiesKilled++;
    }

    public void OnNextWaveSpawned(NextWaveEvent wave)
    {
        GameManager.instance.GameStats.WavesCount++;

        if (wave.IsSilent)
            return;

        // upgrade player and send events
        PlayerStats.Upgrade(PlayerUpgrades);
        SendChangedStatsEvents();
    }

    public void OnPowerupCollected(PowerupCollectedEvent powerupEvent)
    {
        powerupEvent.Powerup.ApplyPowerup(PlayerStats);
        SendChangedStatsEvents();

        if (powerupEvent.Powerup is HealthRefillPowerup)
        {
            GetComponent<F3DCharacterAvatar>().TweenColor(new Color32(185, 255, 185, 255), 0.08f);
        }
    }

    public void OnDroppableCollected(DroppableCollectedEvent powerupEvent)
    {
        powerupEvent.Drop.ApplyDroppable(PlayerStats);
        SendChangedStatsEvents();
    }

    private void SendChangedStatsEvents()
    {
        GameManager.instance.EventManager.Dispatch(new PlayerHealthChangeEvent(PlayerStats.CurrentHp, PlayerStats.MaxHp));
        GameManager.instance.EventManager.Dispatch(new PlayerArmorChangeEvent(PlayerStats.CurrentArmor, PlayerStats.MaxArmor));
        GameManager.instance.EventManager.Dispatch(new PlayerCurrenciesChangeEvent(PlayerStats.Currencies));

        GameManager.instance.EventManager.AddListener<GamePauseEvent>(OnGamePaused);
    }

    private void OnGamePaused(GamePauseEvent obj)
    {
        if (obj.IsPaused)
        {
            _animator.SetFloat("Speed", 0f);
        }
    }

    private void SendChangedStatsEvent(ChangedStatsEventType type)
    {
        switch (type)
        {
            case ChangedStatsEventType.Armor:
                GameManager.instance.EventManager.Dispatch(new PlayerArmorChangeEvent(PlayerStats.CurrentArmor, PlayerStats.MaxArmor));
                break;
            case ChangedStatsEventType.Hitpoints:
                GameManager.instance.EventManager.Dispatch(new PlayerHealthChangeEvent(PlayerStats.CurrentHp, PlayerStats.MaxHp));
                break;
            case ChangedStatsEventType.Currencies:
                GameManager.instance.EventManager.Dispatch(new PlayerCurrenciesChangeEvent(PlayerStats.Currencies));
                break;
        }
    }

    public void InstantKillPlayer()
    {
        if (isDead)
            return;

        // died
        PlayerStats.SetHp(0);

        OneShotAudioPool.SpawnOneShot(LevelProps.instance.DeathPlayerSound);
        isDead = true;
        GameManager.instance.EventManager.Dispatch(new PlayerDiedEvent());
        _animator.SetFloat("Speed", 0f);
        _animator.SetBool("DeathBack", true);
        WeaponController.GetCurrentWeapon().gameObject.SetActive(false);

        SendChangedStatsEvents();
    }

    internal bool isDead = false;


    public void ReceiveDamage(int damage, bool ignoreDodge = false, bool isMelee = false)
    {
        if (GameManager.instance.GameConstants.IsPlayerInvincible)
            return;

        if (IsImmuneToDamage)
            return;

        if (!isMelee && InvincibleFromWeapon)
        {
            // take cooldown
            WeaponController.TakeFromCurrentWeapon(0.15f);
            return;
        }

        // check if we should dodge the attack
        if (!ignoreDodge)
        {
            var dodgeRandom = UnityEngine.Random.value;
            if (dodgeRandom < GameConstants.Constants.DodgeChance)
            {
                DamageTextSpawner.Spawn("<color=white>Dodge</color>", transform.position + new Vector3(0.5f,1.2f,0));
                return;
            }
        }

        if (PlayerStats.TakeDamage(damage, out var isArmorOnly))
        {
            // survived
            CameraShake.instance.SetShake(0.2f);

            //make sound
            OneShotAudioPool.SpawnOneShot(LevelProps.instance.HitShot1, 0.75f, 0.97f, 1);

            // send event if armor did not cover whole damage
            if (!isArmorOnly)
                GameManager.instance.EventManager.Dispatch<PlayerDamagedEvent>();
        }
        else
        {
            if (isDead)
                return;

            // died
            PlayerStats.SetHp(0);

            OneShotAudioPool.SpawnOneShot(LevelProps.instance.DeathPlayerSound);

            if (GameManager.instance.GameConstants.IsFallenAngelActive)
            {
                _animator.SetFloat("Speed", 0f);
                UIPerkManager.instance.CancelPerk(UIPerkManager.instance.CurrentlyAppliedPerks.Find(x => x is FallenAngelPerk));
                GameConstants.Constants.IsPlayerInvincible = true;
                CoroutineManager.InvokeAction(() => 
                {
                    GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, "<size=80>Revived by Fallen Angel</size>\n<b>Choose your perk!<b>"));
                    GameConstants.Constants.IsPlayerInvincible = false;
                }, 0.2f);
                GameManager.instance.GameConstants.IsFallenAngelActive = false;
                isDead = false;
                PlayerStats.SetHp(Mathf.CeilToInt(PlayerStats.MaxHp * 0.5f));
            }
            else
            {
                isDead = true;
                GameManager.instance.EventManager.Dispatch(new PlayerDiedEvent());
                _animator.SetFloat("Speed", 0f);
                _animator.SetBool("DeathBack", true);
                WeaponController.GetCurrentWeapon().gameObject.SetActive(false);
            }
        }

        SendChangedStatsEvents();
    }


    void Update()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        // draw bound
        var boundDistX = BoundaryX.y - _transform.position.x;
        var col = BoundaryRenderer.color;
        col.a = (2 - boundDistX) * 0.1f;
        col.a = Mathf.Clamp(col.a, 0, 0.1f);
        BoundaryRenderer.color = col;

        var vertical = SimpleInput.GetAxis("Vertical");
        var horiz = SimpleInput.GetAxis("Horizontal");

        var canGoVertical = true;
        var canGoHorizontal = true;

        if (!LevelInfo.instance.IsUnderTopBoundary(_transform.position) && vertical > 0)
            canGoVertical = false;

        if (!LevelInfo.instance.IsAboveBottomBoundary(_transform.position) && vertical < 0)
            canGoVertical = false;

        if (_transform.position.x <= BoundaryX.x && horiz < 0)
            canGoHorizontal = false;

        if (_transform.position.x >= BoundaryX.y && horiz > 0)
            canGoHorizontal = false;

        var goal = _transform.position + new Vector3(horiz, vertical).normalized;
        var horizAbs = Mathf.Abs(horiz);
        var vertAbs = Mathf.Abs(vertical);

        var x = Mathf.MoveTowards(_transform.position.x, goal.x, Time.deltaTime * speedX * horizAbs);
        var y = Mathf.MoveTowards(_transform.position.y, goal.y, Time.deltaTime * speedY * vertAbs);

        if (horizAbs > 0f && vertAbs > 0f)
        {
            var deltaX = x - _transform.position.x;
            var deltaY = y - _transform.position.y;
            var normalized = new Vector3(deltaX, deltaY).normalized * Time.deltaTime;
            x -= normalized.x;
            y -= normalized.y;
        }

        var distance = (Mathf.Abs(horiz * speedX) + Mathf.Abs(vertical * speedY)) * Time.deltaTime;
        GameManager.instance.GameStats.DistanceTraveled += distance;

        if (!canGoHorizontal)
            x = _transform.position.x;

        if (!canGoVertical)
            y = _transform.position.y;

        _transform.position = new Vector3(x, y, 0);


        if (!GameManager.instance.IsParallaxPaused || (vertical != 0 || horiz != 0))
        {
            _animator.SetFloat("Speed", 2.2f);
        }
        else
        {
            _animator.SetFloat("Speed", 0);
        }
        
        if (!Mathf.Approximately(lastY, _transform.position.y))
        {
            lastChange = Time.time;
            lastY = _transform.position.y;
        }
        else
        {
            if (Time.time - lastChange > 13.144f)
            {
                lastChange = Time.time;
                GameManager.instance.EnemySpawner.SpawnMine(lastY + UnityEngine.Random.Range(0.15f, 0.5f));
            }
        }
    }
}

[Serializable]
public class PlayerCurrencyModule
{
    public ObscuredInt CoinAmount = 0;

    public ObscuredInt NextPerkCoins = 5, NextRarePerksCoins = 15, PassiveAbilityBuildupCoins = 20;

    internal void AddCoins(int coinAddAmount)
    {
        CoinAmount += coinAddAmount;

        var amount = CoinAmount;
        var lastPerkCoins = NextPerkCoins;
        if (amount >= NextPerkCoins)
        {
            NextPerkCoins += 5;

            if (amount >= NextPerkCoins)
            {
                NextPerkCoins = amount + 5;
            }
        }

        var amountOfPerks = (NextPerkCoins - lastPerkCoins) / 5;
    
        if (amountOfPerks > 0)
        {
            // give perks
            if (amount % PassiveAbilityBuildupCoins==0)
            {
                GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, "<size=80>Passive strategy build</size>\n<b>Choose your perk!<b>", true));
                return;
            }
            if (amount % 15 == 0)
            {
                GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, new[] { Rarity.Rare, Rarity.Epic }));
            }
            else
            {
                GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3));
            }
        }

       /* if (CoinAmount % PassiveAbilityBuildupCoins == 0)
        {
            GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, "<size=80>Passive strategy build</size>\n<b>Choose your perk!<b>", true));

            NextPerkCoins = GameManager.instance.PerkSetup.BasicPerksBoundary - (CoinAmount % GameManager.instance.PerkSetup.BasicPerksBoundary) + CoinAmount;
            NextRarePerksCoins = GameManager.instance.PerkSetup.RarePerksBoundary - (CoinAmount % GameManager.instance.PerkSetup.RarePerksBoundary) + CoinAmount;
            return;
        }

        if (CoinAmount >= NextRarePerksCoins)
        {
            GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, new[] { Rarity.Rare, Rarity.Epic }));
        }
        else if (CoinAmount >= NextPerkCoins)
        {
            GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3));
        }
        NextPerkCoins = GameManager.instance.PerkSetup.BasicPerksBoundary - (CoinAmount % GameManager.instance.PerkSetup.BasicPerksBoundary) + CoinAmount;
        NextRarePerksCoins = GameManager.instance.PerkSetup.RarePerksBoundary - (CoinAmount % GameManager.instance.PerkSetup.RarePerksBoundary) + CoinAmount;*/

        /*if (CoinAmount % GameManager.instance.PerkSetup.RarePerksBoundary == 0)
        {
            GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, new[] { Rarity.Rare, Rarity.Epic }));
        }

        else if (CoinAmount % GameManager.instance.PerkSetup.BasicPerksBoundary == 0)
        {
            GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3));
        }*/
    }
}

[Serializable]
public class PlayerStatsModule
{
    [SerializeField]
    private ObscuredFloat realHp, realMaxHp;
    public ObscuredInt CurrentHp;

    public ObscuredInt MaxHp;

    public ObscuredInt CurrentArmor, MaxArmor;

    public StatsModifier Modifiers = new StatsModifier();
    public PlayerCurrencyModule Currencies = new PlayerCurrencyModule();

    public void SetNewMaxHp(int value)
    {
        MaxHp = value;
        realMaxHp = value;
    }

    internal void AddHp(int amount)
    {
        if(CurrentHp == MaxHp)
            return;
        else
        {
            if(CurrentHp + amount > MaxHp)
                CurrentHp = MaxHp;
            else
                CurrentHp += amount;
        }
    }

    internal void SetHp(int hp)
    {
        CurrentHp = hp > MaxHp ? MaxHp : hp;
    }

    internal void AddArmor(int amount)
    {
        if (CurrentArmor == MaxArmor)
            return;
        else
        {
            if (CurrentArmor + amount > MaxArmor)
                CurrentArmor = MaxArmor;
            else
                CurrentArmor += amount;
        }
    }

    internal bool TakeDamage(int damage, out bool armorOnly)
    {
        int remainingDamage = damage;
        if(CurrentArmor > 0)
        {
            CurrentArmor -= damage;
            
            if(CurrentArmor >= 0)
            {
                armorOnly = true;
                return true;
            }

            remainingDamage = Mathf.Abs(CurrentArmor);
            CurrentArmor = 0;
        }
        armorOnly = false;

        CurrentHp -= remainingDamage;
        
        if(CurrentHp <= 0)
        {
            CurrentHp = 0;
            realHp = 0;
            return false;
        }

        return true;
    }

    internal void Upgrade(UpgradeModule module)
    {
        var playerUpgrades = UpgradeModule.Scale(module, DifficultyScaling.CurrentDynamicFactor, DifficultyScaling.CurrentCriticalDynamicFactor);
        MaxHp = Mathf.RoundToInt(MaxHp + playerUpgrades.MaxHitPointsIncreasePerWave);
        realMaxHp += playerUpgrades.MaxHitPointsIncreasePerWave;
        MaxArmor = Mathf.RoundToInt(MaxArmor + playerUpgrades.MaxArmorIncreasePerWave);

        CurrentHp = Mathf.RoundToInt(CurrentHp + playerUpgrades.MaxHitPointsIncreasePerWave);
        realHp += playerUpgrades.MaxArmorIncreasePerWave;

        Modifiers.AllUpgrades = new List<WeaponUpgrade>(playerUpgrades.AllUpgrades);
       // Modifiers.CriticalChanceDamageIncrement = Mathf.RoundToInt(Modifiers.CriticalChanceDamageIncrement+playerUpgrades.CriticalDamageIncreasePerWave);

        // also do scaling by perks
        ScaleByPerks();
    }

    public void ScaleByPerks()
    {
       // var newScaledHp = Mathf.RoundToInt(realHp + realHp * Modifiers.Multiplier);
        MaxHp = Mathf.RoundToInt(realMaxHp + realMaxHp * Modifiers.HitPointsMultiplier);
    }

    internal void SetFrom(PlayerBaseStats baseStats)
    {
        realHp = realMaxHp = baseStats.Health;
        CurrentHp = MaxHp = baseStats.Health;

        MaxArmor = baseStats.Shield;

        if (baseStats.IsShieldActiveFromStart)
            CurrentArmor = Mathf.RoundToInt(MaxArmor * 0.5f);
    }
}

[Serializable]
public class UpgradeModule
{
    public float MaxHitPointsIncreasePerWave;
    public float MaxArmorIncreasePerWave;

    public WeaponUpgrade[] AllUpgrades;

    public static UpgradeModule Scale(UpgradeModule baseModule, float scaleFactor, float critScaleFactor)
    {
        var newModule = new UpgradeModule();
        newModule.AllUpgrades = new WeaponUpgrade[baseModule.AllUpgrades.Length];
        
        newModule.MaxHitPointsIncreasePerWave = Mathf.RoundToInt(baseModule.MaxHitPointsIncreasePerWave * scaleFactor);
        newModule.MaxArmorIncreasePerWave = Mathf.RoundToInt(baseModule.MaxArmorIncreasePerWave * scaleFactor);

        for (var i = 0; i < baseModule.AllUpgrades.Length; i++)
        {
            var oldWeap = baseModule.AllUpgrades[i];
            var newWeap = newModule.AllUpgrades[i];

            newWeap.WeaponType = oldWeap.WeaponType;

            newWeap.DamageIncreasePerWave = Mathf.RoundToInt(oldWeap.DamageIncreasePerWave * scaleFactor);
            newWeap.CriticalDamageIncreasePerWave = oldWeap.CriticalDamageIncreasePerWave * critScaleFactor;

            newModule.AllUpgrades[i] = newWeap;
        }

        return newModule;
    }
}

public enum WeaponType
{
    Unknown,
    Pistol,
    Assault,
    AssaultPlasma,
    AssaultLaser,
    ShotgunLaser,
    Sniper,
    Sword,
    PowerfulSword
}
[Serializable]
public struct WeaponUpgrade
{
    public WeaponType WeaponType;
    public ObscuredFloat DamageIncreasePerWave;
    public ObscuredFloat CriticalDamageIncreasePerWave;
}

public class StatsModifier
{
    public float HitPointsMultiplier { get; set; }
    public int CriticalChanceIncreaseFromPerks { get; set; } = 0;
    public List<WeaponUpgrade> AllUpgrades;
}