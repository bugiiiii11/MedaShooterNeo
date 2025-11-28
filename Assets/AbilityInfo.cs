using ElRaccoone.Tweens;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AbilityInfo : Singleton<AbilityInfo>
{
    public TextMeshProUGUI Title, Description, Passivity, Cooldown;
    public Image AbilityIcon;
    public GameObject Holder;

    public void Show(AbilityConfig ability)
    {
        Title.text = ability.AbilityName;
        Description.text = ability.AbilityDescription;
        AbilityIcon.sprite = ability.Icon;
        Passivity.text = "Active Ability";

        if (ability.Cooldown <= 0)
        {
            Cooldown.text = "";
        }
        else
        {
            Cooldown.text = $"{ability.Cooldown} second cooldown";
        }
        Holder.SetActive(true);
        Holder.TweenCanvasGroupAlpha(1, 0.5f).SetFrom(0);
    }

    public void Show(string n)
    {
        var ability = PlayerProfileInfo.instance.NftHandler.otherAbilities.Find(x => x.AbilityName == n);
        if(ability)
        {
            Show(ability);
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
