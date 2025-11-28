using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;
using System.Linq;

public class UIPerkManager : Singleton<UIPerkManager>
{
    public UIPerkBuilder Builder;
    public PerkDatabaseAsset PerkDatabase;

    public List<PerkBehaviour> CurrentlyAppliedPerks;

    private List<PerkBehaviour> _markedForRemoval = new();

    public static bool IsVisible { get; internal set; }

    private void Start() 
    {
        CurrentlyAppliedPerks = new List<PerkBehaviour>();
        IsVisible = false;
        GameManager.instance.EventManager.AddListener<PerkSelectionEvent>(OnDisplayPerkSelection);
    }

    private void OnDisplayPerkSelection(PerkSelectionEvent ev)
    {
        IsVisible = true;

        if (!ev.IsBuildup)
        {
            var perks = GetPerks(ev.Count, ev.AllowedRarities);
            Builder.BuildPerks(perks, ev.OverrideTitle, CurrentlyAppliedPerks);
        }
        else
        {
            var perks = GetBuildupPerks(ev.Count, ev.AllowedRarities);
            Builder.BuildPerks(perks, ev.OverrideTitle, CurrentlyAppliedPerks);
        }
    }

    public List<Perk> GetBuildupPerks(int count, Rarity[] allowedRarities)
    {
        var selectedPerks = new List<Perk>();
        var allowedRarityList = new List<Rarity>(allowedRarities);
        for (int i = 0; i < count; i++)
        {
            var perk = PerkDatabase.GetBuildupPerkByProbability(selectedPerks, allowedRarityList);

            selectedPerks.Add(perk);
        }
        return selectedPerks;
    }

    public List<Perk> GetPerks(int count, Rarity[] allowedRarities)
    {
        var selectedPerks = new List<Perk>();
        var allowedRarityList = new List<Rarity>(allowedRarities);

        for (int i = 0; i < count; i++)
        {
            Perk perk;
            
            perk = PerkDatabase.GetPowerupRandomByProbability(selectedPerks, allowedRarityList); 

            selectedPerks.Add(perk);
        }

        return selectedPerks;
    }

    public void ApplyPerk(Perk perkScriptableObject)
    {
        var perk = perkScriptableObject.Behaviour;
        if(!perk)
        {
            Debug.LogError("Perk behaviour is null.");
            return;
        }

        var instance = Instantiate(perk);
        instance.Title = perkScriptableObject.Title;

        var find = CurrentlyAppliedPerks.Find(x => x.Id == perk.Id);
        var isStack = false;
        if(find == null)
        {
            CurrentlyAppliedPerks.Add(instance);
            instance.OnInitialize(GameManager.instance.Player);
        }
        else
        {
            isStack = find.Stack(instance);

            if(!isStack)
            {
                if (find is not PassivePerkBehaviour)
                {
                    find.OnInitialize(GameManager.instance.Player);
                }
                else
                {
                    // it is full
                }
            }
        }

        GameManager.instance.EventManager.Dispatch(new PerkActivatedEvent(perkScriptableObject, instance, isStack));
    }

    public void CancelPerk(PerkBehaviour perk, bool allStacks = true)
    {
        var instance = CurrentlyAppliedPerks.Find(x => x.Id == perk.Id);

        if(!instance)
            return;
        var isStack = false;

        if (allStacks || !perk.CanRemoveStacks)
        {
            //CurrentlyAppliedPerks.Remove(instance);
            _markedForRemoval.Add(instance);
            instance.OnEnd();
        }
        else
        {
            isStack = instance.RemoveStack();
        }

        GameManager.instance.EventManager.Dispatch(new PerkDeactivatedEvent(perk, isStack));

    }

    private void Update() 
    {
        if (GameManager.instance.IsGamePaused)
            return;

        // iterate applied perks and update them if required
        foreach(var pb in CurrentlyAppliedPerks)
        {
            if(!pb.IsPassive)
                pb.OnUpdate();
        }

        if(_markedForRemoval.Count > 0)
        {
            foreach (var marked in _markedForRemoval)
                CurrentlyAppliedPerks.Remove(marked);
            _markedForRemoval.Clear();
        }

        //if(Input.GetKeyDown(KeyCode.P))
        //{
        //    /*PerksUi.TweenCanvasGroupAlpha(1, 1).SetFrom(0);
        //    GameManager.instance.PauseGame(true);*/
        //    foreach(var perk in GetPerks(3))
        //    {
        //        var color = "";
        //        if(perk.Rarity == Rarity.Basic)
        //            color = "<color=#919191>";
        //        else if(perk.Rarity == Rarity.Epic)
        //            color = "<color=#28B000>";
        //        else if(perk.Rarity == Rarity.Rare)
        //            color = "<color=#BE00E3>";
        //        //else if(perk.Rarity == Rarity.Unique)
        //        //    color = "<color=#FF8800>";
        //        print($"{color}r={perk.Rarity}\tn={perk.Title}\td={perk.Description}</color>");
        //    }
        //}
    }

    internal void GiveRandomPerks(int waveIndex)
    {
        const int enemiesPerWave = 10;
        const float dropCoinPercentage = 0.45f;
        var numOfPerksPerWave = enemiesPerWave * dropCoinPercentage;
        var allDrops = numOfPerksPerWave * waveIndex;
        var numOfPerks = allDrops / 5f;

        const float dropout = 0.6f; // for active perks
        numOfPerks *= dropout;

        // each 4th is rare
        for (var i = 0; i < numOfPerks; i++)
        {
            var perk = PerkDatabase.GetPassivePerk();

            var find = CurrentlyAppliedPerks.Find(x => x.Id == perk.Behaviour.Id);
            if (find)
            {
                if (find.Stack(perk.Behaviour))
                {
                    ApplyPerk(perk);
                }
            }
            else
                ApplyPerk(perk);
        }
    }
}
