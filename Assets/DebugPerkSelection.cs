using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DebugPerkSelection : MonoBehaviour
{
    public PerkDatabaseAsset asset;
    public GameObject Prefab;
    public Transform ScrollViewContent;
    public GameObject ToActivate;

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.T))
        {
            DisplayPerks();
        }
    }

    public void HideScreen()
    {
        ToActivate?.SetActive(false);
        GameManager.instance.PauseGame(false);
    }

    public void DisplayPerks()
    {
        ToActivate.SetActive(true);
        ScrollViewContent.ForEachChild(Destroy);

        foreach (var p in asset.Perks.Where(x => !x.IsDisabled).OrderBy(y => y.Rarity))
        {
            var obj = Instantiate(Prefab, ScrollViewContent);
            obj.GetComponent<UIPerkBox>().SetFrom(this, p, UIPerkManager.instance.CurrentlyAppliedPerks);
        }

        // pause game
        GameManager.instance.PauseGame(true);
    }
}
