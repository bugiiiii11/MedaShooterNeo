using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UINftInfo : MonoBehaviour
{
    public TextMeshProUGUI Fraction, Title, Description;
    public Image Icon;

    public GameObject SkillsPrefab;
    public RectTransform SkillsContent;
    public Sprite defaultBg;

    public void Setup(INft nft)
    {
        Icon.sprite = nft.Visualization;
        //Fraction.text =
        Title.text = nft.Name;

        if (nft is NftHero hero)
        {
            Fraction.text = hero.Fraction.ToString();
            Description.text = "Make a combination with NFT weapon and get even better stats!";
            GenerateSkills(hero);
        }
        else if(nft is NftWeapon weap)
        {
            Fraction.text = "<NONE>";
            Description.text = "Make a combination with NFT hero and get even better stats!";
            GenerateWeaponSkills(weap);
        }
    }

    public void SetDefaultState()
    {
        Icon.sprite = defaultBg;
        Title.text = "NOT SELECTED";
        Fraction.text = "<NONE>";
        Description.text = "";
        UINftPreview.ClearScrollView(SkillsContent);
    }

    private void GenerateWeaponSkills(NftWeapon weap)
    {
        UINftPreview.ClearScrollView(SkillsContent);
        var obj = Instantiate(SkillsPrefab, SkillsContent).GetComponent<Image>();
        var ability = Instantiate(PlayerProfileInfo.instance.GetWeaponAbilityDescriptor(WeaponButtonActivator.GetDetieredWeaponName(weap.Name.ToLower())));
        obj.sprite = ability.Icon;
        ability.AbilityName = weap.Name;
        obj.GetComponentInChildren<TextMeshProUGUI>().text = ability.AbilityName;
        obj.GetComponent<UIAbility>().Ability = ability;
    }

    private void GenerateSkills(NftHero nft)
    {
        UINftPreview.ClearScrollView(SkillsContent);

        var heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbilityForFraction(nft.Fraction);
        var obj = Instantiate(SkillsPrefab, SkillsContent).GetComponent<Image>();
        obj.sprite = heroAbility.Icon;
        obj.GetComponentInChildren<TextMeshProUGUI>().text = heroAbility.AbilityName;
        obj.GetComponent<UIAbility>().Ability = heroAbility;


        if(nft.Attributes.Power > 0)
        {
            // it is revolution card
            var epicPerkChance = Instantiate(SkillsPrefab, SkillsContent).GetComponent<Image>();
            epicPerkChance.sprite = UINftPreview.instance.EpicPerkChanceSprite;
            epicPerkChance.GetComponentInChildren<TextMeshProUGUI>().text = "Epic Perks Chance";

            epicPerkChance.GetComponent<UIAbility>().Ability = PlayerProfileInfo.instance.NftHandler.otherAbilities.Find(a => a.AbilityName == "Epic Perks Chance");
        }
    }
    /*
      
    <sprite name="stat_up"> 51
    <sprite name="stat_up"> 0.04
    <sprite name="stat_down"> 1
    <sprite name="stat_up"> 50
     
    */
}
