using ElRaccoone.Tweens;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissingNftNotification : MonoBehaviour
{
    public TextMeshProUGUI Title, Description, ButtonText;
    public OpenLinkButton ActionButton;
    public Image ImageHolder, NormalImage;
    public GameObject Holder;

    public Sprite MissingWeapon, MissingHero, Shield, ChoosePerk;

    public void Show(string title, string description, Sprite sprite)
    {
        Title.text = title;
        Description.text = description;
        ImageHolder.sprite = sprite;
        NormalImage.gameObject.SetActive(false);
        ImageHolder.gameObject.SetActive(true);

        Holder.SetActive(true);

        Holder.TweenCanvasGroupAlpha(1, 0.5f).SetFrom(0);

        ButtonText.text = "Open Marketplace";
        ActionButton.Link = "https://cryptomeda.tech/marketplace";
    }

    public void Show(string title, string description, Sprite sprite, string buttonText, string link)
    {
        Title.text = title;
        Description.text = description;
        NormalImage.sprite = sprite;
        NormalImage.gameObject.SetActive(true);
        ImageHolder.gameObject.SetActive(false);

        Holder.SetActive(true);

        Holder.TweenCanvasGroupAlpha(1, 0.5f).SetFrom(0);

        ButtonText.text = buttonText;
        ActionButton.Link = link;
    }

    public void Show(string id)
    {
        switch (id)
        {
            case "buy_shield":
                Show("Get Your shield right now!", "Get far away with a shield during your battle journey! You have to stake your TECH tokens in Staking pool.", Shield, "Start Staking", "https://cryptomeda.tech/staking");
                break;

            case "buy_firstperk":
                Show("Get starting perk!", "Get far away with a starting perk chosen on the start of the game! You have to be part of ETH/TECH Farming pool.", ChoosePerk, "Start Farming", "https://cryptomeda.tech/staking");
                break;
        }    
    }

    public void Hide()
    {
        Holder.TweenCanvasGroupAlpha(0, 0.5f).SetFrom(1).SetOnComplete(() =>
        {
            Holder.SetActive(false);
        });
    }
}
