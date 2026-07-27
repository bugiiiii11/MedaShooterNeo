// 01

using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIInventory : Singleton<UIInventory>
{
    public enum PopulationType
    {
        Heroes,
        Weapons,
        Abilities,
        Stats
    }

    public Button[] Buttons;
    public UICardPreview Preview;
    public byte ColumnCount = 5;
    public RectTransform Container;
    public TMPro.TextMeshProUGUI TitleText, GetNewText;
    public TMPro.TextMeshProUGUI HeaderTitleText; // Header title at top of window
    public UIStats Stats;

    public Image GetNewImage;
    public Sprite GetNewHero, GetNewWeapon;
    public Sprite ShieldSprite; // Bright shield sprite (shown when user owns NFT Land)
    public Sprite StakingTechSprite; // "Staking Tech" sprite (shown when user doesn't own NFT Land)

    // Combat Boost indicator sprites
    public Sprite CombatBoostActiveSprite; // Active boost sprite (bright/glowing)
    public Sprite CombatBoostInactiveSprite; // Inactive boost sprite (grey/dimmed)

    // Farming Perk indicator sprites
    public Sprite PerkActiveSprite; // Active perk sprite (user has liquidity staked)
    public Sprite PerkInactiveSprite; // Inactive perk sprite (user doesn't have liquidity staked)

    public GameObject HeroIconPrefab, WeaponIconPrefab, SpaceFillerPrefab, SelectYourHeroPrefab, SelectYourWeaponPrefab, StakingAbility, FarmingAbility, ArrowDown;

    public GameObject CombatBoostIndicator; // UI element showing combat boost status

    public PopulationType PopulatedBy = PopulationType.Heroes;

    private void Start()
    {
        // load sprites
        LoadSprites();

        // Initialize StakingAbility with grey shield (default state until land ticket check completes)
        InitializeStakingAbilityAsGreyShield();

        // Initialize Combat Boost Indicator (inactive by default)
        InitializeCombatBoostIndicator();

        // Initialize Farming Perk Indicator (inactive by default)
        InitializeFarmingPerkIndicator();
    }

    /// <summary>
    /// Shows the StakingAbility button with "Staking Tech" sprite by default.
    /// The sprite will be swapped to bright shield if user owns NFT Land after the API check completes.
    /// </summary>
    public void InitializeStakingAbilityAsGreyShield()
    {
        if (StakingAbility != null)
        {
            StakingAbility.SetActive(true);
            var shieldImage = StakingAbility.GetComponent<Image>();
            if (shieldImage != null)
            {
                // Show "Staking Tech" sprite by default (user doesn't own NFT Land)
                if (StakingTechSprite != null)
                {
                    shieldImage.sprite = StakingTechSprite;
                }
                shieldImage.color = Color.white; // Full brightness, sprite itself shows the "inactive" state
            }
        }
    }

    /// <summary>
    /// Initializes the Combat Boost Indicator with inactive sprite by default.
    /// Will be updated when boost status is received from backend.
    /// </summary>
    public void InitializeCombatBoostIndicator()
    {
        if (CombatBoostIndicator != null)
        {
            CombatBoostIndicator.SetActive(true);

            // Force set inactive sprite immediately
            var boostImage = CombatBoostIndicator.GetComponent<Image>();
            if (boostImage != null && CombatBoostInactiveSprite != null)
            {
                boostImage.sprite = CombatBoostInactiveSprite;
                boostImage.color = Color.white;
                Debug.Log($"💊 InitializeCombatBoostIndicator: Set to INACTIVE sprite");
            }
            else
            {
                Debug.LogWarning($"💊 InitializeCombatBoostIndicator: boostImage={boostImage != null}, InactiveSprite={CombatBoostInactiveSprite != null}");
            }

            // Check PlayerProfileInfo boost status and sync with it
            bool hasBoost = PlayerProfileInfo.instance != null && PlayerProfileInfo.instance.HasActiveCombatBoost;
            Debug.Log($"💊 InitializeCombatBoostIndicator: PlayerProfileInfo.HasActiveCombatBoost = {hasBoost}");

#if UNITY_EDITOR
            // Warn if debug boost is enabled in editor
            if (PlayerProfileInfo.instance != null && PlayerProfileInfo.instance.UseDebugCombatBoost)
            {
                Debug.LogWarning($"💊 ⚠️ UseDebugCombatBoost is ENABLED in PlayerProfileInfo! This will show active boost indicator even without real boost data.");
            }
#endif

            UpdateCombatBoostIndicator(hasBoost);
        }
        else
        {
            Debug.LogWarning("💊 CombatBoostIndicator GameObject is not assigned in UIInventory!");
        }
    }

    /// <summary>
    /// Updates the Combat Boost Indicator based on boost status.
    /// Shows active sprite (bright) if boost is active, inactive sprite (grey) otherwise.
    /// </summary>
    public void UpdateCombatBoostIndicator(bool hasActiveBoost)
    {
        if (CombatBoostIndicator == null)
        {
            Debug.LogWarning("💊 Cannot update Combat Boost Indicator: GameObject is null");
            return;
        }

        var boostImage = CombatBoostIndicator.GetComponent<Image>();
        if (boostImage != null)
        {
            if (hasActiveBoost)
            {
                // Show active sprite (bright/glowing)
                if (CombatBoostActiveSprite != null)
                {
                    boostImage.sprite = CombatBoostActiveSprite;
                    boostImage.color = Color.white;
                    Debug.Log("💊 Combat Boost Indicator: ACTIVE (boost enabled)");
                }
                else
                {
                    Debug.LogWarning("💊 CombatBoostActiveSprite is not assigned!");
                }
            }
            else
            {
                // Show inactive sprite (grey/dimmed)
                if (CombatBoostInactiveSprite != null)
                {
                    boostImage.sprite = CombatBoostInactiveSprite;
                    boostImage.color = Color.white;
                    Debug.Log("💊 Combat Boost Indicator: INACTIVE (no boost)");
                }
                else
                {
                    Debug.LogWarning("💊 CombatBoostInactiveSprite is not assigned!");
                }
            }
        }
        else
        {
            Debug.LogError("💊 CombatBoostIndicator has no Image component!");
        }
    }

    /// <summary>
    /// Initializes the Farming Perk Indicator with inactive sprite by default.
    /// Will be updated when staking status is received from backend.
    /// </summary>
    public void InitializeFarmingPerkIndicator()
    {
        if (FarmingAbility != null)
        {
            FarmingAbility.SetActive(true);

            // Force set inactive sprite immediately
            var perkImage = FarmingAbility.GetComponent<Image>();
            if (perkImage != null && PerkInactiveSprite != null)
            {
                perkImage.sprite = PerkInactiveSprite;
                perkImage.color = Color.white;
                Debug.Log($"🌾 InitializeFarmingPerkIndicator: Set to INACTIVE sprite");
            }
            else
            {
                Debug.LogWarning($"🌾 InitializeFarmingPerkIndicator: perkImage={perkImage != null}, InactiveSprite={PerkInactiveSprite != null}");
            }

            // Check PlayerProfileInfo farmer status and sync with it
            bool isFarmer = PlayerProfileInfo.instance != null && PlayerProfileInfo.instance.IsUserFarmer;
            Debug.Log($"🌾 InitializeFarmingPerkIndicator: PlayerProfileInfo.IsUserFarmer = {isFarmer}");

            UpdateFarmingPerkIndicator(isFarmer);
        }
        else
        {
            Debug.LogWarning("🌾 FarmingAbility GameObject is not assigned in UIInventory!");
        }
    }

    /// <summary>
    /// Updates the Farming Perk indicator sprite based on farmer status
    /// </summary>
    public void UpdateFarmingPerkIndicator(bool isFarmer)
    {
        if (FarmingAbility == null)
        {
            Debug.LogWarning("🌾 Cannot update Farming Perk Indicator: GameObject is null");
            return;
        }

        var perkImage = FarmingAbility.GetComponent<Image>();
        if (perkImage != null)
        {
            if (isFarmer)
            {
                // Show active sprite (user has liquidity staked)
                if (PerkActiveSprite != null)
                {
                    perkImage.sprite = PerkActiveSprite;
                    perkImage.color = Color.white;
                    Debug.Log("🌾 Farming Perk Indicator: ACTIVE (user is farmer)");
                }
                else
                {
                    Debug.LogWarning("🌾 PerkActiveSprite is not assigned!");
                }
            }
            else
            {
                // Show inactive sprite (user doesn't have liquidity staked)
                if (PerkInactiveSprite != null)
                {
                    perkImage.sprite = PerkInactiveSprite;
                    perkImage.color = Color.white;
                    Debug.Log("🌾 Farming Perk Indicator: INACTIVE (user is not farmer)");
                }
                else
                {
                    Debug.LogWarning("🌾 PerkInactiveSprite is not assigned!");
                }
            }
        }
        else
        {
            Debug.LogError("🌾 FarmingAbility has no Image component!");
        }
    }

    private List<Sprite> allIcons;
    private void LoadSprites()
    {
        allIcons = Resources.LoadAll<Sprite>("NFTIcons/characterIcons").ToList();
        var weapIcons = Resources.LoadAll<Sprite>("NFTIcons/weaponIcons").ToList();
        allIcons.AddRange(weapIcons);
    }

    public static Sprite LoadIcon(string spriteName)
    {
        foreach (var s in instance.allIcons)
        {
            if (s.name == spriteName.ToLower())
            {
                return s;
            }
        }
        return null;
    }

#if UNITY_EDITOR
    //private void Update()
    //{
    //    if (Input.GetKey(KeyCode.LeftShift))
    //    {
    //        if (Input.GetKeyDown(KeyCode.Alpha1))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "Viper"), true);
    //        }
    //        else if (Input.GetKeyDown(KeyCode.Alpha2))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "sandcrawler's sniper rifle"), true);
    //        }
    //        else if(Input.GetKeyDown(KeyCode.Alpha3))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "adept's repeater"), true);
    //        }
    //        else if (Input.GetKeyDown(KeyCode.Alpha4))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "underdog meda-gun"), true);
    //        }
    //        else if(Input.GetKeyDown(KeyCode.Alpha5))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "blessed blade"), true);
    //        }
    //        else if (Input.GetKeyDown(KeyCode.Alpha6))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "gladiator's greatsword"), true);
    //        }
    //        else if (Input.GetKeyDown(KeyCode.Alpha7))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "ryoshi katana"), true);
    //        }
    //        else if (Input.GetKeyDown(KeyCode.Alpha8))
    //        {
    //            var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
    //            icon.Setup(new NftWeapon(rndatt(), rndatt(), rndatt(), 0, "tactician's claymore"), true);
    //        }
    //        ArrowDown.SetActive(false);
    //    }

    //    if (Input.GetKeyDown(KeyCode.Z))
    //    {
    //        var nameOfNft = (Resources.LoadAll<Sprite>("NFTIcons/characterIcons").ToList().Random().name);
    //        var icon = Instantiate(HeroIconPrefab, Container).GetComponent<UINftHero>();

    //        var val = UnityEngine.Random.value;
    //        var faction = val < 0.5f ? "renegade" : "goliath";

    //        if(nameOfNft == "floki" || nameOfNft == "commander")
    //            icon.Setup(new NftHero(100+rndatt(), 100+rndatt(), 100+rndatt(), rndatt()*12, "neutral", nameOfNft, false), true);
    //        else
    //            icon.Setup(new NftHero(rndatt(), rndatt(), rndatt(), 0, faction, nameOfNft, false), true);
    //        ArrowDown.SetActive(false);
    //    }
    //}

    //private int rndatt() => UnityEngine.Random.Range(1, 101);

#endif

    public void DisplayStats()
    {
        ClearScrollView();
        TitleText.text = "Stats";
        Stats.UpdateStats(
            (PlayerProfileInfo.instance.EquippedHero as NftHero)?.ConvertToBoostedStats(),
            (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats());
        PopulatedBy = PopulationType.Stats;
    }

    public void DisplayAbilities()
    {
        ClearScrollView();
        TitleText.text = "Abilities";

        PopulatedBy = PopulationType.Abilities;
    }

    public void DisplayHeroes()
    {
        Debug.Log($"🦸 DEBUG: DisplayHeroes called - Heroes count: {PlayerProfileInfo.instance.NftHeroes?.Nfts?.Count ?? 0}");

        if (PlayerProfileInfo.instance.NftHeroes != null && PlayerProfileInfo.instance.NftHeroes.Nfts != null && PlayerProfileInfo.instance.NftHeroes.Nfts.Count > 0)
        {
            Debug.Log($"🦸 DEBUG: Calling PopulateScrollView with {PlayerProfileInfo.instance.NftHeroes.Nfts.Count} heroes");
            PopulateScrollView(PlayerProfileInfo.instance.NftHeroes.Nfts);

            // Refresh Preview visuals if items are equipped (called after UI is built)
            if (Preview != null)
            {
                Preview.RefreshEquippedVisuals();
                Debug.Log("🎯 DisplayHeroes: Refreshed Preview visuals");
            }
        }
        else
        {
            Debug.LogWarning("🦸 WARNING: No heroes found to display");
            ClearScrollView();
            TitleText.text = "NFT HEROES";
            if (HeaderTitleText != null) HeaderTitleText.text = "NFT HEROES";
            ArrowDown.SetActive(true);
            PopulatedBy = PopulationType.Heroes;
        }
    }

    public void DisplayWeapons()
    {
        Debug.Log($"⚔️ DEBUG: DisplayWeapons called - Weapons count: {PlayerProfileInfo.instance.NftWeapons?.Nfts?.Count ?? 0}");

        if (PlayerProfileInfo.instance.NftWeapons != null && PlayerProfileInfo.instance.NftWeapons.Nfts != null && PlayerProfileInfo.instance.NftWeapons.Nfts.Count > 0)
        {
            Debug.Log($"⚔️ DEBUG: Calling PopulateScrollView with {PlayerProfileInfo.instance.NftWeapons.Nfts.Count} weapons");
            PopulateScrollView(PlayerProfileInfo.instance.NftWeapons.Nfts);

            // Refresh Preview visuals if items are equipped (called after UI is built)
            if (Preview != null)
            {
                Preview.RefreshEquippedVisuals();
                Debug.Log("🎯 DisplayWeapons: Refreshed Preview visuals");
            }
        }
        else
        {
            Debug.LogWarning("⚔️ WARNING: No weapons found to display");
            ClearScrollView();
            TitleText.text = "NFT WEAPONS";
            if (HeaderTitleText != null) HeaderTitleText.text = "NFT WEAPONS";
            ArrowDown.SetActive(true);
            PopulatedBy = PopulationType.Weapons;
        }
    }

    //[Button("Populate from Resources")]
    //public void RepopulateFromResources()
    //{
    //    var allSprites = Resources.LoadAll<Sprite>("NFTIcons/characterIcons");
    //    foreach (var hero in AllHeroes)
    //    {
    //        hero.Visualization = allSprites.First(x => string.Equals(x.name, $"{hero.Name}_inactive", StringComparison.OrdinalIgnoreCase));
    //    }
    //}

    public void ClearScrollView()
    {
        Container.ForEachChild(x => Destroy(x));
    }

    // 🔥 FIX: Updated to work with InventoryConfig
    public void PopulateScrollView(InventoryConfig config)
    {
        Debug.Log($"🎮 DEBUG: PopulateScrollView called with config: {config?.Name}, Count: {config?.Nfts?.Count ?? 0}");

        // clear current scroll items
        ClearScrollView();

        // adjust texts
        TitleText.text = config.Name;

        var count = config.Nfts.Count;
        if (count == 0)
        {
            Debug.LogWarning($"🎮 WARNING: No NFTs in config {config.Name}");
            ArrowDown.SetActive(true);
            return;
        }

        ArrowDown.SetActive(false);

        switch (config.Type)
        {
            case InventoryConfig.ConfigType.Heores:
                PopulateHeroes(config.Nfts);
                PopulatedBy = PopulationType.Heroes;
                break;

            case InventoryConfig.ConfigType.Weapons:
                PopulateWeapons(config.Nfts);
                PopulatedBy = PopulationType.Weapons;
                break;

            default:
                PopulatedBy = PopulationType.Stats;
                break;
        }
    }

    // 🔥 FIX: New overload for direct NFT list
    public void PopulateScrollView(IList<INft> nfts)
    {
        Debug.Log($"🎮 DEBUG: PopulateScrollView called directly with {nfts?.Count ?? 0} NFTs");

        if (nfts == null || nfts.Count == 0)
        {
            Debug.LogWarning("🎮 WARNING: No NFTs to display");
            ClearScrollView();
            ArrowDown.SetActive(true);
            return;
        }

        // Clear current scroll items
        ClearScrollView();
        ArrowDown.SetActive(false);

        // Determine if these are heroes or weapons based on the first item
        var firstNft = nfts.First();
        if (firstNft is NftHero)
        {
            TitleText.text = "NFT HEROES";
            if (HeaderTitleText != null) HeaderTitleText.text = "NFT HEROES";
            PopulateHeroes(nfts);
            PopulatedBy = PopulationType.Heroes;
        }
        else if (firstNft is NftWeapon)
        {
            TitleText.text = "NFT WEAPONS";
            if (HeaderTitleText != null) HeaderTitleText.text = "NFT WEAPONS";
            PopulateWeapons(nfts);
            PopulatedBy = PopulationType.Weapons;
        }
        else
        {
            Debug.LogError($"🎮 ERROR: Unknown NFT type: {firstNft.GetType()}");
        }
    }

    private void PopulateHeroes(IList<INft> nfts)
    {
        Debug.Log($"🦸 DEBUG: PopulateHeroes called with {nfts?.Count ?? 0} heroes");

        var spawnedCount = 0;

        GetNewImage.sprite = GetNewHero;
        GetNewText.text = "Get a new hero";

        try
        {
            GetNewText.transform.parent.Find("redirect").GetComponent<OpenLinkButton>().Link = OpenLinkButton.MarketplaceUrl;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"🦸 WARNING: Could not set marketplace link: {e.Message}");
        }

        if (SelectYourHeroPrefab != null)
        {
            Instantiate(SelectYourHeroPrefab, Container);
        }

        foreach (var nft in nfts.Where(x => x != PlayerProfileInfo.instance.EquippedHero).OrderBy(x => x.Name))
        {
            spawnedCount++;
            if (HeroIconPrefab != null)
            {
                var icon = Instantiate(HeroIconPrefab, Container).GetComponent<UINftHero>();
                icon.Setup(nft, true);
                Debug.Log($"🦸 DEBUG: Created hero UI for: {nft.Name}");
            }
            else
            {
                Debug.LogError("🦸 ERROR: HeroIconPrefab is null!");
            }
        }

        spawnedCount++;

        // build "get more" section
        //  => spawn empties to fill space
        if (spawnedCount % 5 != 0)
            FillSpace(ref spawnedCount);

        Debug.Log($"🦸 DEBUG: PopulateHeroes completed - {spawnedCount} items spawned");
    }

    private void PopulateWeapons(IList<INft> nfts)
    {
        Debug.Log($"⚔️ DEBUG: PopulateWeapons called with {nfts?.Count ?? 0} weapons");

        var spawnedCount = 0;
        GetNewImage.sprite = GetNewWeapon;
        GetNewText.text = "Get a new weapon";

        try
        {
            GetNewText.transform.parent.Find("redirect").GetComponent<OpenLinkButton>().Link = OpenLinkButton.MarketplaceUrl;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"⚔️ WARNING: Could not set marketplace link: {e.Message}");
        }

        if (SelectYourWeaponPrefab != null)
        {
            Instantiate(SelectYourWeaponPrefab, Container);
        }

        foreach (var nft in nfts.Where(x => x != PlayerProfileInfo.instance.EquippedWeapon))
        {
            spawnedCount++;
            if (WeaponIconPrefab != null)
            {
                var icon = Instantiate(WeaponIconPrefab, Container).GetComponent<UINftWeapon>();
                icon.Setup(nft, true);
                Debug.Log($"⚔️ DEBUG: Created weapon UI for: {nft.Name}");
            }
            else
            {
                Debug.LogError("⚔️ ERROR: WeaponIconPrefab is null!");
            }
        }

        spawnedCount++;

        // build "get more" section
        //  => spawn empties to fill space
        FillSpace(ref spawnedCount);

        Debug.Log($"⚔️ DEBUG: PopulateWeapons completed - {spawnedCount} items spawned");
    }

    private void FillSpace(ref int spawnedCount)
    {
        //  => spawn empties to fill space
        for (var i = spawnedCount % ColumnCount; i < ColumnCount; i++)
        {
            spawnedCount++;
            if (SpaceFillerPrefab != null)
            {
                Instantiate(SpaceFillerPrefab, Container);
            }
        }
    }
}