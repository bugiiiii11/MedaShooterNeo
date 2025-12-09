using ElRaccoone.Tweens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TextSpriteState
{
    public string Text;
    public Sprite Image;
    public Color TextColor;
}

public class UINftPreview : Singleton<UINftPreview>
{
    public TextSpriteState InvokeEquipState, AlreadyEquippedState;
    public TextMeshProUGUI ChooseNftMainTitle, ChooseNftMainTransition, ChooseYourNftScroll;
    public TextMeshProUGUI WindowHeaderTitle; // The visible header title at top of window
    public UINftInfo HeroNftInfo;
    public Image EquipButton;
    public GameObject SelectIconPrefab, SelectIconWeaponPrefab;

    public GameObject MidPart;
    public GameObject HeroesPart;
    public RectTransform HeroesScrollRectContent;
    public GameObject MarketplaceLink;
    public Image EquippedHeroImage, EquippedWeaponImage, EquippedAbilityImage;
    public MissingNftNotification NoNftNotification;
    public NftHero SelectedHero { get; set; }
    public NftWeapon SelectedWeapon { get; set; }
    public Sprite EpicPerkChanceSprite, SelectedFrame, NormalFrame, EquippedFrame;
    public UIStats Stats;
    public IncrementStats IncrementStats;
    private IUiReferencable PreviouslySelected, CurrentlySelected, CurrentlyEquipped;

    private bool shouldEquipHero { get; set; }

    private void Start()
    {
        DetermineEquipState();
    }

