using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElRaccoone.Tweens;
using TMPro;

public class DialogBox : Singleton<DialogBox>
{
    public Image DialogPanel;
    public TextMeshProUGUI DialogText, UrlText;
    public Button YesButton, NoButton;

    public static void DisplayRedirectDialog(string text, string url, Action onYes, Action onNo)
    {
        instance.DialogPanel.gameObject.SetActive(true);
        instance.DialogPanel.TweenCanvasGroupAlpha(1, 0.5f).SetFrom(0);
        instance.DialogText.text = text;
        instance.UrlText.text = url;

        instance.YesButton.onClick.RemoveAllListeners();
        instance.YesButton.onClick.AddListener( () =>
        {
            onYes.Invoke();
            instance.HideDialog();
        });

        instance.NoButton.onClick.RemoveAllListeners();
        instance.NoButton.onClick.AddListener(() =>
        {
            onNo.Invoke();
            instance.HideDialog();
        });
    }

    private void HideDialog()
    {
        instance.DialogPanel.gameObject.SetActive(false);
        instance.DialogPanel.TweenCanvasGroupAlpha(0, 0.5f).SetFrom(1);
    }
}
