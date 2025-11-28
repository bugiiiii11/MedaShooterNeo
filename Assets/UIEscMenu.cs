using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;

public class UIEscMenu : MonoBehaviour
{
    private Action toInvoke;
    public GameObject TutorialScreen, NotifScreen, PerksCanvas;
    private bool isHidden = true;

    public VideoAdUi ServingAd;

    public void Kill()
    {
        toInvoke = () => GameManager.instance.Player.InstantKillPlayer();
        NotifScreen.SetActive(true);
    }

    public void Tutorial()
    {
        TutorialScreen.SetActive(true);
        var cg = TutorialScreen.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = true;
        cg.alpha = 1;
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        isHidden = true;
    }

    public void NotifYes()
    {
        HideEscMenu();
        toInvoke?.Invoke();
    }

    public void NotifNo()
    {
        toInvoke = null;
    }

    public void ShowEscMenu()
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;

        GameManager.instance.PauseGame(true, true);
        isHidden = false;

        PerksCanvas.SetActive(false);

        StartCoroutine(SetVideoActive(true));
    }

    IEnumerator SetVideoActive(bool active)
    {
        yield return new WaitForEndOfFrame();

        if (active)
            ServingAd.ServeAd();
        else
            ServingAd.DisableAd();
    }

    public void HideEscMenu()
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        TutorialScreen.SetActive(false);

        GameManager.instance.PauseGame(false, true);
        isHidden = true;

        PerksCanvas.SetActive(true);
        StartCoroutine(SetVideoActive(false));
    }

    private void Update()
    {
        if (UIPerkManager.IsVisible || UISettings.IsVisible)
            return;

        if(SimpleInput.GetKeyDown(KeyCode.Escape) || SimpleInput.GetKeyDown(KeyCode.Q) || SimpleInput.GetKeyDown(KeyCode.JoystickButton2))
        {
            if (isHidden)
                ShowEscMenu();
            else
                HideEscMenu();
        }
    }
}
