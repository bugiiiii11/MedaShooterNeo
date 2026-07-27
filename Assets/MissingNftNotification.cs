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

    public Sprite MissingWeapon, MissingHero, Shield, ChoosePerk, CombatBoost;

    public void Show(string title, string description, Sprite sprite)
    {
        if (Title == null || Description == null || ImageHolder == null || NormalImage == null || Holder == null || ButtonText == null || ActionButton == null)
        {
            Debug.LogError("⚠️ MissingNftNotification.Show() - Missing component references! Please assign all fields in Inspector.");
            return;
        }

        Title.text = title;
        Description.text = description;
        ImageHolder.sprite = sprite;
        NormalImage.gameObject.SetActive(false);
        ImageHolder.gameObject.SetActive(true);

        Holder.SetActive(true);

        Holder.TweenCanvasGroupAlpha(1, 0.5f).SetFrom(0);

        ActionButton.gameObject.SetActive(true);
        ButtonText.text = "Open Marketplace";
        ActionButton.Link = OpenLinkButton.MarketplaceUrl;
    }

    public void Show(string title, string description, Sprite sprite, string buttonText, string link)
    {
        if (Title == null || Description == null || NormalImage == null || ImageHolder == null || Holder == null || ButtonText == null || ActionButton == null)
        {
            Debug.LogError("⚠️ MissingNftNotification.Show(5 params) - Missing component references! Please assign all fields in Inspector.");
            return;
        }

        if (sprite == null)
        {
            Debug.LogWarning($"⚠️ MissingNftNotification.Show() - sprite parameter is null for popup: {title}");
        }

        Title.text = title;
        Description.text = description;
        NormalImage.sprite = sprite;
        NormalImage.gameObject.SetActive(true);
        ImageHolder.gameObject.SetActive(false);

        Holder.SetActive(true);

        Holder.TweenCanvasGroupAlpha(1, 0.5f).SetFrom(0);

        // Hide button if buttonText is empty, otherwise show it
        if (string.IsNullOrEmpty(buttonText))
        {
            ActionButton.gameObject.SetActive(false);
        }
        else
        {
            ActionButton.gameObject.SetActive(true);
            ButtonText.text = buttonText;
            ActionButton.Link = link;
        }
    }

    public void Show(string id)
    {
        Debug.Log($"🔔 MissingNftNotification.Show(id) called with: {id}");

        switch (id)
        {
            case "buy_shield":
                if (Shield == null)
                {
                    Debug.LogError("⚠️ Shield sprite is not assigned in MissingNftNotification Inspector!");
                    return;
                }
                Show("Get Your Shield Ability!", "Own NFT land in your wallet to use shield ability in the game. The more land plots you have, the longer shield duration.", Shield, "", "");
                break;

            case "buy_firstperk":
                Debug.Log("🔔 buy_firstperk case triggered - checking ChoosePerk sprite...");
                if (ChoosePerk == null)
                {
                    Debug.LogError("⚠️ ChoosePerk sprite is not assigned in MissingNftNotification Inspector!");
                    return;
                }
                Debug.Log($"🔔 ChoosePerk sprite is valid: {ChoosePerk.name}, showing popup...");
                Show("Get starting perk!", "Choose a starting perk on the start of the game! You have to own Meda tokens in your wallet.", ChoosePerk, "", "");
                break;

            case "buy_boost":
                if (CombatBoost == null)
                {
                    Debug.LogError("⚠️ CombatBoost sprite is not assigned in MissingNftNotification Inspector!");
                    return;
                }
                Show("Activate Combat Boosts!", "Activate boosts to enhance player stats in the game. Purchase a boost package to gain damage, fire rate, and critical hit bonuses!", CombatBoost, "Get Boosts", OpenLinkButton.MedaShooterUrl);
                break;

            default:
                Debug.LogWarning($"⚠️ Unknown notification id: {id}");
                break;
        }
    }

    public void Hide()
    {
        if (Holder == null)
        {
            Debug.LogError("⚠️ MissingNftNotification.Hide() - Holder is null!");
            return;
        }

        Holder.TweenCanvasGroupAlpha(0, 0.5f).SetFrom(1).SetOnComplete(() =>
        {
            Holder.SetActive(false);
        });
    }
}
