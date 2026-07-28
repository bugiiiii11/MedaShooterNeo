using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenLinkButton : MonoBehaviour
{
    // The cryptomeda.tech storefront is dead. These are the living destinations
    // (S199) -- previously patched into the built data file, now the source of truth.
    // The frontend frame keeps a window.open remap as a safety net for old builds.
    public const string MarketplaceUrl = "https://opensea.io/collection/cryptomedacards-v4";
    public const string MedaShooterUrl = "https://www.swarmresistance.com/meda-shooter";

    public string Link;

    // The confirm dialog names its destination (founder call, S201): live prod reads
    // "You will be redirected to OpenSea" and that stays. Links go to two hosts now,
    // so the sentence is derived from the URL instead of hardcoded.
    public static string RedirectMessageFor(string url)
    {
        if (url == MarketplaceUrl)
            return "You will be redirected to OpenSea";
        if (url == MedaShooterUrl)
            return "You will be redirected to Swarm Resistance";

        return "You will be redirected to your browser";
    }

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OpenLink);
    }

    private void OpenLink()
    {
        DialogBox.DisplayRedirectDialog(RedirectMessageFor(Link), Link, () =>
        {
            Application.OpenURL(Link);
        }, () => { });
    }
}
