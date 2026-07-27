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

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OpenLink);
    }

    private void OpenLink()
    {
        DialogBox.DisplayRedirectDialog("You will be redirected to your browser", Link, () =>
        {
            Application.OpenURL(Link);
        }, () => { });
    }
}
