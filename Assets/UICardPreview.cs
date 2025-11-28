using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElRaccoone.Tweens;
using TMPro;

public class UICardPreview: MonoBehaviour
{
    public UIInventory inventory;
    public Image Background;
    public Transform Panel;

    public TextMeshProUGUI CardNameText;
    public TextMeshProUGUI CardAttributesText;

    public TextMeshProUGUI AbilityNameText;
    public TextMeshProUGUI AbilityCooldownText;
    public TextMeshProUGUI AbilityDescriptionText;

    public Image AbilityIconImage, EquippedImage, EquippedWeapon, EquippedAbility;
    public Sprite HeroSprite, WeaponSprite, AbilitySprite, FrameSprite;

    public Animator CardPreview;

    private NftHero showedNft;

    public Button EquipButton;

    private string attributesTemplate;

    public CardPreviewAbilityDisplay AbilityDisplay;
    private void Start()
    {
        attributesTemplate = CardAttributesText.text;
    }

    public void Equip(NftWeapon weapon)
    {
        var playerProfile = PlayerProfileInfo.instance;
        playerProfile.EquippedWeapon = weapon;
        inventory.PopulateScrollView(playerProfile.NftWeapons);

        EquippedWeapon.gameObject.SetActive(true);
        EquippedWeapon.sprite = weapon.Visualization;
        EquippedWeapon.transform.localScale *= 0;

        //EquippedImage.TweenLocalScale(Vector3.one, 0.2f).SetFrom(Vector3.zero).SetDelay(0.2f);
        EquippedWeapon.TweenLocalScale(Vector3.one * 1.3f, 0.2f).SetFrom(Vector3.zero).SetDelay(0.2f).SetOnComplete(() =>
        {
            EquippedWeapon.TweenLocalScale(Vector3.one, 0.15f);
            EquippedWeapon.transform.parent.GetComponent<Image>().sprite = FrameSprite;
        });
    }

    public void UnequipBoosts()
    {

    }

