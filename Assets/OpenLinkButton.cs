using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class OpenLinkButton : MonoBehaviour
{
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
