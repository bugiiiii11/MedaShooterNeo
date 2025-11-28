using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;
using System.Linq;

public class UIPerkBuffs : Singleton<UIPerkBuffs>
{
    public GameObject UiBuffPrefab, UiBuffDetailPrefab;
    public RectTransform PerkContainer, DetailsContainer;
    private List<UIPerkBuff> buffs;

    private void Start()
    {
        buffs = new();
        GameManager.instance.EventManager.AddListener<PerkActivatedEvent>(OnPerkActivated);
        GameManager.instance.EventManager.AddListener<PerkDeactivatedEvent>(OnPerkDeactivated);
        GameManager.instance.EventManager.AddListener<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDiedEvent obj)
    {
        foreach(var buff in buffs.ToList())
        {
            Remove(buff);
        }
    }

    private void OnPerkDeactivated(PerkDeactivatedEvent obj)
    {
        if (obj.IsStack)
        {
            //  existing perk buff
            buffs.Find(x => x.Title == obj.Perk.Title).Unstack();
        }
        else
        {
            Remove(buffs.Find(x => x.Title == obj.Perk.Title));
        }
    }

    internal void Remove(UIPerkBuff uIPerkBuff)
    {
        Destroy(uIPerkBuff.gameObject);
        buffs.Remove(uIPerkBuff);
    }

    private void OnPerkActivated(PerkActivatedEvent obj)
    {
        GameManager.instance.GameStats.PerksCollected++;

        if (obj.IsStack)
        {
            // use existing perk buff
            buffs.Find(x => x.Title == obj.Perk.Title).Stack();
        }
        else
        {
            // if there already is icon..
            var buffPerk = buffs.Find(x => x.Title == obj.Perk.Title);
            if (buffPerk)
                buffPerk.Reactivate();
            else
            {
                // add icon
                var perk = Instantiate(UiBuffPrefab, PerkContainer).GetComponent<UIPerkBuff>();
                perk.SetFrom(obj.Perk, obj.Behaviour);
                buffs.Add(perk);
            }
        }
    }

    public CanvasGroup PerkDetailsCanvas;
    public void DisplayPerkDetails()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        PerkDetailsCanvas.TweenCanvasGroupAlpha(1, 0.15f).SetFrom(0);
        PerkDetailsCanvas.blocksRaycasts = true;
        GameManager.instance.PauseGame(true);
        UIPerkManager.IsVisible = true;

        foreach (var buff in buffs)
        {
            var detail = Instantiate(UiBuffDetailPrefab, DetailsContainer).GetComponent<UIPerkBuffDetail>();
            detail.SetFrom(buff);
        }
    }
    
    public void HidePerkDetails()
    {
        PerkDetailsCanvas.TweenCanvasGroupAlpha(0, 0.15f).SetFrom(1);
        PerkDetailsCanvas.blocksRaycasts = false;
        GameManager.instance.PauseGame(false);
        UIPerkManager.IsVisible = false;

        DetailsContainer.ForEachChild(Destroy);
    }

    private void Update()
    {
        if(SimpleInput.GetKeyDown(KeyCode.P) || SimpleInput.GetKeyDown(KeyCode.JoystickButton1))
        {
            if (PerkDetailsCanvas.blocksRaycasts)
                HidePerkDetails();
            else
                DisplayPerkDetails();
        }
    }
}