    public void UnequipWeapon()
    {
        var playerProfile = PlayerProfileInfo.instance;
        playerProfile.EquippedWeapon = null;

        if (inventory.PopulatedBy == UIInventory.PopulationType.Weapons)
            inventory.PopulateScrollView(playerProfile.NftWeapons);
        else if (inventory.PopulatedBy == UIInventory.PopulationType.Stats)
        {
            var heroStats = (playerProfile.EquippedHero as NftHero)?.ConvertToBoostedStats();
            var weapStats = (playerProfile.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();
            inventory.Stats.UpdateStats(heroStats, weapStats);
        }

        EquippedWeapon.transform.parent.GetComponent<Image>().sprite = WeaponSprite;
        EquippedWeapon.TweenLocalScale(Vector3.zero, 0.2f).SetFrom(Vector3.one).SetOnComplete(() =>
        {
            EquippedWeapon.gameObject.SetActive(false);
        });
    }

    public void Display(INft nft, bool equip)
    {
        showedNft = nft as NftHero;
        CardNameText.text = showedNft.Name;
        CardAttributesText.text = attributesTemplate.Replace("S", showedNft.Attributes.Security.ToString());
        CardAttributesText.text = CardAttributesText.text.Replace("A", showedNft.Attributes.Anonymity.ToString());
        CardAttributesText.text = CardAttributesText.text.Replace("I", showedNft.Attributes.Innovation.ToString());

        var heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbilityForFraction(showedNft.Fraction);
        AbilityNameText.text = heroAbility.AbilityName;
        AbilityDescriptionText.text = heroAbility.AbilityDescription;
        AbilityIconImage.gameObject.SetActive(true);
        AbilityIconImage.sprite = heroAbility.Icon;
        AbilityCooldownText.text = $"{Mathf.RoundToInt(showedNft.ConvertToBoostedStats().CooldownReductionFactor * heroAbility.Cooldown)}s";

        AbilityDisplay.SetAbilitiesFor(showedNft);

        Background.gameObject.SetActive(true);
        Background.TweenCanvasGroupAlpha(1, 0.4f).SetFrom(0);
        Panel.TweenLocalScale(Vector3.one, 0.3f).SetFrom(Vector3.zero);
        Panel.TweenCanvasGroupAlpha(1, 0.3f).SetFrom(0);

        var resourcePath = $"NFTCards/{nft.Name}_Controller";
        CardPreview.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(resourcePath);
        if (CardPreview.runtimeAnimatorController == null)
            CardPreview.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/missing");

        if (equip)
        {
            EquipButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip";
         
            EquipButton.onClick.RemoveAllListeners();
            EquipButton.onClick.AddListener(Equip);
        }
        else
        {
            EquipButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Buy";
            
            EquipButton.onClick.RemoveAllListeners();
            EquipButton.onClick.AddListener(Buy);
        }
    }

    public void Display(NftWeapon weapon, bool equip)
    {
        //showedNft = nft as NftHero;
        CardNameText.text = weapon.Name;
        CardAttributesText.text = attributesTemplate.Replace("S", (weapon.Security).ToString());
        CardAttributesText.text = CardAttributesText.text.Replace("A", (weapon.Anonymity).ToString());
        CardAttributesText.text = CardAttributesText.text.Replace("I", (weapon.Innovation).ToString());

        bool hasDesc = !string.IsNullOrEmpty(weapon.OptionalDescription);
        AbilityNameText.text = hasDesc ? "Description" : "";
        AbilityDescriptionText.text = weapon.OptionalDescription;
        AbilityIconImage.gameObject.SetActive(hasDesc);
        AbilityIconImage.sprite = weapon.Visualization;
        AbilityCooldownText.text = "";

        AbilityDisplay.SetAbilitiesFor(weapon);

        Background.gameObject.SetActive(true);
        Background.TweenCanvasGroupAlpha(1, 0.4f).SetFrom(0);
        Panel.TweenLocalScale(Vector3.one, 0.3f).SetFrom(Vector3.zero);
        Panel.TweenCanvasGroupAlpha(1, 0.3f).SetFrom(0);
       
        var resourcePath = $"NFTWeapons/{weapon.Name}_Controller";
        CardPreview.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(resourcePath);
        if(CardPreview.runtimeAnimatorController == null)
            CardPreview.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/missing");

        EquipButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = "Equip";

        EquipButton.onClick.RemoveAllListeners();
        EquipButton.onClick.AddListener(()=> 
        { 
            Equip(weapon);
            Hide();
        });
    }

    public void Buy()
    {
        var url = "https://cryptomeda.tech/marketplace";
        DialogBox.DisplayRedirectDialog("You will be redirected to your browser", url, () =>
        {
            Application.OpenURL(url);
        }, () => { });

        Hide();
    }

    public void Unequip()
    {
        var playerProfile = PlayerProfileInfo.instance;
        var heroStats = (playerProfile.EquippedHero as NftHero)?.ConvertToBoostedStats();
        var weapStats = (playerProfile.EquippedWeapon as NftWeapon)?.ConvertToBoostedStats();
        playerProfile.EquippedHero = null;

        if (inventory.PopulatedBy == UIInventory.PopulationType.Heroes)
            inventory.PopulateScrollView(playerProfile.NftHeroes);
        else if (inventory.PopulatedBy == UIInventory.PopulationType.Stats)
            inventory.Stats.UpdateStats(heroStats, weapStats);
        
        EquippedImage.transform.parent.GetComponent<Image>().sprite = HeroSprite;
        EquippedImage.TweenLocalScale(Vector3.zero, 0.2f).SetFrom(Vector3.one).SetOnComplete(() =>
        {
            EquippedImage.gameObject.SetActive(false);
        });

        EquippedAbility.TweenCanvasGroupAlpha(0, 1).SetDelay(0.35f).SetOnComplete(() =>
        {
            EquippedAbility.gameObject.SetActive(false);
        });
    }

    public void Equip()
    {
        var playerProfile = PlayerProfileInfo.instance;
        playerProfile.EquippedHero = showedNft;
        inventory.PopulateScrollView(playerProfile.NftHeroes);

        EquippedImage.gameObject.SetActive(true);
        EquippedImage.sprite = showedNft.Visualization;
        EquippedImage.transform.localScale *= 0;

        
        var heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbilityForFraction(showedNft.Fraction);
        EquippedAbility.GetComponent<CanvasGroup>().alpha = 0;
        EquippedAbility.gameObject.SetActive(true);
        EquippedAbility.sprite = heroAbility.Icon;
        EquippedAbility.TweenCanvasGroupAlpha(1, 1).SetDelay(0.35f).SetFrom(0);

        GetComponent<AudioSource>().Play();

        //EquippedImage.TweenLocalScale(Vector3.one, 0.2f).SetFrom(Vector3.zero).SetDelay(0.2f);
        EquippedImage.TweenLocalScale(Vector3.one * 1.3f, 0.2f).SetFrom(Vector3.zero).SetDelay(0.2f).SetOnComplete(() =>
        {
            EquippedImage.TweenLocalScale(Vector3.one, 0.15f);
            EquippedImage.transform.parent.GetComponent<Image>().sprite = FrameSprite;
        });

        Hide();
    }

    public void Hide()
    {
        Background.TweenCanvasGroupAlpha(0, 0.3f).SetFrom(1);
        Panel.TweenLocalScale(Vector3.zero, 0.3f).SetFrom(Vector3.one).SetOnComplete(()=> Background.gameObject.SetActive(false));
    }
}