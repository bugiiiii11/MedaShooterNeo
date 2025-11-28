using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPerkBuff : MonoBehaviour
{
    public TMPro.TextMeshProUGUI StackAmountText;
    public string Title, Description;
    public Image IconImage;
    public Image CooldownFill;
    internal Sprite BackgroundImage;
    internal Rarity rarity;

    public int StackAmount = 1;
    private ActiveTimedPerkBehaviour timedBehaviour;

    public float Cooldown => timedBehaviour ? (timedBehaviour.Duration - timedBehaviour.CurrentLenght) : -1;

    public void SetFrom(Perk perk, PerkBehaviour behaviour)
    {
        Title = perk.Title;
        Description = perk.Description;
        StackAmountText.text = "";
        BackgroundImage = perk.Background;
        IconImage.sprite = perk.Icon;
        rarity = perk.Rarity;

        if(behaviour is ActiveTimedPerkBehaviour t)
        {
            timedBehaviour = t;
        }
        else
        {
            CooldownFill.enabled = false;
        }
    }

    public void Stack()
    {
        StackAmount++;
        StackAmountText.text = StackAmount.ToString();
    }

    internal void Unstack()
    {
        StackAmount--;

        if (StackAmount == 1)
            StackAmountText.text = "";
        else if (StackAmount <= 0)
            UIPerkBuffs.instance.Remove(this);
        else
            StackAmountText.text = StackAmount.ToString();
    }

    internal void Reactivate()
    {

    }

    private void Update()
    {
        if(timedBehaviour && timedBehaviour.CurrentLenght > 0)
        {
            CooldownFill.fillAmount = 1 - timedBehaviour.CurrentLenght / timedBehaviour.Duration;
        }
    }
}
