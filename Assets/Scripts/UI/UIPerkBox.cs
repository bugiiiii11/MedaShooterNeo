using Sirenix.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPerkBox : MonoBehaviour, IPointerEnterHandler
{
    public Image Background;
    public TMPro.TextMeshProUGUI Description, Title, MaxStack, Rarity;
    private Perk perkToUse;
    private UIPerkBuilder parentBuilder;

    private bool rewardScoreInstead = false;
    private int scoreReward = 0;

    public void SetFrom(UIPerkBuilder builder, Perk perk, List<PerkBehaviour> currentlyAppliedPerks)
    {
        Background.sprite = perk.Background;
        Description.text = perk.Description;
        Title.text = perk.Title;

        perkToUse = perk;
        parentBuilder = builder;
        Rarity.text = perk.Rarity.ToString();
        Rarity.color = perk.Rarity == global::Rarity.Basic ? LevelProps.instance.BasicColor : (perk.Rarity == global::Rarity.Rare ? LevelProps.instance.RareColor : LevelProps.instance.EpicColor);

        if (perk.Behaviour is PassivePerkBehaviour defaultPassive)
        {
            var availablePerk = currentlyAppliedPerks.Find(x => x.Id == perk.Behaviour.Id);

            if (availablePerk is PassivePerkBehaviour passive)
            {
                var maxStacks = passive.RemainingStacks();
                MaxStack.text = $"{availablePerk.StackCount}/{maxStacks}";

                if(availablePerk.StackCount == maxStacks)
                {
                    scoreReward = builder.ScoresForPerks.Find(x => x.Rarity == perk.Rarity).Score;
                    rewardScoreInstead = true;
                    Description.text = $"<size=33>Max stacks received.</size>\n<b>Get {scoreReward} score.</b>";
                    Background.color = new Color32(126, 126, 126, 255);
                }
            }
            else
            {
                MaxStack.text = $"0/{defaultPassive.RemainingStacks()}";
            }
        }
        else if (perk.Behaviour is FallenAngelPerk)
        {
            MaxStack.text = "";
            // if two fallen angels, reward score
            var availablePerk = currentlyAppliedPerks.Find(x => x.Id == perk.Behaviour.Id);

            if (availablePerk is FallenAngelPerk)
            {
                MaxStack.text = "";
                scoreReward = builder.ScoresForPerks.Find(x => x.Rarity == perk.Rarity).Score;
                rewardScoreInstead = true;
                Description.text = $"<size=33>Already active.</size>\n<b>Get {scoreReward} score.</b>";
                Background.color = new Color32(126, 126, 126, 255);
            }
        }
        else
        {
            MaxStack.text = "";
        }
    }

    public void SetFrom(DebugPerkSelection builder, Perk perk, List<PerkBehaviour> currentlyAppliedPerks)
    {
        if (currentlyAppliedPerks.Count > 0 && currentlyAppliedPerks[0].StackCount == 3)
            Debug.Log("here");

        Background.sprite = perk.Background;
        Description.text = perk.Description;
        Title.text = perk.Title;

        perkToUse = perk;
        Rarity.text = perk.Rarity.ToString();
        Rarity.color = perk.Rarity == global::Rarity.Basic ? LevelProps.instance.BasicColor : (perk.Rarity == global::Rarity.Rare ? LevelProps.instance.RareColor : LevelProps.instance.EpicColor);

        GetComponent<Button>().onClick.AddListener(() =>
        {
            builder.DisplayPerks();
        });

        if (perk.Behaviour is PassivePerkBehaviour defaultPassive)
        {
            var availablePerk = currentlyAppliedPerks.Find(x => x.Id == perk.Behaviour.Id);

            if (availablePerk is PassivePerkBehaviour passive)
            {
                var maxStacks = passive.RemainingStacks();
                MaxStack.text = $"{availablePerk.StackCount}/{maxStacks}";

                if (availablePerk.StackCount == maxStacks)
                {
                    // scoreReward = builder.ScoresForPerks.Find(x => x.Rarity == perk.Rarity).Score;
                    scoreReward = 400;
                    rewardScoreInstead = true;
                    Description.text = $"<size=33>Max stacks received.</size>\n<b>Get {scoreReward} score.</b>";
                    Background.color = new Color32(126, 126, 126, 255);
                }
            }
            else
            {
                MaxStack.text = $"0/{defaultPassive.RemainingStacks()}";
            }
        }
        else if (perk.Behaviour is FallenAngelPerk)
        {
            MaxStack.text = "";
            // if two fallen angels, reward score
            var availablePerk = currentlyAppliedPerks.Find(x => x.Id == perk.Behaviour.Id);

            if (availablePerk is FallenAngelPerk)
            {
                MaxStack.text = "";
                scoreReward = 400; //builder.ScoresForPerks.Find(x => x.Rarity == perk.Rarity).Score;
                rewardScoreInstead = true;
                Description.text = $"<size=33>Already active.</size>\n<b>Get {scoreReward} score.</b>";
                Background.color = new Color32(126, 126, 126, 255);
            }
        }
        else
        {
            MaxStack.text = "";
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        UIPerkManager.instance.Builder.OutlineByHoverEvent(this);
    }

    public void OnClicked()
    {
        if(parentBuilder)
            parentBuilder.Hide();

        if (rewardScoreInstead)
        {
            var scEvent = new RewardScoreEvent(scoreReward);
            scEvent.AllowMultiplier = false;
            GameManager.instance.EventManager.Dispatch(scEvent);
        }
        else
        {
            UIPerkManager.instance.ApplyPerk(perkToUse);
        }

        OneShotAudioPool.SpawnOneShot(LevelProps.instance.Confirm, volume:0.3f, pitch: 1.3f);
    }

    internal void SetOutline(bool visible)
    {
        transform.Find("Outline").gameObject.SetActive(visible);
    }
}