    private void DetermineEquipState()
    {
        var heroStats = (PlayerProfileInfo.instance.EquippedHero as NftHero)?.ConvertToBoostedStats();
        var weapStats = (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();

        if (PlayerProfileInfo.instance.EquippedWeapon != null)
        {
            EquippedWeaponImage.gameObject.SetActive(true);
            EquippedWeaponImage.sprite = PlayerProfileInfo.instance.EquippedWeapon.Visualization;
            CurrentlySelected = null;
            PreviouslySelected = null;
            CurrentlyEquipped = null;

            IncrementStats.ReportWeaponEquip(weapStats);

            // update numbers on screen
            Stats.UpdateStats(heroStats, weapStats);
        }

        if (PlayerProfileInfo.instance.EquippedHero != null)
        {
            EquippedHeroImage.gameObject.SetActive(true);
            EquippedHeroImage.sprite = PlayerProfileInfo.instance.EquippedHero.Visualization;
            CurrentlySelected = null;
            PreviouslySelected = null;
            CurrentlyEquipped = null;

            IncrementStats.ReportHeroEquip(heroStats);

            // update numbers on screen
            Stats.UpdateStats(heroStats, weapStats);
        }
    }

    public void ToggleDisplay()
    {
        HideHeroes();

        if (!shouldEquipHero)
        {
            // weapons is current
            DisplayHeroes();
        }
        else
        {
            DisplayWeapons();
        }
    }

    public void AdjustTexts(bool shouldEquipHero)
    {
        if (!shouldEquipHero)
        {
            // heroes view is current
            ChooseYourNftScroll.text = "Select your hero";
            if (ChooseNftMainTitle != null) ChooseNftMainTitle.text = "NFT HEROES";
            if (ChooseNftMainTransition != null) ChooseNftMainTransition.text = "NFT Weapons";
            // Update window header title
            if (WindowHeaderTitle != null) WindowHeaderTitle.text = "NFT HEROES";
        }
        else
        {
            // weapons view is current
            ChooseYourNftScroll.text = "Select your weapon";
            if (ChooseNftMainTitle != null) ChooseNftMainTitle.text = "NFT WEAPONS";
            if (ChooseNftMainTransition != null) ChooseNftMainTransition.text = "NFT Heroes";
            // Update window header title
            if (WindowHeaderTitle != null) WindowHeaderTitle.text = "NFT WEAPONS";
        }
    }

    public void DisplayWeapons()
    {
        Debug.Log($"⚔️ DisplayWeapons() called - shouldEquipHero before: {shouldEquipHero}");

        AdjustTexts(true);

        MidPart.SetActive(false);
        HeroesPart.SetActive(true);

        ClearScrollView(HeroesScrollRectContent);

        PopulateScrollView(PlayerProfileInfo.instance.NftWeapons);

        if(shouldEquipHero)
        {
            HeroNftInfo.SetDefaultState();
            IncrementStats.ShowEmptyWeaponStats();
        }

        shouldEquipHero = false;
        Debug.Log($"⚔️ DisplayWeapons() - shouldEquipHero set to: {shouldEquipHero}");
    }

    public void DisplayHeroes()
    {
        Debug.Log($"🦸 DisplayHeroes() called - shouldEquipHero before: {shouldEquipHero}");

        AdjustTexts(false);

        MidPart.SetActive(false);
        HeroesPart.SetActive(true);

        ClearScrollView(HeroesScrollRectContent);

        // fetch heroes
        PopulateScrollView(PlayerProfileInfo.instance.NftHeroes);

        if (!shouldEquipHero)
        {
            HeroNftInfo.SetDefaultState();
            IncrementStats.ShowEmptyHeroStats();
        }

        shouldEquipHero = true;
        Debug.Log($"🦸 DisplayHeroes() - shouldEquipHero set to: {shouldEquipHero}");
    }

    public void HideHeroes()
    {
        MidPart.SetActive(true);
        HeroesPart.SetActive(false);

        CurrentlySelected = null;
        PreviouslySelected = null;
        CurrentlyEquipped = null;
    }

    public void Equip()
    {
        Debug.Log($"🎮 Equip() called - shouldEquipHero: {shouldEquipHero}, SelectedHero: {SelectedHero?.Name ?? "null"}, SelectedWeapon: {SelectedWeapon?.Name ?? "null"}");

        bool unequip = EquipButton.sprite == AlreadyEquippedState.Image;
        Debug.Log($"🎮 Equip() - unequip mode: {unequip}");

        // determine what to equip
        if(shouldEquipHero)
        {
            if (unequip)
            {
                if(UnequipHero())
                    // handle button switch
                    SetButtonEquipped(false);
            }
            else
            {
                if(EquipSelectedHero())
                    // handle button switch
                    SetButtonEquipped(true);
            }
        }
        else
        {
            if (unequip)
            {
                if(UnequipWeapon())
                    // handle button switch
                    SetButtonEquipped(false);
            }
            else
            {
                if(EquipSelectedWeapon())
                    // handle button switch
                    SetButtonEquipped(true);
            }
        }
    }

    public bool UnequipHero()
    {
        if (CurrentlyEquipped == null)
            return false;

        PlayerProfileInfo.instance.EquippedHero = null;
        EquippedHeroImage.gameObject.SetActive(false);

        CurrentlySelected = CurrentlyEquipped;
        CurrentlyEquipped?.Select(SelectionIconType.Selected);
        CurrentlyEquipped = null;

        var heroStats = SelectedHero?.ConvertToBoostedStats();
        var weapStats = (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();
        IncrementStats.ReportHeroUnequip(weapStats, heroStats);

        // update numbers on screen
        IncrementStats.ShowHeroStats(SelectedHero, heroStats?.Clone(), weapStats?.Clone());
        Stats.UpdateStats(null, weapStats);

        return true;
    }

    public bool UnequipWeapon()
    {
        if (CurrentlyEquipped == null)
            return false;

        PlayerProfileInfo.instance.EquippedWeapon = null;
        EquippedWeaponImage.gameObject.SetActive(false);

        CurrentlySelected = CurrentlyEquipped;
        CurrentlyEquipped?.Select(SelectionIconType.Selected);
        CurrentlyEquipped = null;

        var heroStats = (PlayerProfileInfo.instance.EquippedHero as NftHero)?.ConvertToBoostedStats();
        var weapStats = SelectedWeapon?.ConvertToBoostedStats();
        IncrementStats.ReportWeaponUnequip(weapStats, heroStats);

        // update numbers on screen
        IncrementStats.ShowWeaponStats(SelectedWeapon, heroStats?.Clone(), weapStats?.Clone());
        Stats.UpdateStats(heroStats, null);
        return true;
    }

    public bool EquipSelectedWeapon()
    {
        Debug.Log($"⚔️ EquipSelectedWeapon() called - SelectedWeapon: {SelectedWeapon?.Name ?? "NULL"}");
        if (SelectedWeapon == null)
        {
            Debug.LogWarning("⚔️ EquipSelectedWeapon() - FAILED: SelectedWeapon is null!");
            return false;
        }

        PlayerProfileInfo.instance.EquippedWeapon = SelectedWeapon;

        EquippedWeaponImage.gameObject.SetActive(true);
        EquippedWeaponImage.sprite = SelectedWeapon.Visualization;

        if (CurrentlySelected != null)
            CurrentlySelected.Select(SelectionIconType.Equipped);

        if (CurrentlyEquipped != null)
            CurrentlyEquipped.Select(SelectionIconType.None);

        CurrentlyEquipped = CurrentlySelected;
        CurrentlySelected = null;

        GetComponent<AudioSource>().Play();

        //var heroStats = SelectedHero?.ConvertToBoostedStats();
        //var weapStats = (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();
        var heroStats = (PlayerProfileInfo.instance.EquippedHero as NftHero)?.ConvertToBoostedStats();
        var weapStats = SelectedWeapon?.ConvertToBoostedStats();

        IncrementStats.ReportWeaponEquip(weapStats);

        // update numbers on screen
        IncrementStats.ShowWeaponStats(SelectedWeapon, heroStats, weapStats);
        Stats.UpdateStats(heroStats, weapStats);
        return true;
    }

    public bool EquipSelectedHero()
    {
        Debug.Log($"🦸 EquipSelectedHero() called - SelectedHero: {SelectedHero?.Name ?? "NULL"}");
        if (SelectedHero == null)
        {
            Debug.LogWarning("🦸 EquipSelectedHero() - FAILED: SelectedHero is null!");
            return false;
        }

        PlayerProfileInfo.instance.EquippedHero = SelectedHero;

        EquippedHeroImage.gameObject.SetActive(true);
        EquippedHeroImage.sprite = SelectedHero.Visualization;

        if (CurrentlySelected != null)
            CurrentlySelected.Select(SelectionIconType.Equipped);

        if (CurrentlyEquipped != null)
            CurrentlyEquipped.Select(SelectionIconType.None);

        CurrentlyEquipped = CurrentlySelected;
        CurrentlySelected = null;

        /* --> part of skills screen
                var heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbilityForFraction(SelectedHero.Fraction);
                EquippedAbilityImage.GetComponent<CanvasGroup>().alpha = 0;
                EquippedAbilityImage.gameObject.SetActive(true);
                EquippedAbilityImage.sprite = heroAbility.Icon;
                EquippedAbilityImage.TweenCanvasGroupAlpha(1, 1).SetDelay(0.35f).SetFrom(0);
        */
        GetComponent<AudioSource>().Play();

        var heroStats = SelectedHero?.ConvertToBoostedStats();
        var weapStats = (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();
        IncrementStats.ReportHeroEquip(heroStats);

        // update numbers on screen
        IncrementStats.ShowHeroStats(SelectedHero, heroStats, weapStats);
        Stats.UpdateStats(heroStats, weapStats);
        return true;
    }

    internal void Display(INft nft, IUiReferencable sceneReference)
    {
        Debug.Log($"🎯 Display() called with nft: {nft?.Name ?? "null"}, type: {nft?.GetType().Name ?? "null"}");

        HeroNftInfo.Setup(nft);

        if (nft is NftHero hero)
        {
            SelectedHero = hero;
            Debug.Log($"🦸 Display() - Set SelectedHero to: {hero.Name}");
            // display stats changes
            var heroStats = SelectedHero?.ConvertToBoostedStats();
            var weapStats = (PlayerProfileInfo.instance.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();
            IncrementStats.ShowHeroStats(SelectedHero, heroStats, weapStats);
        }
        else if (nft is NftWeapon weapon)
        {
            SelectedWeapon = weapon;
            Debug.Log($"⚔️ Display() - Set SelectedWeapon to: {weapon.Name}");

            var heroStats = (PlayerProfileInfo.instance.EquippedHero as NftHero)?.ConvertToBoostedStats();
            var weapStats = SelectedWeapon?.ConvertToBoostedStats();
            IncrementStats.ShowWeaponStats(SelectedWeapon, heroStats, weapStats);
        }

        if (sceneReference == CurrentlyEquipped)
        {
            // it is hero already equipped
            SetButtonEquipped(true);
        }
        else
        {
            SetButtonEquipped(false);
        }

        // select in UI by assigning the frame
        if (PreviouslySelected != null)
        {
            if (PreviouslySelected == CurrentlyEquipped)
                PreviouslySelected.Select(SelectionIconType.Equipped);
            else
                PreviouslySelected.Select(SelectionIconType.None);
        }

        CurrentlySelected = sceneReference;
        CurrentlySelected.Select(SelectionIconType.Selected);

        PreviouslySelected = CurrentlySelected;
    }

    public void SetButtonEquipped(bool equipped)
    {
        if(!equipped)
        {
            EquipButton.sprite = InvokeEquipState.Image;
            var text = EquipButton.GetComponentInChildren<TextMeshProUGUI>();
            text.text = InvokeEquipState.Text;
            text.color = InvokeEquipState.TextColor;
        }
        else
        {
            EquipButton.sprite = AlreadyEquippedState.Image;
            var text = EquipButton.GetComponentInChildren<TextMeshProUGUI>();
            text.text = AlreadyEquippedState.Text;
            text.color = AlreadyEquippedState.TextColor;
        }
    }

    public static void ClearScrollView(RectTransform r)
    {
        r.ForEachChild(x => Destroy(x));
    }

    public void PopulateScrollView(InventoryConfig config)
    {
        var count = config.Nfts.Count;
        if (count == 0)
        {
            MarketplaceLink.SetActive(true);
            if(!shouldEquipHero)
            {
                bool wasNotifShowed = false;

                if (PlayerPrefs.HasKey("hero_notif_show"))
                {
                    wasNotifShowed = PlayerPrefs.GetInt("hero_notif_show") > 0;
                }

                if (!wasNotifShowed)
                {
                    NoNftNotification.Show("You have no NFT hero!", "Having a hero increases your chance to progress further and place higher in leaderboards where you can win interesting prizes. Check out our marketplace and buy a hero now!", NoNftNotification.MissingHero);
                    PlayerPrefs.SetInt("hero_notif_show", 1);
                }
            }
            else
            {
                bool wasNotifShowed = false;

                if (PlayerPrefs.HasKey("weapon_notif_show"))
                {
                    wasNotifShowed = PlayerPrefs.GetInt("weapon_notif_show") > 0;
                }

                if (!wasNotifShowed)
                {
                    NoNftNotification.Show("You have no NFT weapon!", "Having a weapon increases your chance to progress further and place higher in leaderboards where you can win interesting prizes. Check out our marketplace and buy a weapon now!", NoNftNotification.MissingWeapon);
                    PlayerPrefs.SetInt("weapon_notif_show", 1);
                }
            }
            return;
        }
        MarketplaceLink.SetActive(false);


        // populating by nft heroes
        if (config.Nfts[0] is NftHero)
        {
            var eqHero = PlayerProfileInfo.instance.EquippedHero as NftHero;

            foreach (var hero in config.Nfts.OrderByDescending(x => ((NftHero)x).Attributes.Sum()))
            {
                var obj = Instantiate(SelectIconPrefab, HeroesScrollRectContent).GetComponent<UINftHero>();
                obj.Setup(hero, true);
                obj.gameObject.SetActive(true);

                if (eqHero != null)
                {
                    if (eqHero.Name == hero.Name && eqHero.Attributes == ((NftHero)hero).Attributes)
                    {
                        obj.Select(SelectionIconType.Equipped);
                        CurrentlyEquipped = obj;
                    }
                }
            }
        }

        // populating by nft weapons
        else if (config.Nfts[0] is NftWeapon)
        {
            var eqWeap = PlayerProfileInfo.instance.EquippedWeapon as NftWeapon;

            foreach (var weapon in config.Nfts.OrderByDescending(x => ((NftWeapon)x).Sum()))
            {
                var obj = Instantiate(SelectIconWeaponPrefab, HeroesScrollRectContent).GetComponent<UINftHero>();
                obj.Setup(weapon, true);
                obj.gameObject.SetActive(true);

                if (eqWeap != null)
                {
                    if (eqWeap.Name == weapon.Name && eqWeap.Sum() == ((NftWeapon)weapon).Sum())
                    {
                        obj.Select(SelectionIconType.Equipped);
                        CurrentlyEquipped = obj;
                    }
                }
            }

        }
    }
}
