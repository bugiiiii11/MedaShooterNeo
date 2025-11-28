using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;
using TMPro;

public class UIPerkBuilder : MonoBehaviour
{
    [Serializable]
    public struct ScoreAndRarity
    {
        public Rarity Rarity;
        public int Score;
    }

    [SerializeField]
    private GameObject BasePrefab;
    [SerializeField]
    private RectTransform PerkContainer;

    [SerializeField]
    private GameObject PerkUiCanvasGroup;

    [SerializeField]
    private TextMeshProUGUI TitleText;
    public List<ScoreAndRarity> ScoresForPerks;

    private bool isHidden = true;

    public void BuildPerks(IEnumerable<Perk> perks, string overrideTitle, List<PerkBehaviour> currentlyAppliedPerks)
    {
        selectedChild = 1;
        PerkContainer.ForEachChild(Destroy);
        TitleText.text = overrideTitle;
        foreach (var p in perks)
        {
            var obj = Instantiate(BasePrefab, PerkContainer);
            obj.GetComponent<UIPerkBox>().SetFrom(this, p, currentlyAppliedPerks);
        }

        // display canvas
        var canvasGroup = PerkUiCanvasGroup.GetComponent<CanvasGroup>();
        PerkUiCanvasGroup.TweenCanvasGroupAlpha(1, 0.7f).SetFrom(0).SetOnComplete(() =>
        {
            canvasGroup.blocksRaycasts = true;
        });

        // pause game
        GameManager.instance.PauseGame(true);
        isHidden = false;

        // add selection
        PerkContainer.GetChild(selectedChild).GetComponent<UIPerkBox>().SetOutline(true);
    }

    internal void Hide()
    {
        PerkContainer.ForEachChild(Destroy);
        var canvasGroup = PerkUiCanvasGroup.GetComponent<CanvasGroup>();
        PerkUiCanvasGroup.TweenCanvasGroupAlpha(0, 0.7f).SetFrom(1);
        UICountdown.InitCountdown(3, 0.6f, () =>
        {
            canvasGroup.blocksRaycasts = false;
            GameManager.instance.PauseGame(false);
        });
        isHidden = true;
        UIPerkManager.IsVisible = false;
    }

    private float horizontal = 0;
    private int selectedChild = 0;

    private void LateUpdate()
    {
        if (isHidden)
            return;

        var horiz = SimpleInput.GetAxisRaw("Horizontal");
        if (horizontal == 0 && horiz != 0)
        {
            // we have an event
            var left = horiz < 0;
            var prev = selectedChild;

            if(left)
            {
                selectedChild = (selectedChild - 1) < 0 ? (PerkContainer.childCount - 1) : selectedChild - 1;
            }
            else
            {
                selectedChild = (selectedChild + 1) % (PerkContainer.childCount);
            }

            // deselect prev, select next
                PerkContainer.GetChild(prev).GetComponent<UIPerkBox>().SetOutline(false);
                PerkContainer.GetChild(selectedChild).GetComponent<UIPerkBox>().SetOutline(true);
                OneShotAudioPool.SpawnOneShot(LevelProps.instance.SelectionClip, 0.65f, pitch:1.5f, secondsAlive: 0.3f);
        }
        horizontal = horiz;

        if (SimpleInput.GetKeyUp(KeyCode.Space) || SimpleInput.GetKeyUp(KeyCode.JoystickButton0))
        {
            PerkContainer.GetChild(selectedChild).GetComponent<UIPerkBox>().OnClicked();
        }
    }

    internal void OutlineByHoverEvent(UIPerkBox perkBox)
    {
        //  PerkContainer.GetChild(prev).GetComponent<UIPerkBox>().SetOutline(false);
        var childIndex = 0;
        for(var i = 0; i < PerkContainer.childCount; i++)
        {
            if(PerkContainer.GetChild(i).gameObject == perkBox.gameObject)
            {
                childIndex = i;
                break;
            }
        }

        if (PerkContainer.childCount > 0)
        {
            PerkContainer.GetChild(selectedChild).GetComponent<UIPerkBox>().SetOutline(false);
            PerkContainer.GetChild(childIndex).GetComponent<UIPerkBox>().SetOutline(true);
            selectedChild = childIndex;
        }

        OneShotAudioPool.SpawnOneShot(LevelProps.instance.SelectionClip, 0.65f, pitch: 1.5f, secondsAlive: 0.3f);
    }
}
